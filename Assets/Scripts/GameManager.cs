using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // ---------- Wreath preview ----------
    [Header("Wreath Preview")]
    public Image wreathPreviewImage;
    public List<Sprite> levelWreathPreviews;

    // ---------- Client UI ---------------
    [Header("Client UI")]
    public GameObject clientBox;                 // client bubble panel
    public TextMeshProUGUI requestText;          // "Hi, I'm ___ / I want my wreath..."
    public Image clientImageDisplay;             // portrait
    public TextMeshProUGUI clientChoicePromptText;

    // ---------- Client info -------------
    [Header("Client Info")]
    public ClientData selectedClient;
    public List<ClientData> allClients;
    [HideInInspector] public ClientData clientOptionA;
    [HideInInspector] public ClientData clientOptionB;

    // ---------- Round control -----------
    [Header("Round Control")]
    public int currentRound = 0;
    public int maxRounds = 10;
    public int totalSlots = 12;
    private bool retried = false;

    // ---------- Request data ------------
    [Header("Request Info")]
    public Dictionary<FlowerColor, int> requestedCounts;

    // ---------- Panels ------------------
    [Header("Panels")]
    public GameObject clientChoicePanel;
    public GameObject gameOverPanel;

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

    // ---------- FX ----------------------
    [Header("FX")]
    [SerializeField] private WreathCelebration wreathFX;
    [SerializeField] private float celebrationDuration = 4f;

    // ---------- Tutorial / Practice -----
    [Header("Tutorial & Practice")]
    public int practiceRounds = 2; // first N rounds are "Practice"
    public TextMeshProUGUI roundBadgeText;
    public bool showRoundBadges = true;
    public TutorialDirector tutorialDirector;
    public ClientData tutorialClient;

    [Header("Practice Options")]
    public bool skipClientChoiceInPractice = true;
    public ClientData forcedPracticeClient;

    public bool IsPracticeRound => currentRound < practiceRounds;
    public int VisibleRoundIndex => IsPracticeRound ? (currentRound + 1) : (currentRound + 1 - practiceRounds);

    // tutorial counters
    private int tutorialFlowerCount = 0;

    private List<LevelData> levels = new();

    // ---------- Flavor lines ------------
    private static readonly string[] SUCCESS_LINES = {
        "Perfectly arranged—every flower is just right. Thank you!",
        "You nailed my request: the colors and flowers are spot on!",
        "Gorgeous work! This wreath is exactly what I hoped for.",
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

    // guards
    private bool clickLocked = false;
    private bool bowChoiceScheduled = false;
    private bool bowChoiceShownThisRound = false;
    private bool nextClientScheduled = false;

    public enum FlowerColor { Red, Blue, Yellow, Purple }

    // ------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        BuildLevels();
        PickTwoClients();

        EnsureFeedbackVisible(initialFeedbackMessage);

        WreathZone.Instance.ClearWreath();
        FlowerSpawner.Instance.SpawnAllFlowers();

        if (wreathFX == null)
            wreathFX = UnityEngine.Object.FindFirstObjectByType<WreathCelebration>(FindObjectsInactive.Include);

        ValidateBowList();

        // enter first round
        if (IsPracticeRound && skipClientChoiceInPractice)
        {
            selectedClient = AutoSelectPracticeClient();
            if (clientChoicePanel) clientChoicePanel.SetActive(false);
            if (clientBox) clientBox.SetActive(false);
            StartNewRound();
        }
        else
        {
            if (clientChoicePanel) clientChoicePanel.SetActive(true);
            if (clientBox) clientBox.SetActive(false);
            if (clientChoicePromptText) clientChoicePromptText.text = "Which customer do you want to help first?";
        }
    }

    // ---------------- UI helpers (used by TutorialDirector via UnityEvents) ---
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

    /// Show client request (used by tutorial “reveal request” step)
    public void RevealClientRequestNow()
    {
        if (IsPracticeRound && selectedClient == null)
            selectedClient = tutorialClient != null ? tutorialClient : AutoSelectPracticeClient();

        ToggleFeedbackPanel(false);
        ShowClientRequestUI();
        ToggleRequestPanel(true);
    }

    /// Reveal request, wait briefly, then open the chooser.
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

    /// Open the client choice and move out of practice, guaranteed.
    public void EndTutorialAndOpenClientChoice(bool skipRemainingPractice = true, float delay = 0f)
    {
        StartCoroutine(_EndTutorialAndOpenClientChoice(skipRemainingPractice, delay));
    }
    private IEnumerator _EndTutorialAndOpenClientChoice(bool skipRemainingPractice, float delay)
    {
        if (skipRemainingPractice)
            currentRound = Mathf.Max(currentRound + 1, practiceRounds); // jump past practice

        if (delay > 0f) yield return new WaitForSeconds(delay);

        ToggleFeedbackPanel(false);
        if (clientBox) clientBox.SetActive(false);

        PickTwoClients();

        if (clientChoicePanel)
        {
            clientChoicePanel.SetActive(true);
            if (clientChoicePromptText)
                clientChoicePromptText.text = "Which customer do you want to help first?";
        }
    }

    public void ShowFeedbackLine(string message) => EnsureFeedbackVisible(message);

    void EnsureFeedbackVisible(string message)
{
    if (!feedbackText) return;

    // parent (your FeedbackPanel object)
    var parentGO = feedbackText.transform.parent ? feedbackText.transform.parent.gameObject : null;

    // 1) Make sure the whole panel is ON and in front
    if (clientBox) clientBox.SetActive(true);
    if (parentGO) parentGO.SetActive(true);
    parentGO?.transform.SetAsLastSibling();   // bring to front over other UI

    // 2) Force CanvasGroup back to visible (tutorial may have faded to 0)
    var cg = feedbackGroup ? feedbackGroup : parentGO ? parentGO.GetComponent<CanvasGroup>() : null;
    if (cg)
    {
        cg.alpha = 1f;
        cg.interactable = true;     // optional
        cg.blocksRaycasts = false;  // usually off for a speech bubble
    }

    // 3) Make sure the TMP text itself isn’t hidden
    feedbackText.enabled = true;
    var col = feedbackText.color; col.a = 1f; feedbackText.color = col;

    // 4) Update text and flush
    feedbackText.gameObject.SetActive(true);
    feedbackText.text = message;
    feedbackText.ForceMeshUpdate();

    // Debug breadcrumb (remove later if you want)
    // Debug.Log("[Feedback] Resurrected bubble and set: " + message);
}


    public void SetFeedback(string msg) => EnsureFeedbackVisible(msg);

    public void SetSelectedClient(ClientData client)
    {
        selectedClient = client;
        if (clientChoicePanel) clientChoicePanel.SetActive(false);
        StartNewRound();
    }

    IEnumerator WaitThenShowBowChoice(float delay) { yield return new WaitForSeconds(delay); ShowBowChoice(); }
    IEnumerator WaitThenNextClient(float delay)    { yield return new WaitForSeconds(delay); ContinueToNextClient(); }
    IEnumerator UnlockClickAfter(float delay)      { yield return new WaitForSeconds(delay); clickLocked = false; }

    // ---------------- Round lifecycle ----------------------------------------
    public void StartNewRound()
    {
        retried = false;
        clickLocked = false;
        bowChoiceScheduled = false;
        bowChoiceShownThisRound = false;
        nextClientScheduled  = false;

        tutorialFlowerCount = 0;

        if (bowChoicePanel) bowChoicePanel.SetActive(false);
        wreathFX?.Stop();

        if (IsPracticeRound)
            selectedClient = tutorialClient != null ? tutorialClient : AutoSelectPracticeClient();

        requestedCounts = levels[currentRound].fixedRequest;

        ShowClientRequestUI();
        if (IsPracticeRound && clientBox) clientBox.SetActive(false); // tutorial reveals later

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

        EnsureFeedbackVisible(IsPracticeRound
            ? "Practice: drag a flower onto the wreath to begin."
            : initialFeedbackMessage);

        if (currentRound < levelWreathPreviews.Count && wreathPreviewImage != null)
        {
            wreathPreviewImage.sprite = levelWreathPreviews[currentRound];
            wreathPreviewImage.gameObject.SetActive(true);
        }
        else if (wreathPreviewImage != null)
        {
            wreathPreviewImage.gameObject.SetActive(false);
        }

        GameEvents.RoundStarted?.Invoke();

        if (IsPracticeRound && tutorialDirector != null)
            tutorialDirector.Begin();
    }

    /// Called by Draggable when a flower is placed.
    public void NotifyFlowerPlaced()
    {
        tutorialFlowerCount++;

        if (tutorialFlowerCount == 1)
            GameEvents.FirstFlowerPlaced?.Invoke();   // tutorial step: "try dragging one more"
        else if (tutorialFlowerCount == 2)
            GameEvents.SecondFlowerPlaced?.Invoke();  // tutorial step: reveal request -> chooser
    }

    /// Back-compat: older script name
    public void NotifyFirstFlowerPlaced() => NotifyFlowerPlaced();

    void ShowClientRequestUI()
    {
        if (selectedClient == null)
        {
            if (requestText) requestText.text = "No client selected.";
            if (clientBox) clientBox.SetActive(true);
            return;
        }

        string requestStr = $"Hi, I'm {selectedClient.clientName}.\nI want my wreath:\n";
        foreach (var kv in requestedCounts)
        {
            string fraction = FractionFromCount(kv.Value);
            requestStr += $"- {fraction} {kv.Key}\n";
        }
        if (requestText) requestText.text = requestStr;

        if (clientImageDisplay) clientImageDisplay.sprite = selectedClient.clientImage;

        if (clientBox) clientBox.SetActive(true);
    }

    void ShowFeedback(string message) => EnsureFeedbackVisible(message);

    // ---------------- Bow choice ---------------------------------------------
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

        bowChoicePanel.SetActive(true);

        if (bowAImage) bowAImage.sprite = bowOptionA.bowSprite;
        if (bowBImage) bowBImage.sprite = bowOptionB.bowSprite;

        var textA = bowAImage ? bowAImage.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        var textB = bowBImage ? bowBImage.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (textA) textA.gameObject.SetActive(false);
        if (textB) textB.gameObject.SetActive(false);

        EnsureFeedbackVisible(NextPickBowLine());
    }

    public void SelectBowA() => ApplyBowToWreath(bowOptionA);
    public void SelectBowB() => ApplyBowToWreath(bowOptionB);

    void ApplyBowToWreath(BowData selectedBow)
    {
        WreathZone.Instance.AttachBow(selectedBow.bowSprite);
        bowChoicePanel.SetActive(false);

        if (wreathFX == null)
            wreathFX = UnityEngine.Object.FindFirstObjectByType<WreathCelebration>(FindObjectsInactive.Include);

        wreathFX?.Play(celebrationDuration);

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

    // ---------------- Validation --------------------------------------------
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
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Yellow, 2 }, { FlowerColor.Purple, 4 }, { FlowerColor.Red, 4 }, { FlowerColor.Blue, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Blue, 3 }, { FlowerColor.Yellow, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 4 }, { FlowerColor.Blue, 4 }, { FlowerColor.Yellow, 4 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Blue, 3 }, { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Yellow, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 6 }, { FlowerColor.Red, 3 }, { FlowerColor.Blue, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Yellow, 4 }, { FlowerColor.Purple, 4 }, { FlowerColor.Red, 2 }, { FlowerColor.Blue, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Blue, 4 }, { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Yellow, 2 } }),
        };
    }

    public void DoneButtonPressed()
    {
        if (bowChoiceScheduled || bowChoiceShownThisRound) return;
        if (clickLocked) return;
        clickLocked = true;

        bool valid = ValidateWreath();

        if (valid)
        {
            ShowClientSuccess();

            if (!bowChoiceScheduled && !bowChoiceShownThisRound)
            {
                bowChoiceScheduled = true;
                StartCoroutine(WaitThenShowBowChoice(2f));
            }
        }
        else if (!retried)
        {
            retried = true;
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

    bool ValidateWreath()
    {
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

    void ContinueToNextClient()
    {
        currentRound++;

        if (currentRound >= maxRounds)
        {
            if (gameOverPanel) gameOverPanel.SetActive(true);
            EnsureFeedbackVisible("Great work—shop closed for today!");
            return;
        }

        PickTwoClients();

        if (IsPracticeRound && skipClientChoiceInPractice)
        {
            selectedClient = AutoSelectPracticeClient();
            if (clientChoicePanel) clientChoicePanel.SetActive(false);
            if (clientBox) clientBox.SetActive(false);
            StartNewRound();
        }
        else
        {
            if (clientChoicePanel) clientChoicePanel.SetActive(true);
            if (clientBox) clientBox.SetActive(false);
            EnsureFeedbackVisible("Choose your next customer!");
        }
    }

    ClientData AutoSelectPracticeClient()
    {
        if (forcedPracticeClient != null) return forcedPracticeClient;
        if (clientOptionA != null) return clientOptionA;
        if (allClients != null && allClients.Count > 0) return allClients[0];
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

    // Fallback if the tutorial ends without calling our chooser functions.
    public void OnTutorialFinished()
    {
        if (clientChoicePanel && !clientChoicePanel.activeInHierarchy)
            EndTutorialAndOpenClientChoice(true, 0f);
    }
}

// ------------------------------------------------------------

[System.Serializable]
public class LevelData
{
    public Dictionary<GameManager.FlowerColor, int> fixedRequest;
    public LevelData(Dictionary<GameManager.FlowerColor, int> request) { fixedRequest = request; }
}

// Keep this at the very end of the file.
public static class GameEvents
{
    public static Action RoundStarted;
    public static Action FirstFlowerPlaced;
    public static Action SecondFlowerPlaced;
}
