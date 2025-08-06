using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Wreath Preview")]
    public Image wreathPreviewImage;
    public List<Sprite> levelWreathPreviews; // Assign 5 in Inspector


    [Header("Client UI")]
    public GameObject clientBox;
    public TextMeshProUGUI requestText;
    public Image clientImageDisplay;
    public TextMeshProUGUI clientChoicePromptText;

    [Header("Client Info")]
    public ClientData selectedClient;
    public List<ClientData> allClients;
    [HideInInspector] public ClientData clientOptionA;
    [HideInInspector] public ClientData clientOptionB;

    [Header("Round Control")]
    public int currentRound = 0;
    public int maxRounds = 10;
    public int totalSlots = 12;
    private bool retried = false;
    private bool waitingForPostBowConfirm = false;

    [Header("Request Info")]
    public Dictionary<FlowerColor, int> requestedCounts;

    [Header("Panels")]
    public GameObject clientChoicePanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI feedbackText;

    [Header("Bow Selection")]
    public GameObject bowChoicePanel;
    public Image bowAImage;
    public Image bowBImage;
    public List<BowData> allBows;
    private BowData bowOptionA;
    private BowData bowOptionB;

    private List<LevelData> levels = new List<LevelData>();

    public enum FlowerColor { Red, Blue, Green, Purple }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        BuildLevels();
        PickTwoClients();

        clientChoicePanel.SetActive(true);
        clientBox.SetActive(false);

        WreathZone.Instance.ClearWreath();
        FlowerSpawner.Instance.SpawnAllFlowers();
    }

    public void SetSelectedClient(ClientData client)
    {
        selectedClient = client;
        Debug.Log("Selected client: " + selectedClient.clientName);
        clientChoicePanel.SetActive(false);
        StartNewRound();
    }

    IEnumerator WaitThenNextRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowBowChoice();
    }

   public void StartNewRound()
{
    if (feedbackText != null)
    {
        feedbackText.gameObject.SetActive(false);
        if (feedbackText.transform.parent != null)
            feedbackText.transform.parent.gameObject.SetActive(false);
    }

    retried = false;
    requestedCounts = levels[currentRound].fixedRequest;
    ShowClientRequestUI();
    WreathZone.Instance.ClearWreath();
    FlowerSpawner.Instance.SpawnAllFlowers();

    // 🖼 Show wreath preview for first 5 levels (or however many you've assigned)
    if (currentRound < levelWreathPreviews.Count && wreathPreviewImage != null)
    {
        wreathPreviewImage.sprite = levelWreathPreviews[currentRound];
        wreathPreviewImage.gameObject.SetActive(true);
    }
    else if (wreathPreviewImage != null)
    {
        wreathPreviewImage.gameObject.SetActive(false);
    }
}


    void ShowClientRequestUI()
    {
        string requestStr = $"Hi, I'm {selectedClient.clientName}.\nI want my wreath:\n";

        foreach (var kv in requestedCounts)
        {
            string fraction = FractionFromCount(kv.Value);
            requestStr += $"- {fraction} {kv.Key}\n";
        }

        requestText.text = requestStr;

        if (clientImageDisplay != null)
            clientImageDisplay.sprite = selectedClient.clientImage;

        clientBox.SetActive(true);
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            if (feedbackText.transform.parent != null)
                feedbackText.transform.parent.gameObject.SetActive(true);
            feedbackText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("FeedbackText not assigned!");
        }
    }

    void ShowBowChoice()
    {
        bowChoicePanel.SetActive(true);

        List<BowData> shuffled = new(allBows);
        ShuffleList(shuffled);

        bowOptionA = shuffled[0];
        bowOptionB = shuffled[1];

        bowAImage.sprite = bowOptionA.bowSprite;
        bowBImage.sprite = bowOptionB.bowSprite;

        
        var textA = bowAImage.GetComponentInChildren<TextMeshProUGUI>(true);
        var textB = bowBImage.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textA) textA.gameObject.SetActive(false);
        if (textB) textB.gameObject.SetActive(false);
    }

    public void SelectBowA() => ApplyBowToWreath(bowOptionA);
    public void SelectBowB() => ApplyBowToWreath(bowOptionB);

    void ApplyBowToWreath(BowData selectedBow)
    {
        WreathZone.Instance.AttachBow(selectedBow.bowSprite);
        bowChoicePanel.SetActive(false);
        waitingForPostBowConfirm = true;
    }

    string FractionFromCount(int count)
    {
        return count switch
        {
            6 => "1/2",
            4 => "1/3",
            3 => "1/4",
            2 => "1/6",
            1 => "1/12",
            8 => "2/3",
            9 => "3/4",
            10 => "5/6",
            12 => "all",
            _ => $"{count}/12"
        };
    }

    void BuildLevels()
    {
        levels = new List<LevelData>
        {
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 6 }, { FlowerColor.Blue, 6 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Green, 6 }, { FlowerColor.Blue, 6 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 4 }, { FlowerColor.Red, 4 }, { FlowerColor.Blue, 4 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Green, 4 }, { FlowerColor.Purple, 8 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Red, 3 }, { FlowerColor.Purple, 3 }, { FlowerColor.Blue, 3 }, { FlowerColor.Green, 3 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Blue, 2 }, { FlowerColor.Red, 10 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Green, 8 }, { FlowerColor.Purple, 2 }, { FlowerColor.Red, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Green, 3 }, { FlowerColor.Red, 3 }, { FlowerColor.Purple, 6 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Purple, 6 }, { FlowerColor.Red, 2 }, { FlowerColor.Blue, 2 }, { FlowerColor.Green, 2 } }),
            new(new Dictionary<FlowerColor, int> { { FlowerColor.Green, 2 }, { FlowerColor.Blue, 2 }, { FlowerColor.Purple, 8 } })
        };
    }

    public void DoneButtonPressed()
    {
        if (waitingForPostBowConfirm)
        {
            waitingForPostBowConfirm = false;
            ContinueToNextClient();
            return;
        }

        if (ValidateWreath())
        {
            ShowClientSuccess();
            StartCoroutine(WaitThenNextRound(2f));
        }
        else if (!retried)
        {
            retried = true;
            ShowFeedback("Hmm... this isn’t what I asked for. Try again.");
        }
        else
        {
            ShowClientFinalReaction();
            StartCoroutine(WaitThenNextRound(2f));
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

    void ShowClientSuccess() => ShowFeedback("Wow, that’s beautiful! Thank you.");
    void ShowClientFinalReaction() => ShowFeedback("This isn’t quite right, but I’ll take it anyway.");

    void NextRound() => ContinueToNextClient();

    void ShuffleList<T>(List<T> list)
{
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = Random.Range(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}


    void PickTwoClients()
{
    if (allClients.Count < 2)
    {
        Debug.LogError("Not enough clients in the list!");
        return;
    }

    List<ClientData> shuffled = new(allClients);
    ShuffleList(shuffled);

    clientOptionA = shuffled[0];
    clientOptionB = shuffled[1];

    // Set client choice prompt based on current round
    if (clientChoicePromptText != null)
    {
        if (currentRound == 0)
            clientChoicePromptText.text = "Which customer do you want to help first?";
        else
            clientChoicePromptText.text = "Which customer do you want to help next?";
    }
}


    void ContinueToNextClient()
    {
        currentRound++;
        if (currentRound >= maxRounds)
        {
            Debug.Log("Game Over!");
            gameOverPanel.SetActive(true);
        }
        else
        {
            PickTwoClients();
            clientChoicePanel.SetActive(true);
            clientBox.SetActive(false);
        }
    }
}

[System.Serializable]
public class LevelData
{
    public Dictionary<GameManager.FlowerColor, int> fixedRequest;

    public LevelData(Dictionary<GameManager.FlowerColor, int> request)
    {
        fixedRequest = request;
    }
}

