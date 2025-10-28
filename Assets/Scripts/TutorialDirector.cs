using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// This script runs the *tutorial* at the beginning of the game.
// It displays short step-by-step messages (“Pick up a flower,” “Now tap Done”) in a text bubble.
// It can fade messages in/out, wait for player actions, and tell the game when to begin real play.

public class TutorialDirector : MonoBehaviour
{
    // Each tutorial step can be timed or wait for a signal (like a click or in-game action)
    public enum StepMode { Timed, WaitForSignal }

    // A class that defines what happens in each tutorial step
    [System.Serializable]
    public class Step
    {
        [TextArea] public string message; // The text shown to the player

        [Header("Placement (optional)")]
        public RectTransform messageAnchor; // Optional position anchor (UI element to point near)
        public Vector2 screenOffset = new Vector2(0, 80); // Offset of the bubble from the anchor

        [Header("Flow")]
        public StepMode mode = StepMode.Timed;  // Whether the step auto-advances or waits
        public float duration = 2f;             // Time to show if “Timed” mode
        [Tooltip("If empty and mode=WaitForSignal, waits for NEXT click.")]
        public string waitSignal = "";          // Optional keyword to wait for (e.g., "firstFlower")

        [Header("Actions")]
        [Tooltip("Legacy reveal toggle. Leave OFF for the step that starts practice; StartPracticeAfterThisStep will reveal at the correct time.")]
        public bool revealClientBoxAfterThisStep = false;

        [Header("Tutorial gating (optional)")]
        [Tooltip("Tick ONLY on the step that says 'Now tap the Done button'.")]
        public bool armDoneForPractice = false; // Enables the Done button temporarily during practice

        [Tooltip("Tick on the step AFTER practice Done (e.g., 'Awesome! …'). This will start the real round and then reveal the client box once content is ready.")]
        public bool startPracticeAfterThisStep = false; // Triggers start of actual gameplay after tutorial
    }

    // ---------- UI (message bubble) ----------
    [Header("UI (message bubble)")]
    public RectTransform messagePanel;  // The floating bubble container
    public TMP_Text messageText;        // The text inside the bubble
    public CanvasGroup messageGroup;    // Controls fading in/out of the bubble

    [Header("Bubble defaults")]
    public bool snapToDefaultWhenNoAnchor = true;           // Place bubble at a default spot if no anchor
    public Vector2 defaultScreenPercent = new Vector2(0.5f, 0.82f); // Default bubble screen position

    // ---------- Navigation UI ----------
    [Header("Navigation UI")]
    [Tooltip("Parent that contains the tutorial Back/Next arrows.")]
    public GameObject navRoot;          // The parent object for back/next arrows
    public Button backButton;           // Manual back button (optional)
    public Button nextButton;           // Manual next button (optional)
    public bool hideNavUntilBegin = true; // Hide the navigation until the tutorial actually starts

    // ---------- Done button gating ----------
    [Header("Practice gating")]
    public Button doneButton;           // The big green "Done" button
    [Tooltip("Optional transparent blocker object. ON = blocks clicks.")]
    public GameObject doneBlocker;      // Invisible blocker to prevent early clicks
    public bool lockDoneUntilPracticeStep = true; // If true, "Done" is locked until tutorial allows it

    // ---------- Behaviour ----------
    [Header("Behaviour")]
    public bool autoStartOnEnable = false; // Start automatically when script is enabled
    public bool debugLogs = false;         // Print debug info to console for testing

    [Header("Fade")]
    public float fadeIn = 0.2f;           // Fade in time for text bubble
    public float fadeOut = 0.15f;         // Fade out time for text bubble

    [Header("Steps")]
    public List<Step> steps = new();      // The list of tutorial steps
    public UnityEvent onFinished;         // Optional event triggered when tutorial ends

    // ---------- Optional Client Box ----------
    [Header("Client Box")]
    public ClientBoxController clientBox; // The box that shows greeting and request
    public bool showGreetingAtStart = true;
    [TextArea] public string greetingAtStart = "Hi there!"; // Greeting text for intro

    // ---------- Optional congrats hook ----------
    [Header("After two flowers (optional)")]
    public bool congratsAfterSecondFlower = true; // Whether to show a "Good job!" message
    [TextArea] public string congratsMessage = "Good job! Now let's complete your first customer's request!";
    public float congratsHold = 2.0f; // How long the "Good job" message stays on screen

    // ---------- Runtime control (keeps track of current state) ----------
    int index = -1;                 // Which tutorial step we’re on
    Coroutine runCo;                // Holds reference to the running coroutine
    readonly HashSet<string> signals = new(); // Tracks in-game events (like firstFlower)
    bool nextClicked = false;       // Player pressed "Next"
    bool waitingForPracticeDone = false; // Waiting for player to press "Done" in tutorial
    bool firstRequestStarted = false;    // Prevents starting the main game twice

    // =====================================================
    // LIFECYCLE
    // =====================================================
    void Awake()
    {
        // Hide navigation until the tutorial actually starts
        if (hideNavUntilBegin && navRoot) navRoot.SetActive(false);
    }

    void OnEnable()
    {
        // Prepare back/next buttons
        SafeWireBack();
        SafeWireNext();

        // Listen for gameplay events (from GameEvents.cs)
        GameEvents.FirstFlowerPlaced += OnFirstFlower;
        GameEvents.SecondFlowerPlaced += OnSecondFlower;

        // Optionally auto-start tutorial
        if (autoStartOnEnable) Begin();
    }

    void OnDisable()
    {
        // Clean up listeners when disabled
        if (backButton) backButton.onClick.RemoveListener(Back);
        if (nextButton) nextButton.onClick.RemoveListener(Next);
        if (doneButton) doneButton.onClick.RemoveListener(OnDoneClicked);

        GameEvents.FirstFlowerPlaced -= OnFirstFlower;
        GameEvents.SecondFlowerPlaced -= OnSecondFlower;

        if (runCo != null) StopCoroutine(runCo);
        runCo = null;

        if (hideNavUntilBegin && navRoot) navRoot.SetActive(false);
        ArmPracticeDone(false);
    }

    // =====================================================
    // PUBLIC API
    // =====================================================

    // Start the tutorial from the beginning
    public void Begin() => BeginFrom(0);

    // Start the tutorial from a specific step index
    public void BeginFrom(int startIndex)
    {
        if (hideNavUntilBegin && navRoot) navRoot.SetActive(true);

        // Auto-link buttons if not assigned manually
        AutoFindButtonsIfNeeded();
        SafeWireBack();
        SafeWireNext();

        if (lockDoneUntilPracticeStep) ArmPracticeDone(false);

        // Reset runtime values
        signals.Clear();
        nextClicked = false;
        waitingForPracticeDone = false;
        firstRequestStarted = false;

        if (messageGroup) messageGroup.alpha = 0f;

        // Show greeting if using a ClientBox
        if (clientBox && showGreetingAtStart)
            clientBox.ShowGreetingOnly(greetingAtStart);

        index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, steps.Count - 1)) - 1;

        // Start running tutorial steps
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(Run());
    }

    // Player manually clicks "Next"
    public void Next()
    {
        if (debugLogs) Debug.Log("[Tutorial] Next()");
        nextClicked = true;
    }

    // Player manually clicks "Back"
    public void Back()
    {
        if (debugLogs) Debug.Log("[Tutorial] Back()");
        if (runCo != null) StopCoroutine(runCo);

        signals.Clear();
        nextClicked = false;
        waitingForPracticeDone = false;
        if (messageGroup) messageGroup.alpha = 0f;

        index = Mathf.Max(-1, index - 2); // Step back
        runCo = StartCoroutine(Run());
    }

    // Called when some other script triggers a signal
    public void Signal(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        id = id.Trim();
        if (debugLogs) Debug.Log($"[Tutorial] Signal({id})");
        signals.Add(id);
    }

    // =====================================================
    // PRACTICE “DONE” GATING — controls when Done button works
    // =====================================================

    // Lets GameManager check if practice Done button is active
    public bool PracticeDoneArmed => waitingForPracticeDone;

    public void ExternalPracticeDone()
    {
        // Called by GameManager after the tutorial's Done is pressed
        if (!waitingForPracticeDone) return;
        waitingForPracticeDone = false;
        Signal("practiceDone"); // Advance the step
    }

    // Enable or disable Done button interactivity
    void ArmPracticeDone(bool armed)
    {
        waitingForPracticeDone = armed;

        if (doneButton)
        {
            doneButton.onClick.RemoveListener(OnDoneClicked);
            if (armed) doneButton.onClick.AddListener(OnDoneClicked);
            doneButton.interactable = armed; // Shows visually if clickable
        }

        if (doneBlocker) doneBlocker.SetActive(!armed); // ON = blocks clicks
        if (debugLogs) Debug.Log($"[Tutorial] Practice Done armed = {armed}");
    }

    void OnDoneClicked()
    {
        if (!waitingForPracticeDone) return;
        if (debugLogs) Debug.Log("[Tutorial] Practice Done clicked → signal 'practiceDone'");
        waitingForPracticeDone = false;
        Signal("practiceDone");
    }

    // =====================================================
    // EVENT SIGNALS FROM GAMEPLAY
    // =====================================================
    void OnFirstFlower() => Signal("firstFlower");   // When player places 1st flower
    void OnSecondFlower() => Signal("secondFlower"); // When player places 2nd flower

    // =====================================================
    // NAVIGATION & BUTTON HELPERS
    // =====================================================
    void SafeWireBack()
    {
        if (!backButton) return;
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(Back);
        UpdateBackState();
    }

    void SafeWireNext()
    {
        if (!nextButton) return;
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(Next);
    }

    void AutoFindButtonsIfNeeded()
    {
        if (!navRoot) return;
        var buttons = navRoot.GetComponentsInChildren<Button>(true);

        // Try to find buttons by name if not assigned
        if (!backButton)
            foreach (var b in buttons)
                if (b && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("prev"))) { backButton = b; break; }

        if (!nextButton)
            foreach (var b in buttons)
                if (b && (b.name.ToLower().Contains("next") || b.name.ToLower().Contains("forward"))) { nextButton = b; break; }

        // Fallback: if there are exactly 2 buttons, use the first as back and second as next
        if ((!backButton || !nextButton) && buttons.Length == 2)
        {
            if (!backButton) backButton = buttons[0];
            if (!nextButton) nextButton = buttons[1];
        }
    }

    void UpdateBackState()
    {
        if (backButton) backButton.interactable = (index > 0);
    }

    // =====================================================
    // MESSAGE BUBBLE PLACEMENT
    // =====================================================
    void EnsureBubbleVisible()
    {
        if (!messagePanel) return;
        messagePanel.gameObject.SetActive(true);
        messagePanel.SetAsLastSibling(); // Keep it on top of other UI
    }

    void PositionBubble(Step s)
    {
        if (!messagePanel) return;

        if (s.messageAnchor)
        {
            // Position the bubble near a specific UI element
            var cam = GetCanvasCam();
            Vector3 screen = RectTransformUtility.WorldToScreenPoint(cam, s.messageAnchor.position);
            messagePanel.position = screen + (Vector3)s.screenOffset;
        }
        else if (snapToDefaultWhenNoAnchor)
        {
            // Place bubble in default top-center location
            Vector3 screen = new Vector3(
                Screen.width * defaultScreenPercent.x,
                Screen.height * defaultScreenPercent.y,
                0f
            );
            messagePanel.position = screen + (Vector3)s.screenOffset;
        }
    }

    // =====================================================
    // MAIN TUTORIAL RUNNER (coroutine that steps through tutorial)
    // =====================================================
    IEnumerator Run()
    {
        while (NextIndex())
        {
            var s = steps[index];

            if (debugLogs) Debug.Log($"[Tutorial] Step {index + 1}/{steps.Count}: \"{s.message}\"");

            EnsureBubbleVisible();
            PositionBubble(s);
            if (messageText) messageText.text = s.message;

            // Allow “Done” button if this step is the one teaching it
            if (s.armDoneForPractice) ArmPracticeDone(true);

            // Fade in message bubble
            yield return Fade(messageGroup, 1f, fadeIn);

            // Handle how each step ends
            if (s.mode == StepMode.Timed)
            {
                // Auto-advance after a few seconds
                yield return new WaitForSeconds(Mathf.Max(0.01f, s.duration));
            }
            else // WaitForSignal
            {
                // Wait for a specific gameplay event or button press
                if (!string.IsNullOrWhiteSpace(s.waitSignal))
                {
                    string key = s.waitSignal.Trim();
                    if (debugLogs) Debug.Log($"[Tutorial] Waiting for signal '{key}'");
                    while (!signals.Contains(key)) yield return null;

                    // Show bonus message after second flower
                    if (key == "secondFlower" && congratsAfterSecondFlower && messageText && !string.IsNullOrWhiteSpace(congratsMessage))
                    {
                        messageText.text = congratsMessage;
                        yield return new WaitForSeconds(Mathf.Max(0.01f, congratsHold));
                    }

                    signals.Remove(key);
                }
                else
                {
                    // If no specific signal, wait for "Next" button click
                    if (debugLogs) Debug.Log("[Tutorial] Waiting for NEXT click");
                    nextClicked = false;
                    while (!nextClicked) yield return null;
                    nextClicked = false;
                }
            }

            // Turn off Done button if it was armed
            if (s.armDoneForPractice) ArmPracticeDone(false);

            // Fade out bubble before moving on
            yield return Fade(messageGroup, 0f, fadeOut);

            // If step says to reveal client box, show it
            if (!s.startPracticeAfterThisStep && s.revealClientBoxAfterThisStep && clientBox)
                yield return StartCoroutine(clientBox.RevealFullPrepared());

            // If step starts real gameplay, begin the first real round
            if (s.startPracticeAfterThisStep)
                yield return StartCoroutine(StartPracticePhase());
        }

        // Hide navigation when done
        if (hideNavUntilBegin && navRoot) navRoot.SetActive(false);

        onFinished?.Invoke(); // Trigger any "finished" events

        // Tell GameManager that tutorial is done
        if (GameManager.Instance) GameManager.Instance.OnTutorialFinished();
    }

    IEnumerator StartPracticePhase()
    {
        if (firstRequestStarted) yield break; // Prevent duplicate start
        firstRequestStarted = true;

        // Tell GameManager to start actual gameplay
        if (GameManager.Instance)
            GameManager.Instance.ReleasePracticeHold();

        // Wait a few frames so everything loads correctly
        yield return null;
        yield return null;

        // Reveal the first client box with full request
        if (clientBox)
            yield return StartCoroutine(clientBox.RevealFullPrepared());
    }

    // Moves to the next step index and updates UI
    bool NextIndex()
    {
        index++;
        UpdateBackState();
        return index >= 0 && index < steps.Count;
    }

    // Generic fade helper for UI elements
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

    // Finds the correct camera for positioning UI in world space
    Camera GetCanvasCam()
    {
        var cv = messagePanel ? messagePanel.GetComponentInParent<Canvas>() : null;
        if (!cv) return null;
        if (cv.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return cv.worldCamera != null ? cv.worldCamera : Camera.main;
    }
}
