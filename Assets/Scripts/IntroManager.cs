using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    public enum Transition { Fade, Pop, Instant }

    // ---------- Slides ----------
    [Header("Slides (in order)")]
    public List<GameObject> introPanels = new();   // 0 = title (Play), 1..N = content

    // ---------- Intro navigation ONLY ----------
    [Header("Navigation UI (INTRO ONLY)")]
    [Tooltip("Parent that holds the INTRO arrows (NOT the tutorial ones).")]
    public GameObject navRoot;          // e.g. Canvas/IntroNavRoot
    public Button backButton;           // e.g. Backbutton
    public Button nextButton;           // e.g. Nextbutton

    // ---------- Background (optional) ----------
    [Header("Background (optional)")]
    public Image backgroundTarget;

    // ---------- Behaviour ----------
    [Header("Behaviour")]
    [Tooltip("Hide Back/Next on the very first (title) slide.")]
    public bool hideNavOnFirstSlide = true;

    [Tooltip("Disable Next button when you're on the last slide.")]
    public bool disableNextOnLastSlide = false;

    [Tooltip("Index of the first CONTENT slide (shown after Play). Usually 1.")]
    public int firstContentSlideIndex = 1;

    // ---------- Global ----------
    [Header("Global Overrides")]
    public bool disableAllTypewriter = true;
    public bool debugLogs = false;

    // ---------- Fallback transitions ----------
    [Header("Default Transition (no SubMessageSequence)")]
    public Transition defaultIn  = Transition.Pop;
    public Transition defaultOut = Transition.Instant;

    [Header("Pop settings")]
    public float popDuration = 0.18f;
    public float popInScale  = 1.08f;
    public float popOutScale = 0.92f;

    [Header("Sequential reveal (no SubMessageSequence)")]
    public float childStagger = 0.15f;

    // ---------- Per-slide config ----------
    [System.Serializable]
    public class Slide
    {
        public GameObject root;
        public Sprite background;

        [Header("Message group (optional)")]
        public Transform textGroup;     // parent with CanvasGroups or SubMessageSequence
        public float fadeIn = 0.2f;     // used if defaultIn == Fade
        public float hold   = 0f;
        public float fadeOut= 0f;       // used if defaultOut == Fade

        [Header("Minimum slide time")]
        public float minTotalDuration = 0.0f;

        [Header("Exclusive sequence (one-at-a-time)")]
        public Transform exclusiveSequenceGroup; // children with CanvasGroup
        public float seqFadeIn  = 0.25f;
        public float seqHold    = 0.90f;
        public float seqFadeOut = 0.25f;
    }

    [Header("Per-Slide Settings (same order as panels)")]
    public List<Slide> slides = new();

    // ---------- Handoff to Tutorial ----------
    [Header("Handoff to Tutorial")]
    [Tooltip("Drag your TutorialDirector here. We'll call Begin() when the intro finishes.")]
    public TutorialDirector tutorial;
    public bool startTutorialOnFinish = true;

    // ---------- Runtime ----------
    int currentIndex = 0;   // starts at title (0)
    Coroutine runner;
    bool skipping;

    // Debounce lock to prevent double-advance on a single click
    bool _nextLocked;
    [Tooltip("How long to ignore extra Next clicks (seconds).")]
    public float nextDebounceSeconds = 0.12f;

    // ========================================================================
    // Lifecycle
    // ========================================================================
    void OnEnable()
    {
        EnsureSlidesCount();
        currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, introPanels.Count - 1));
        ShowOnlyCurrentPanel();
        WireNav();
        UpdateNavState();
        UpdateNavVisibility();

        if (debugLogs)
        {
            var count = FindObjectsOfType<IntroManager>(true).Length;
            Debug.Log($"[Intro] Enabled on {name}. Active IntroManagers in scene: {count}");
        }
    }

    void OnDisable()
    {
        UnwireNav();
        StopRunner();
    }

    // ========================================================================
    // Play button on slide 0
    // ========================================================================
    public void OnPlayClicked()
    {
        if (debugLogs) Debug.Log("[Intro] Play");
        BeginFrom(firstContentSlideIndex);
    }

    // ========================================================================
    // Manual navigation (intro arrows)
    // ========================================================================
    public void BackSlide()
    {
        StopRunner();
        if (currentIndex > 0) currentIndex--;
        if (debugLogs) Debug.Log($"[Intro] Back → {currentIndex}");
        ShowOnlyCurrentPanel();
        UpdateNavState();
        UpdateNavVisibility();
        StartCurrentSlide();
    }

    public void NextSlide()
    {
        if (_nextLocked) return;                       // <<< debounce
        StartCoroutine(_NextDebounced());
    }

    IEnumerator _NextDebounced()
    {
        _nextLocked = true;
        if (nextButton) nextButton.interactable = false;

        StopRunner();
        if (currentIndex < introPanels.Count - 1)
        {
            currentIndex++;
            if (debugLogs) Debug.Log($"[Intro] Next → {currentIndex}");
            ShowOnlyCurrentPanel();
            UpdateNavState();
            UpdateNavVisibility();
            StartCurrentSlide();
        }
        else
        {
            if (debugLogs) Debug.Log("[Intro] Finished → handoff");
            FinishIntro();
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.06f, nextDebounceSeconds));
        _nextLocked = false;
        if (nextButton) nextButton.interactable = true;
    }

    public void BeginFrom(int startIndex)
    {
        StopRunner();
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, introPanels.Count - 1));
        if (debugLogs) Debug.Log($"[Intro] BeginFrom({currentIndex})");
        ShowOnlyCurrentPanel();
        UpdateNavState();
        UpdateNavVisibility();
        StartCurrentSlide();
    }

    // ========================================================================
    // Slide runner (no auto-advance)
    // ========================================================================
    void StartCurrentSlide()
    {
        if (runner != null) StopCoroutine(runner);
        runner = StartCoroutine(RunSlide(currentIndex));
    }

    void StopRunner()
    {
        skipping = true;
        if (runner != null) StopCoroutine(runner);
        runner = null;
        skipping = false;
    }

    IEnumerator RunSlide(int index)
    {
        yield return null; // settle
        skipping = false;

        if (index < 0 || index >= introPanels.Count) yield break;
        var s = slides[index];
        if (!s.root) s.root = introPanels[index];
        if (!s.root) yield break;

        if (backgroundTarget && s.background) backgroundTarget.sprite = s.background;

        bool handled = false;

        // 1) Exclusive sequence (optional)
        if (s.exclusiveSequenceGroup)
        {
            yield return PlayExclusiveSequence(s);
            handled = true;
        }

        // 2) SubMessageSequence path (optional)
        if (!handled && s.textGroup)
        {
            var seq = s.textGroup.GetComponent<SubMessageSequence>();
            if (seq != null && seq.messages != null && seq.messages.Count > 0)
            {
                seq.playOnEnable = false;
                if (disableAllTypewriter) seq.useTypewriter = false;

                foreach (var g in seq.messages)
                {
                    if (!g) continue;
                    g.alpha = 0f;
                    g.transform.localScale = Vector3.one;
                    g.gameObject.SetActive(false);
                }

                float startAt = Time.unscaledTime;
                yield return StartCoroutine(seq.Play());

                float elapsed = Time.unscaledTime - startAt;
                float remain = s.minTotalDuration - elapsed;
                if (remain > 0f) yield return HoldOrSkip(remain);

                handled = true;
            }
        }

        // 3) Fallback: simple sequential reveal of CanvasGroups
        if (!handled) yield return ShowSequential(s);

        // STOP — user uses Next/Back to change slides.
    }

    // ========================================================================
    // Finish → TutorialDirector.Begin()
    // ========================================================================
    void FinishIntro()
    {
        StopRunner();
        foreach (var p in introPanels) if (p) p.SetActive(false);
        SetNavVisible(false);

        if (startTutorialOnFinish && tutorial)
        {
            tutorial.Begin();     // <<< handoff to your TutorialDirector
        }

        gameObject.SetActive(false);
    }

    // ========================================================================
    // UI helpers
    // ========================================================================
    void ShowOnlyCurrentPanel()
    {
        for (int i = 0; i < introPanels.Count; i++)
            if (introPanels[i]) introPanels[i].SetActive(i == currentIndex);

        if (debugLogs)
            Debug.Log($"[Intro] Showing slide {currentIndex}: {introPanels[currentIndex]?.name}");
    }

    void WireNav()
    {
        AutoFindButtonsIfNeeded();

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackSlide);
        }
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextSlide);
        }
    }

    void UnwireNav()
    {
        if (backButton) backButton.onClick.RemoveAllListeners();
        if (nextButton) nextButton.onClick.RemoveAllListeners();
    }

    void UpdateNavState()
    {
        if (backButton) backButton.interactable = (currentIndex > 0);

        if (nextButton)
        {
            bool isLast = (currentIndex >= introPanels.Count - 1);
            nextButton.interactable = !(disableNextOnLastSlide && isLast);
        }
    }

    void UpdateNavVisibility()
    {
        bool onTitle = (currentIndex == 0);
        bool show = !(hideNavOnFirstSlide && onTitle);
        SetNavVisible(show);
    }

    void SetNavVisible(bool visible)
    {
        if (navRoot) navRoot.SetActive(visible);
        else
        {
            if (backButton) backButton.gameObject.SetActive(visible);
            if (nextButton) nextButton.gameObject.SetActive(visible);
        }
    }

    void EnsureSlidesCount()
    {
        while (slides.Count < introPanels.Count) slides.Add(new Slide());
    }

    void AutoFindButtonsIfNeeded()
    {
        if (!navRoot) return;
        var buttons = navRoot.GetComponentsInChildren<Button>(true);

        if (!backButton)
            foreach (var b in buttons)
                if (b && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("prev"))) { backButton = b; break; }

        if (!nextButton)
            foreach (var b in buttons)
                if (b && (b.name.ToLower().Contains("next") || b.name.ToLower().Contains("forward"))) { nextButton = b; break; }

        if ((!backButton || !nextButton) && buttons.Length == 2)
        {
            if (!backButton) backButton = buttons[0];
            if (!nextButton) nextButton = buttons[1];
        }
    }

    // ========================================================================
    // Simple reveal helpers
    // ========================================================================
    static List<CanvasGroup> CollectGroups(Transform root)
    {
        var list = new List<CanvasGroup>();
        if (!root) return list;
        foreach (var g in root.GetComponentsInChildren<CanvasGroup>(true))
            list.Add(g);
        return list;
    }

    IEnumerator PlayExclusiveSequence(Slide s)
    {
        var items = CollectGroups(s.exclusiveSequenceGroup);
        foreach (var g in items)
        {
            if (!g) continue;
            g.gameObject.SetActive(false);
            SetGroupAlpha(g, 0f);
            g.transform.localScale = Vector3.one;
        }

        float startAt = Time.unscaledTime;

        foreach (var g in items)
        {
            if (!g) continue;
            g.gameObject.SetActive(true);
            yield return FadeTo(g, 1f, Mathf.Max(0f, s.seqFadeIn));
            yield return HoldOrSkip(Mathf.Max(0f, s.seqHold));
            yield return FadeTo(g, 0f, Mathf.Max(0f, s.seqFadeOut));
            g.gameObject.SetActive(false);
            if (skipping) yield break;
        }

        float elapsed = Time.unscaledTime - startAt;
        float remain = s.minTotalDuration - elapsed;
        if (remain > 0f) yield return HoldOrSkip(remain);
    }

    IEnumerator ShowSequential(Slide s)
    {
        var root = s.textGroup ? s.textGroup : s.root.transform;
        var groups = CollectGroups(root);

        foreach (var g in groups)
        {
            if (!g) continue;
            g.gameObject.SetActive(false);
            switch (defaultIn)
            {
                case Transition.Fade:
                    SetGroupAlpha(g, 0f);
                    g.transform.localScale = Vector3.one;
                    break;
                case Transition.Pop:
                    SetGroupAlpha(g, 1f);
                    g.transform.localScale = Vector3.one * (2f - popInScale);
                    break;
                case Transition.Instant:
                    SetGroupAlpha(g, 1f);
                    g.transform.localScale = Vector3.one;
                    break;
            }
        }

        foreach (var g in groups)
        {
            if (!g) continue;
            g.gameObject.SetActive(true);

            if (defaultIn == Transition.Pop)
                yield return Pop(g, true, popDuration, popInScale);
            else if (defaultIn == Transition.Fade)
                yield return FadeTo(g, 1f, s.fadeIn);
            else
                yield return HoldOrSkip(childStagger);
        }

        if (s.hold > 0f) yield return HoldOrSkip(s.hold);

        if (defaultOut == Transition.Fade)
            foreach (var g in groups) yield return FadeTo(g, 0f, s.fadeOut);
        else if (defaultOut == Transition.Pop)
        {
            foreach (var g in groups) yield return Pop(g, false, popDuration, popOutScale);
            foreach (var g in groups) SetGroupAlpha(g, 0f);
        }
    }

    IEnumerator FadeTo(CanvasGroup g, float target, float duration)
    {
        if (!g) yield break;
        if (duration <= 0f || skipping) { SetGroupAlpha(g, target); yield break; }
        float start = g.alpha, t = 0f;
        while (t < duration && !skipping)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        SetGroupAlpha(g, target);
    }

    IEnumerator Pop(CanvasGroup g, bool appearing, float duration, float targetScale)
    {
        if (!g) yield break;

        Vector3 from = appearing ? Vector3.one * (2f - targetScale) : Vector3.one;
        Vector3 to   = appearing ? Vector3.one : Vector3.one * targetScale;

        float t = 0f;
        while (t < duration && !skipping)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            g.transform.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        g.transform.localScale = to;
    }

    IEnumerator HoldOrSkip(float seconds)
    {
        if (seconds <= 0f || skipping) yield break;
        float t = 0f;
        while (t < seconds && !skipping)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static void SetGroupAlpha(CanvasGroup g, float a)
    {
        g.alpha = a;
        bool on = a >= 0.999f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}
