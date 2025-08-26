using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Slides (in order)")]
    public List<GameObject> introPanels = new List<GameObject>();   // IntroPanel_Start, IntroPanel_Flowers1, ...

    [Header("Optional UI")]
    public UnityEngine.UI.Button backButton;

    [Header("Background (optional)")]
    public Image backgroundTarget;              // IntroBackground image

    [Header("Autoplay")]
    public bool autoPlay = true;
    public bool resetToFirstOnEnable = true;
    public bool loop = false;

    [Header("Finish Behaviour")]
    public bool goStraightToPractice = true;    // if false, you can show a menu instead

    [Header("Global Overrides")]
    public bool disableAllTypewriter = true;    // you said no typing
    public bool debugLogs = false;

    // ---- Per-slide settings -------------------------------------------------
    [System.Serializable]
    public class Slide
    {
        [Header("References (leave empty to auto-discover)")]
        public GameObject root;                 // defaults to introPanels[i]
        public Sprite background;               // optional per-slide bg
        public Transform textGroup;             // child named "TextGroup"

        [Header("Sequence inside TextGroup")]
        [Tooltip("Play each direct child of TextGroup as a step.")]
        public bool useSubMessages = true;

        [Tooltip("These are shown from the start and left on (e.g. Panel, Fractions).")]
        public List<string> persistentNames = new List<string>();

        [Tooltip("Keep messages ON screen after they play (accumulate).")]
        public bool messagesStayVisible = false;

        [Tooltip("If not empty, only these names will stay visible (others still fade).")]
        public List<string> accumulateNames = new List<string>();

        [Header("Message Timing (fade only)")]
        public float msgFadeIn  = 0.2f;
        public float msgHold    = 1.0f;
        public float msgFadeOut = 0.2f;

        [Header("Typewriter (ignored if globally disabled)")]
        public bool  typewriter = false;
        public float charDelay  = 0.02f;

        [Header("Persistent (optional)")]
        public float persistentFadeIn  = 0.15f;
        public bool  fadeOutPersistentAtEnd = false;

        [Header("Slide Duration (failsafe)")]
        public float minTotalDuration = 3f;
    }

    [Header("Per-Slide Settings (same order as introPanels)")]
    public List<Slide> slides = new List<Slide>();

    // ---- runtime ------------------------------------------------------------
    int currentIndex = 0;
    Coroutine runner;
    bool skipRequested;

    void OnEnable()
    {
        if (resetToFirstOnEnable) currentIndex = 0;
        ShowOnlyCurrentPanel();
        UpdateBack();
        if (autoPlay) StartCurrentSlide();
    }

    void Start()
    {
        ShowOnlyCurrentPanel();
        UpdateBack();
        if (autoPlay && runner == null) StartCurrentSlide();
    }

    public void BackSlide()
    {
        StopRunner();
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowOnlyCurrentPanel();
            UpdateBack();
            if (autoPlay) StartCurrentSlide();
        }
    }

    public void NextSlide()
    {
        StopRunner();
        if (currentIndex < introPanels.Count - 1)
        {
            currentIndex++;
            ShowOnlyCurrentPanel();
            UpdateBack();
            if (autoPlay) StartCurrentSlide();
        }
        else
        {
            FinishIntro();
        }
    }

    public void SkipCurrent() => skipRequested = true;

    // ------------------------------------------------------------------------
    void StartCurrentSlide() => runner = StartCoroutine(RunSlide(currentIndex));

    void StopRunner()
    {
        skipRequested = true;
        if (runner != null) StopCoroutine(runner);
        runner = null;
    }

    IEnumerator RunSlide(int index)
    {
        yield return null; // let activation settle
        skipRequested = false;

        if (index < 0 || index >= introPanels.Count) yield break;
        EnsureSlidesCount();

        Slide s = slides[index];
        if (!s.root) s.root = introPanels[index];
        if (!s.root) yield break;

        if (backgroundTarget && s.background) backgroundTarget.sprite = s.background;

        if (!s.textGroup)
        {
            var tg = FindChildByName(s.root.transform, "TextGroup");
            if (tg) s.textGroup = tg;
        }

        float slideStart = Time.unscaledTime;

        if (s.useSubMessages && s.textGroup)
        {
            // persistent defaults (useful names you had)
            HashSet<string> persistSet = new HashSet<string>(s.persistentNames);
            persistSet.Add("Panel");
            persistSet.Add("Fractions");

            // collect direct children
            List<Transform> children = new List<Transform>();
            foreach (Transform c in s.textGroup) children.Add(c);

            // partition
            List<CanvasGroup> persistent = new List<CanvasGroup>();
            List<CanvasGroup> messages   = new List<CanvasGroup>();

            foreach (var t in children)
            {
                if (persistSet.Contains(t.name)) persistent.Add(EnsureCanvasGroup(t.gameObject));
                else                              messages.Add(EnsureCanvasGroup(t.gameObject));
            }

            // init alpha
            foreach (var g in persistent) SetGroupAlpha(g, 0f);
            foreach (var g in messages)   SetGroupAlpha(g, 0f);

            // fade in persistent once
            foreach (var g in persistent)
                yield return FadeTo(g, 1f, s.persistentFadeIn);

            // log
            if (debugLogs)
                Debug.Log($"[Intro] Slide {index} '{s.root.name}': {messages.Count} messages, {persistent.Count} persistent.", s.root);

            // sequence all messages
            for (int i = 0; i < messages.Count && !skipRequested; i++)
            {
                var g = messages[i];

                // fade in
                yield return FadeTo(g, 1f, s.msgFadeIn);

                // typewriter (skip if globally disabled)
                if (!disableAllTypewriter && s.typewriter)
                    yield return TypewriterReveal(g.transform, s.charDelay);

                // hold
                yield return HoldOrSkip(s.msgHold);

                // keep or fade
                bool keepThis =
                    s.messagesStayVisible &&
                    (s.accumulateNames == null || s.accumulateNames.Count == 0 || s.accumulateNames.Contains(g.name));

                if (!keepThis)
                    yield return FadeTo(g, 0f, s.msgFadeOut);
                // else: leave alpha at 1 so it accumulates
            }

            // optional clean-up of persistent
            if (s.fadeOutPersistentAtEnd)
                for (int i = 0; i < persistent.Count; i++)
                    yield return FadeTo(persistent[i], 0f, 0.12f);
        }
        else
        {
            // nothing to play, just wait a bit so you see something
            yield return HoldOrSkip(Mathf.Max(1f, s.minTotalDuration));
        }

        // enforce minimum slide time
        if (!skipRequested && s.minTotalDuration > 0f)
        {
            float elapsed = Time.unscaledTime - slideStart;
            float remain = Mathf.Max(0f, s.minTotalDuration - elapsed);
            if (remain > 0f) yield return HoldOrSkip(remain);
        }

        // advance
        if (!skipRequested)
        {
            if (currentIndex < introPanels.Count - 1) NextSlide();
            else if (loop) { currentIndex = 0; ShowOnlyCurrentPanel(); UpdateBack(); StartCurrentSlide(); }
            else FinishIntro();
        }
    }

    // ---- helpers ------------------------------------------------------------
    void ShowOnlyCurrentPanel()
    {
        for (int i = 0; i < introPanels.Count; i++)
            if (introPanels[i]) introPanels[i].SetActive(i == currentIndex);
    }

    void UpdateBack()
    {
        if (backButton) backButton.interactable = (currentIndex > 0);
    }

    void FinishIntro()
    {
        StopRunner();
        foreach (var p in introPanels) if (p) p.SetActive(false);

        if (goStraightToPractice && GameManager.Instance)
        {
            // jump into practice flow
            if (GameManager.Instance.clientChoicePanel)
                GameManager.Instance.clientChoicePanel.SetActive(false);
            GameManager.Instance.currentRound = 0;   // start at round 0
            GameManager.Instance.SetFeedback("Practice: drag a flower onto the wreath to begin.");
            GameManager.Instance.StartNewRound();
        }

        gameObject.SetActive(false);
    }

    IEnumerator FadeTo(CanvasGroup g, float target, float duration)
    {
        if (!g) yield break;
        if (duration <= 0f || skipRequested) { SetGroupAlpha(g, target); yield break; }

        float start = g.alpha, t = 0f;
        while (t < duration && !skipRequested)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        SetGroupAlpha(g, target);
    }

    IEnumerator HoldOrSkip(float seconds)
    {
        if (seconds <= 0f || skipRequested) yield break;
        float t = 0f;
        while (t < seconds && !skipRequested) { t += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator TypewriterReveal(Transform root, float perCharDelay)
    {
        var tmps = root.GetComponentsInChildren<TMP_Text>(true);
        if (tmps == null || tmps.Length == 0) yield break;

        int[] totals = new int[tmps.Length];
        for (int i = 0; i < tmps.Length; i++)
        {
            var t = tmps[i];
            t.ForceMeshUpdate();
            totals[i] = t.textInfo.characterCount;
            t.maxVisibleCharacters = 0;
        }

        int longest = 0; for (int i = 0; i < totals.Length; i++) if (totals[i] > longest) longest = totals[i];

        for (int c = 0; c <= longest && !skipRequested; c++)
        {
            for (int i = 0; i < tmps.Length; i++)
                tmps[i].maxVisibleCharacters = Mathf.Min(c, totals[i]);

            if (perCharDelay > 0f) yield return new WaitForSecondsRealtime(perCharDelay);
            else yield return null;
        }
    }

    void SetGroupAlpha(CanvasGroup g, float a)
    {
        g.alpha = a;
        bool on = a >= 0.999f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    void EnsureSlidesCount()
    {
        while (slides.Count < introPanels.Count) slides.Add(new Slide());
    }

    static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
