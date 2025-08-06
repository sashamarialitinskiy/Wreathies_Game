using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClientChoicePanel : MonoBehaviour
{
    [Header("Button A")]
    public Image imageA;
    public TextMeshProUGUI nameA;
    public Button buttonA;

    [Header("Button B")]
    public Image imageB;
    public TextMeshProUGUI nameB;
    public Button buttonB;

    private void OnEnable()
    {
        // Automatically update buttons when panel becomes active
        ShowClients(GameManager.Instance.clientOptionA, GameManager.Instance.clientOptionB);
    }

    public void ShowClients(ClientData a, ClientData b)
    {
        imageA.sprite = a.clientImage;
        nameA.text = a.clientName;
        buttonA.onClick.RemoveAllListeners();
        buttonA.onClick.AddListener(() => GameManager.Instance.SetSelectedClient(a));

        imageB.sprite = b.clientImage;
        nameB.text = b.clientName;
        buttonB.onClick.RemoveAllListeners();
        buttonB.onClick.AddListener(() => GameManager.Instance.SetSelectedClient(b));
    }
}
