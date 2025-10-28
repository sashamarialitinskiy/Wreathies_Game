using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/*
===============================================================================
 FLOWER SHOP (WREATH MAKER) — GameManager (Master Script) + Progress Bar
 (No customer selection version)
===============================================================================
*/

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ---------- Wreath preview ----------
    [Header("Wreath Preview")]
    public Image wreathPreviewImage;                // optional legacy preview
    public List<Sprite> levelWreathPreviews;
    [Tooltip("Hide the wreath preview after this NON-practice round number (Round 6+ will hide if set to 5).")]
    public int stopPreviewAfterRound = 5;

    // ---------- Client UI ---------------
    [Header("Client UI")]
    public GameObject clientBox;                    // ClientBox root
    [Tooltip("Optional legacy combined request text; not required if using ClientBoxController.")]
    public TextMeshProUGUI requestText;
    public Image clientImageDisplay;                // legacy portrait (optional)
    public TextMeshProUGUI clientChoicePromptText;  // no longer shown, but kept for safety

    [Tooltip("Controls Greeting -> Full and per-field UI.")]
    public ClientBoxController clientBoxController; // <-- assign this

    // Greeting -> Full timing
    [Header("Client UI Timing")]
    [Tooltip("Seconds to show only the greeting before revealing the full request (non-practice). Set to 0 to keep greeting until revealed by script/tutorial.")]
    public float fullRequestRevealDelay = 1.0f;

    // ---------- Client info -------------
    [Header("Client Info")]
    public ClientData selectedClient;
    public List<ClientData> allClients;
    [HideInInspector] public ClientData clientOptionA; // legacy, not used for choice anymore
    [HideInInspector] public ClientData clientOptionB; // legacy, not used for choice anymore

    // ---------- Round control -----------
    [Header("Round Control")]
    public int currentRound = 0;   // 0-based
    public int maxRounds = 10;     // total rounds today (practice + live)
    public int totalSlots = 12;    // flowers per wreath

    // ---------- Progress ----------
    [Header("Progress")]
    [SerializeField] private LevelProgressBar progressBar;

    // ---------- Tries / Attempts --------
    [Header("Attempt Limits")]
    [Tooltip("How many tries the player gets per PRACTICE round (first N rounds).")]
    public int practiceTries = 3;
    [Tooltip("How many tries the player gets per NON-practice round.")]
    public int nonPracticeTries = 2;
    private int triesAllowedThisRound = 0;
    private int attemptsUsed = 0;

    // ---------- Request data ------------
    [Header("Request Info")]
    public Dictionary<FlowerColor, int> requestedCounts;

    // ---------- Panels ------------------
    [Header("Panels")]
    public GameObject clientChoicePanel; // kept in inspector but will stay hidden
    public GameObject gameOverPanel;

    // ---------- Input ----------
    [Header("Input")]
    [SerializeField] private Button doneButton;   // drag the green DoneButton here

    // ---------- Feedback bubble ----------
    [Header("Feedback Panel (tutorial/feedback bubble)")]
    public TextMeshProUGUI feedbackText;
    public CanvasGroup feedbackGroup;
    [TextArea] public string initialFeedbackMessage = "Let's see what you make!";

    // ---------- Bow selection -----------
    [Header("Bow Selection")]
    public GameObject bowChoicePanel;
    public Image bowAImage;
    public Image bowBImage;
    public List<BowData> allBows;
    private BowData bowOptionA;
    private BowData bowOptionB;

    // ---------- Timing (no visual FX) ---
    [Header("Timing")]
    [SerializeField] private float celebrationDuration = 4f;

    // ---------- Tutorial / Practice -----
    [Header("Tutorial & Practice")]
    public int practiceRounds = 2; // first N rounds are "Practice"
    public TextMeshProUGUI roundBadgeText;
    public bool showRoundBadges = true;
    public TutorialDirector tutorialDirector;  // assign if used in practice
    public ClientData tutorialClient;

    [Tooltip("0-based index used if you want practice round 2 to skip earlier steps.")]
    public int practice2TutorialStartIndex = 0;

    [Header("Practice Options")]
    public bool skipClientChoiceInPractice = true;         // now irrelevant, kept for Inspector safety
    [Tooltip("If true, the round auto-starts on scene load; if false, IntroManager should call StartNewRound() when the intro ends.")]
    public bool autoStartPracticeOnSceneStart = false;     // now ignored
    public ClientData forcedPracticeClient;                // legacy assist value, no longer used

    public bool IsPracticeRound => currentRound < practiceRounds;
    public int VisibleRoundIndex => IsPracticeRound ? (currentRound + 1) : (currentRound + 1 - practiceRounds);

    private int tutorialFlowerCount = 0;
    private List<LevelData> levels = new();

    // ---------- Flavor lines ------------
    private static readonly string[] SUCCESS_LINES = {
        "Perfectly arranged—every flower is just right. Thank you!",
        "You nailed my request: the colors and flowers are spot on!",
        "Gorgeous work! This wreath is exactly what I hoped for!",
        "Flawless—every flower fits beautifully. I love it!"
    };
    private static readonly string[] RETRY_LINES = {
        "Not quite the order I wanted—give it another go!",
        "Close, but the flowers don’t match—try again!",
        "Almost there! Adjust the flowers and try again!",
        "This isn’t the right mix of flowers yet—let’s try again."
    };
    private static readonly string[] FINAL_ACCEPT_LINES = {
        "Close—but not quite the wreath I asked for. I’ll still take it—thanks for trying!",
        "Almost there! The flowers are a bit off, but I’ll go with this one.",
        "Not exactly what I wanted, yet it still looks nice. I’ll keep it!",
        "It’s not perfect for my order, but I appreciate the effort—I’ll take it anyway."
    };
    private static readonly string[] PICK_BOW_LINES = {
        "Nice! Now let's finish with a bow.",
        "Beautiful! Now let's add a bow.",
        "Ooohh! Now let's add a bow."
    };
    private static readonly string[] BOW_PRAISE_LINES = {
        "Awesome job!",
        "Look at those colors!",
        "Wowee what a wreath!",
        "Look at you go!"
    };

    private int pickBowCycleIndex = 0;
    private int bowPraiseCycleIndex = 0;

    private bool clickLocked = false;
    private bool bowChoiceScheduled = false;
    private bool bowChoiceShownThisRound = false;
    private bool nextClientScheduled = false;

    private bool practiceHold = false;
    private bool canSubmit   = false;

    public enum FlowerColor { Red, Blue, Yellow, Purple }

    // ------------------------------------------------------------------------
    // LIFECYCLE
    // ------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        BuildLevels();

        // We don't actually need to "PickTwoClients" anymore for UI choice,
        // but we can still call it in case something else depends on clientOptionA/B.
        PickTwoClients();

        practiceHold = (tutorialDirector != null && IsPracticeRound);
        canSubmit = false;

        EnsureFeedbackVisible(initialFeedbackMessage);

        WreathZone.Instance.ClearWreath();
        FlowerSpawner.Instance.SpawnAllFlowers();

        ValidateBowList();
        WireDoneButton();

        // --- NEW BEHAVIOR: hide the choice panel and immediately start with a random client
        if (clientChoicePanel) clientChoicePanel.SetActive(false);

        // pick a starting client for round 0
        selectedClient = AutoSelectRandomClient();
        StartNewRound();

        if (progressBar) progressBar.gameObject.SetActive(false);
        UpdateProgressUI();
    }

    // ------------------------------------------------------------------------
    // BUTTON WIRING / SUBMIT
    // ------------------------------------------------------------------------

    // Keep Inspector-added listeners clean; unify on one handler.
    void WireDoneButton()
    {
        if (!doneButton)
            doneButton = GameObject.Find("DoneButton")?.GetComponent<Button>();
        if (!doneButton)
        {
            Debug.LogWarning("GameManager: doneButton not assigned.");
            return;
        }

        int p = doneButton.onClick.GetPersistentEventCount();
        for (int i = 0; i < p; i++)
            doneButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        doneButton.onClick.RemoveListener(OnDoneClickedUnified);
        doneButton.onClick.AddListener(OnDoneClickedUnified);
    }

    public void DoneButtonPressed() => OnDoneClickedUnified();

    public void OnDoneClickedUnified()
    {
        // tutorial override hook
        if (tutorialDirector != null && tutorialDirector.PracticeDoneArmed)
        {
            tutorialDirector.ExternalPracticeDone();
            return;
        }

        if (practiceHold || !canSubmit) return;
        if (bowChoiceScheduled || bowChoiceShownThisRound) return;
        if (clickLocked) return;
        clickLocked = true;

        if (requestedCounts == null) { clickLocked = false; return; }

        PerformSubmission();
    }

    private void PerformSubmission()
    {
        bool valid = ValidateWreath();

        if (valid)
        {
            AudioManager.Instance?.PlayCorrect();
            ShowClientSuccess();

            if (!bowChoiceScheduled && !bowChoiceShownThisRound)
            {
                bowChoiceScheduled = true;
                StartCoroutine(WaitThenShowBowChoice(2f));
            }
        }
        else
        {
            AudioManager.Instance?.PlayWrong();
            attemptsUsed++;

            if (attemptsUsed < triesAllowedThisRound)
            {
                ShowFeedback(Pick(RETRY_LINES));
                StartCoroutine(UnlockClickAfter(0.25f));
            }
            else
            {
                ShowClientFinalReaction();

                if (!bowChoiceScheduled && !bowChoiceShownThisRound)
                {
                    bowChoiceScheduled = true;
                    StartCoroutine(WaitThenShowBowChoice(2f));
                }
            }
        }
    }

    // ------------------------------------------------------------------------
    // UI HELPERS
    // ------------------------------------------------------------------------

    public void ShowClientBox(bool show = true)
    {
        if (clientBox) clientBox.SetActive(show);
    }

    public void ToggleRequestPanel(bool on)
    {
        if (requestText) requestText.gameObject.SetActive(on);
    }

    public void ToggleFeedbackPanel(bool on)
    {
        if (!feedbackText) return;
        var fp = feedbackText.transform.parent;
        if (fp) fp.gameObject.SetActive(on);
    }

    public void RevealClientRequestNow()
    {
        if (IsPracticeRound && selectedClient == null)
            selectedClient = (tutorialClient != null) ? tutorialClient : AutoSelectRandomClient();

        ToggleFeedbackPanel(false);

        if (clientBoxController)
            StartCoroutine(clientBoxController.RevealFullPrepared());

        ToggleRequestPanel(true);
    }

    // (Legacy tutorial path that used to open the choice screen.
    //  We keep the coroutine but it will no longer open that panel.)
    public void RevealRequestThenOpenChoice(float holdSeconds = 1f)
    {
        StartCoroutine(_RevealRequestThenOpenChoice(holdSeconds));
    }
    private IEnumerator _RevealRequestThenOpenChoice(float holdSeconds)
    {
        RevealClientRequestNow();
        yield return new WaitForSeconds(holdSeconds);
        EndTutorialAndOpenClientChoice(true, 0f);
    }

    // This used to end tutorial and show client choice.
    // Now we end tutorial and just move on like a normal next round.
    public void EndTutorialAndOpenClientChoice(bool skipRemainingPractice = true, float delay = 0f)
    {
        StartCoroutine(_EndTutorialAndOpenClientChoice(skipRemainingPractice, delay));
    }
    private IEnumerator _EndTutorialAndOpenClientChoice(bool skipRemainingPractice, float delay)
    {
        if (skipRemainingPractice)
            currentRound = Mathf.Max(currentRound + 1, practiceRounds);

        UpdateProgressUI();

        if (delay > 0f) yield return new WaitForSeconds(delay);

        ToggleFeedbackPanel(false);
        if (clientBox) clientBox.SetActive(false);

        // We don't open a panel. We just assign a random client and start next round.
        selectedClient = AutoSelectRandomClient();
        if (clientChoicePanel) clientChoicePanel.SetActive(false);

        canSubmit = false;

        StartNewRound();
    }

    public void ShowFeedbackLine(string message) => EnsureFeedbackVisible(message);

    void EnsureFeedbackVisible(string message)
    {
        if (!feedbackText) return;

        var parentGO = feedbackText.transform.parent ? feedbackText.transform.parent.gameObject : null;

        if (clientBox) clientBox.SetActive(true);
        if (parentGO) parentGO.SetActive(true);
        parentGO?.transform.SetAsLastSibling();

        var cg = feedbackGroup ? feedbackGroup : parentGO ? parentGO.GetComponent<CanvasGroup>() : null;
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = false;
        }

        feedbackText.enabled = true;
        var col = feedbackText.color; col.a = 1f; feedbackText.color = col;

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = message;
        feedbackText.ForceMeshUpdate();
    }

    public void SetFeedback(string msg) => EnsureFeedbackVisible(msg);

    // This function was originally called by the customer buttons.
    // We'll keep it so nothing else breaks, but now it just makes sure gameplay continues.
    public void SetSelectedClient(ClientData client)
    {
        selectedClient = client ?? AutoSelectRandomClient();

        if (clientChoicePanel) clientChoicePanel.SetActive(false);
        StartNewRound();

        StartCoroutine(ForceFullAfterSelection());
    }

    IEnumerator WaitThenShowBowChoice(float delay) { yield return new WaitForSeconds(delay); ShowBowChoice(); }
    IEnumerator WaitThenNextClient(float delay)    { yield return new WaitForSeconds(delay); ContinueToNextClient(); }
    IEnumerator UnlockClickAfter(float delay)      { yield return new WaitForSeconds(delay); clickLocked = false; }

    IEnumerator EnsureFullAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsPracticeRound && clientBoxController)
            clientBoxController.ShowFullImmediate();
    }

    IEnumerator ForceFullAfterSelection()
    {
        yield return null;
        yield return null;
        if (!IsPracticeRound && clientBoxController)
            clientBoxController.ShowFullImmediate();
    }

    void SetTutorialActive(bool on)
    {
        if (tutorialDirector == null) return;
        tutorialDirector.gameObject.SetActive(on);
    }

    // ------------------------------------------------------------------------
    // ROUND LIFECYCLE
    // ------------------------------------------------------------------------

    public void ReleasePracticeHold()
    {
        practiceHold = false;
        StartNewRound();
    }

    public void StartNewRound()
    {
        if (practiceHold) return;

        attemptsUsed = 0;
        triesAllowedThisRound = IsPracticeRound ? Mathf.Max(1, practiceTries) : Mathf.Max(1, nonPracticeTries);
        clickLocked = false;
        bowChoiceScheduled = false;
        bowChoiceShownThisRound = false;
        nextClientScheduled  = false;
        canSubmit = true;

        tutorialFlowerCount = 0;

        if (bowChoicePanel) bowChoicePanel.SetActive(false);

        SetTutorialActive(IsPracticeRound);

        // --- NEW: decide who the client is for THIS round ---
        if (IsPracticeRound && tutorialClient != null)
        {
            // during practice we can force a specific tutorial client
            selectedClient = tutorialClient;
        }
        else
        {
            // otherwise pick random every time
            selectedClient = AutoSelectRandomClient();
        }

        // pull the request that defines how many of each flower color we need
        requestedCounts = levels[currentRound].fixedRequest;

        PreloadClientRequestContent();

        if (clientBox) clientBox.SetActive(true);
        if (clientBoxController)
        {
            // Always start with greeting-only ("Hi, I'm <name>.")
            clientBoxController.ShowGreetingOnly();

            // For non-practice rounds, auto-reveal the full request after a short delay (if configured).
            if (!IsPracticeRound && fullRequestRevealDelay > 0f)
                StartCoroutine(_RevealRequestAfterDelay(fullRequestRevealDelay));
        }

        WreathZone.Instance.ClearWreath();
        FlowerSpawner.Instance.SpawnAllFlowers();

        if (roundBadgeText && showRoundBadges)
        {
            if (IsPracticeRound)
                roundBadgeText.text = $"Practice {VisibleRoundIndex}/{practiceRounds}";
            else
                roundBadgeText.text = $"Round {VisibleRoundIndex}/{maxRounds - practiceRounds}";
            roundBadgeText.gameObject.SetActive(true);
        }

        if (!IsPracticeRound)
            EnsureFeedbackVisible(initialFeedbackMessage);

        // --- Wreath preview handling (transparent when no sprite) ---
        if (wreathPreviewImage != null)
        {
            var preview = GetPreviewSpriteForRound(currentRound);
            wreathPreviewImage.sprite = preview;

            var c = wreathPreviewImage.color;
            c.a = (preview != null) ? 1f : 0f;   // visible for real rounds, invisible as placeholder
            wreathPreviewImage.color = c;

            wreathPreviewImage.enabled = true;   // keep enabled to avoid layout jumps
        }

        GameEvents.RoundStarted?.Invoke();

        if (IsPracticeRound && tutorialDirector != null && practiceHold)
        {
            int startIdx = (VisibleRoundIndex == 2) ? practice2TutorialStartIndex : 0;
            tutorialDirector.BeginFrom(startIdx);
        }

        if (progressBar) progressBar.gameObject.SetActive(true);
        UpdateProgressUI();
    }

    private IEnumerator _RevealRequestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RevealClientRequestNow(); // uses existing full reveal path
    }

    public void NotifyFlowerPlaced()
    {
        tutorialFlowerCount++;

        if (tutorialFlowerCount == 1)
            GameEvents.FirstFlowerPlaced?.Invoke();
        else if (tutorialFlowerCount == 2)
            GameEvents.SecondFlowerPlaced?.Invoke();
    }

    public void NotifyFirstFlowerPlaced() => NotifyFlowerPlaced();

    // ------------------------------------------------------------------------
    // CLIENT REQUEST CONTENT / UI
    // ------------------------------------------------------------------------

    void PreloadClientRequestContent()
    {
        string nameLine   = selectedClient ? $"Hi, I'm {selectedClient.clientName}." : "Hi!";
        string requestStr = "I want my wreath:\n";
        foreach (var kv in requestedCounts)
        {
            string fraction = FractionFromCount(kv.Value);
            requestStr += $"- " + fraction + " " + kv.Key + "\n";
        }

        Sprite previewSprite = GetPreviewSpriteForRound(currentRound);

        if (clientBoxController)
        {
            clientBoxController.SetFullContent(
                nameLine,
                requestStr,
                previewSprite,
                selectedClient ? selectedClient.clientImage : null
            );
        }

        if (requestText)
        {
            string legacy = $"{nameLine}\n{requestStr}";
            requestText.text = legacy;
        }
        if (clientImageDisplay && selectedClient)
            clientImageDisplay.sprite = selectedClient.clientImage;
    }

    Sprite GetPreviewSpriteForRound(int roundIndex)
    {
        if (!IsPracticeRound && VisibleRoundIndex > stopPreviewAfterRound)
            return null;

        if (levelWreathPreviews != null &&
            roundIndex >= 0 &&
            roundIndex < levelWreathPreviews.Count)
        {
            return levelWreathPreviews[roundIndex];
        }
        return null;
    }

    void ShowClientRequestUI()               => PreloadClientRequestContent();
    void ShowFeedback(string message)        => EnsureFeedbackVisible(message);

    // ------------------------------------------------------------------------
    // BOW CHOICE FLOW
    // ------------------------------------------------------------------------

    void ShowBowChoice()
    {
        if (bowChoiceShownThisRound) return;

        bowChoiceShownThisRound = true;
        bowChoiceScheduled = false;

        var pool = new List<BowData>();
        foreach (var b in allBows)
        {
            if (b == null || b.bowSprite == null)
            {
                Debug.LogWarning("BowData missing or has no sprite.");
                continue;
            }
            pool.Add(b);
        }

        if (pool.Count < 2)
        {
            Debug.LogError("Need at least 2 valid BowData with sprites to show choices.");
            return;
        }

        ShuffleList(pool);
        bowOptionA = pool[0];
        bowOptionB = pool[1];

        if (bowChoicePanel) bowChoicePanel.SetActive(true);

        if (bowAImage) bowAImage.sprite = bowOptionA.bowSprite;
        if (bowBImage) bowBImage.sprite = bowOptionB.bowSprite;

        var textA = bowAImage ? bowAImage.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        var textB = bowBImage ? bowBImage.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (textA) textA.gameObject.SetActive(false);
        if (textB) textB.gameObject.SetActive(false);

        EnsureFeedbackVisible(NextPickBowLine());
        canSubmit = false;
    }

    public void SelectBowA() { AudioManager.Instance?.PlayClick(); ApplyBowToWreath(bowOptionA); }
    public void SelectBowB() { AudioManager.Instance?.PlayClick(); ApplyBowToWreath(bowOptionB); }

    void ApplyBowToWreath(BowData selectedBow)
    {
        WreathZone.Instance.AttachBow(selectedBow.bowSprite);
        AudioManager.Instance?.PlayBowPlaced();
        if (bowChoicePanel) bowChoicePanel.SetActive(false);
        EnsureFeedbackVisible(NextBowPraiseLine());

        if (nextClientScheduled) return;
        nextClientScheduled = true;
        StartCoroutine(WaitThenNextClient(celebrationDuration));
    }

    string NextPickBowLine()
    {
        if (PICK_BOW_LINES.Length == 0) return "Let's add a bow!";
        string line = PICK_BOW_LINES[pickBowCycleIndex];
        pickBowCycleIndex = (pickBowCycleIndex + 1) % PICK_BOW_LINES.Length;
        return line;
    }

    string NextBowPraiseLine()
    {
        if (BOW_PRAISE_LINES.Length == 0) return "Great job!";
        string line = BOW_PRAISE_LINES[bowPraiseCycleIndex];
        bowPraiseCycleIndex = (bowPraiseCycleIndex + 1) % BOW_PRAISE_LINES.Length;
        return line;
    }

    // ------------------------------------------------------------------------
    // VALIDATION / LEVELS / PROGRESS
    // ------------------------------------------------------------------------

    string FractionFromCount(int count)
    {
        return count switch
        {
            6  => "1/2",
            4  => "1/3",
            3  => "1/4",
            2  => "1/6",
            1  => "1/12",
            8  => "2/3",
            9  => "3/4",
            10 => "5/6",
            12 => "all",
            _  => $"{count}/12"
        };
    }

    void BuildLevels()
    {
        levels = new List<LevelData>
        {
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 6 }, { FlowerColor.Blue, 6 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Yellow, 6 }, { FlowerColor.Blue, 6 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 4 }, { FlowerColor.Red, 4 }, { FlowerColor.Blue, 4 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 4 }, { FlowerColor.Red, 4 }, { FlowerColor.Blue, 2 }, { FlowerColor.Yellow, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Blue, 3 }, { FlowerColor.Yellow, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 4 }, { FlowerColor.Blue, 4 }, { FlowerColor.Yellow, 4 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Blue, 3 }, { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Yellow, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 6 }, { FlowerColor.Red, 3 }, { FlowerColor.Blue, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Yellow, 4 }, { FlowerColor.Purple, 4 }, { FlowerColor.Red, 2 }, { FlowerColor.Blue, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Blue, 4 }, { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Yellow, 2 } }),
        };
    }

    bool ValidateWreath()
    {
        if (requestedCounts == null) return false;

        Dictionary<FlowerColor, int> actualCounts = new();
        foreach (var flowerObj in WreathZone.Instance.GetFlowers())
        {
            var data = flowerObj.GetComponent<FlowerData>();
            if (data != null)
            {
                if (!actualCounts.ContainsKey(data.flowerColor))
                    actualCounts[data.flowerColor] = 0;
                actualCounts[data.flowerColor]++;
            }
        }

        foreach (var req in requestedCounts)
        {
            if (!actualCounts.ContainsKey(req.Key) || actualCounts[req.Key] != req.Value)
                return false;
        }

        int totalPlaced = 0;
        foreach (var val in actualCounts.Values)
            totalPlaced += val;

        return totalPlaced == totalSlots;
    }

    void ShowClientSuccess()       => ShowFeedback(Pick(SUCCESS_LINES));
    void ShowClientFinalReaction() => ShowFeedback(Pick(FINAL_ACCEPT_LINES));
    string Pick(string[] lines)    => lines[UnityEngine.Random.Range(0, lines.Length)];

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Kept mostly for compatibility / debugging, but not relied on for gameplay now.
    void PickTwoClients()
    {
        if (allClients == null || allClients.Count == 0)
        {
            Debug.LogError("No clients configured in GameManager.allClients!");
            return;
        }

        if (allClients.Count == 1)
        {
            clientOptionA = allClients[0];
            clientOptionB = allClients[0];
            return;
        }

        List<ClientData> shuffled = new(allClients);
        ShuffleList(shuffled);

        clientOptionA = shuffled[0];
        clientOptionB = shuffled[1];

        if (clientChoicePromptText)
        {
            clientChoicePromptText.text = (currentRound == 0)
                ? "Which customer do you want to help first?"
                : "Which customer do you want to help next?";
        }
    }

    // After finishing a wreath + bow, move directly to the next round.
    void ContinueToNextClient()
    {
        currentRound++;
        UpdateProgressUI();
        canSubmit = false;

        if (currentRound >= maxRounds)
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            EnsureFeedbackVisible("Great work—shop closed for today!");
            return;
        }

        // pick a new random client for the next round
        selectedClient = AutoSelectRandomClient();

        // make sure the customer choice panel never shows
        if (clientChoicePanel) clientChoicePanel.SetActive(false);

        StartNewRound();
    }

    // Pick a random client from allClients for normal rounds
    ClientData AutoSelectRandomClient()
    {
        if (allClients != null && allClients.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, allClients.Count);
            return allClients[idx];
        }
        return null;
    }

    void ValidateBowList()
    {
        for (int i = 0; i < allBows.Count; i++)
        {
            var b = allBows[i];
            if (b == null) Debug.LogWarning($"allBows[{i}] is NULL.");
            else if (b.bowSprite == null) Debug.LogWarning($"BowData '{b.name}' has NO sprite assigned.");
        }
    }

    public void OnTutorialFinished()
    {
        ToggleFeedbackPanel(false);

        if (clientBoxController && clientBox)
        {
            clientBox.SetActive(true);
            clientBoxController.ShowFullImmediate();
        }
    }

    // --- Progress Bar helper ---
    void UpdateProgressUI()
    {
        if (progressBar == null) return;
        int level1Based = Mathf.Clamp(currentRound + 1, 1, maxRounds);
        progressBar.SetLevel(level1Based);
    }
}

// ------------------------------------------------------------
[System.Serializable]
public class LevelData
{
    public Dictionary<GameManager.FlowerColor, int> fixedRequest;
    public LevelData(Dictionary<GameManager.FlowerColor, int> request) { fixedRequest = request; }
}

public static class GameEvents
{
    public static Action RoundStarted;
    public static Action FirstFlowerPlaced;
    public static Action SecondFlowerPlaced;
}
