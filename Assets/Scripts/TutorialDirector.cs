using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class TutorialDirector : MonoBehaviour
{
    public enum StepMode { Timed, WaitForSignal }

    [System.Serializable]
    public class Step
    {
        [TextArea] public string message;

        [Header("Place the message near (optional)")]
        public RectTransform messageAnchor;
        public Vector2 screenOffset = new Vector2(0, 80);

        [Header("Glows / Highlights")]
        public List<GameObject> glowsToEnable = new();
        public List<GameObject> glowsToDisableOnExit = new();

        [Header("Input & interaction")]
        public bool lockAllInput = true;
        public List<Behaviour> enableOnEnter = new();
        public List<Behaviour> disableOnEnter = new();

        [Header("Timing / flow")]
        public StepMode mode = StepMode.Timed;
        public float duration = 2f;
        public string waitSignal = "";
        public float waitTimeout = 0f;

        [Header("Hooks (optional)")]
        public UnityEvent onEnter;
        public UnityEvent onExit;
    }

    [Header("UI (wire these)")]
    public Canvas tutorialCanvas;
    public Image blocker;
    public RectTransform messagePanel;
    public TMP_Text messageText;
    public CanvasGroup messageGroup;

    [Header("Fade timings")]
    public float messageFadeIn = 0.2f;
    public float messageFadeOut = 0.15f;

    [Header("Steps (autoplay then wait)")]
    public List<Step> steps = new();

    [Header("Behavior")]
    public bool clearAllGlowsOnEnter = true;
    public bool autoCollectGlows = true;
    public bool debugLogs = false;

    [Header("Events")]
    public UnityEvent onAllDone;

    int index = -1;
    bool running;
    readonly HashSet<string> signals = new();
    readonly HashSet<GameObject> allGlows = new();

    void Awake()
    {
        if (tutorialCanvas) tutorialCanvas.enabled = false;
        if (messageGroup) messageGroup.alpha = 0f;
        if (blocker) blocker.raycastTarget = false;
    }

    void OnEnable()
    {
        GameEvents.FirstFlowerPlaced  += () => Signal("firstFlower");
        GameEvents.SecondFlowerPlaced += () => Signal("secondFlower");
    }
    void OnDisable()
    {
        GameEvents.FirstFlowerPlaced  -= () => Signal("firstFlower");
        GameEvents.SecondFlowerPlaced -= () => Signal("secondFlower");
    }

    public void Begin()
    {
        if (running) return;

        // 👇 critical fix: clear stale signals from previous runs
        signals.Clear();

        if (autoCollectGlows)
        {
            allGlows.Clear();
            foreach (var s in steps)
            {
                foreach (var g in s.glowsToEnable)       if (g) allGlows.Add(g);
                foreach (var g in s.glowsToDisableOnExit) if (g) allGlows.Add(g);
            }
        }
        foreach (var g in allGlows) SetGlow(g, false);

        if (tutorialCanvas) tutorialCanvas.enabled = true;
        if (messageGroup) messageGroup.alpha = 0f;
        if (blocker) blocker.raycastTarget = false;

        running = true;
        index = -1;
        StartCoroutine(Run());
    }

    public void Signal(string id)
    {
        if (!string.IsNullOrEmpty(id)) signals.Add(id);
    }

    IEnumerator Run()
    {
        while (NextStep())
        {
            var s = steps[index];

            if (messagePanel && s.messageAnchor)
            {
                Vector3 screen = RectTransformUtility.WorldToScreenPoint(null, s.messageAnchor.position);
                messagePanel.position = screen + (Vector3)s.screenOffset;
            }

            if (messageText) messageText.text = s.message;

            if (blocker) blocker.raycastTarget = s.lockAllInput;

            foreach (var b in s.disableOnEnter) if (b) b.enabled = false;
            foreach (var b in s.enableOnEnter)  if (b) b.enabled = true;

            if (clearAllGlowsOnEnter)
                foreach (var g in allGlows) SetGlow(g, false);
            foreach (var g in s.glowsToEnable) SetGlow(g, true);

            s.onEnter?.Invoke();

            yield return Fade(messageGroup, 1f, messageFadeIn);

            if (s.mode == StepMode.Timed)
            {
                float hold = Mathf.Max(0.01f, s.duration); // tiny clamp so 0 never skips
                yield return WaitUnscaled(hold);
            }
            else
            {
                float waited = 0f;
                while (!signals.Contains(s.waitSignal))
                {
                    if (s.waitTimeout > 0f && waited >= s.waitTimeout) break;
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
                signals.Remove(s.waitSignal);
            }

            yield return Fade(messageGroup, 0f, messageFadeOut);

            foreach (var g in s.glowsToDisableOnExit) SetGlow(g, false);
            s.onExit?.Invoke();
        }

        Finish();
    }

    void Finish()
    {
        if (blocker) blocker.raycastTarget = false;
        if (tutorialCanvas) tutorialCanvas.enabled = false;
        foreach (var g in allGlows) SetGlow(g, false);

        // also clear any signals so a later Begin() starts clean
        signals.Clear();

        running = false;
        if (GameManager.Instance) GameManager.Instance.OnTutorialFinished();
        onAllDone?.Invoke();
    }

    bool NextStep()
    {
        index++;
        return index >= 0 && index < steps.Count;
    }

    IEnumerator Fade(CanvasGroup g, float target, float time)
    {
        if (!g) yield break;
        if (time <= 0f) { g.alpha = target; yield break; }
        float start = g.alpha, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        g.alpha = target;
    }

    IEnumerator WaitUnscaled(float s)
    {
        float t = 0f;
        while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static void SetGlow(GameObject go, bool on)
    {
        if (!go) return;
        if (go.activeSelf != on) go.SetActive(on);
    }
}
