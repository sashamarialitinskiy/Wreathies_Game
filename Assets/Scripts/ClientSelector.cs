using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClientSelector : MonoBehaviour
{
    [Header("Client UI References")]
    public Image clientAImage;
    public TextMeshProUGUI clientAText;
    public Button clientAButton;

    public Image clientBImage;
    public TextMeshProUGUI clientBText;
    public Button clientBButton;

    [Header("Available Clients")]
    public ClientData[] allClients;

    private ClientData clientA;
    private ClientData clientB;

    private void Start()
    {
        ShowRandomClients();
    }

    public void ShowRandomClients()
    {
        // Randomly pick 2 distinct clients
        clientA = allClients[Random.Range(0, allClients.Length)];
        do
        {
            clientB = allClients[Random.Range(0, allClients.Length)];
        } while (clientB == clientA);

        // Update UI
        clientAImage.sprite = clientA.clientImage;
        clientAText.text = clientA.clientName;

        clientBImage.sprite = clientB.clientImage;
        clientBText.text = clientB.clientName;

        // Hook up buttons
        clientAButton.onClick.RemoveAllListeners();
        clientAButton.onClick.AddListener(() => OnClientSelected(clientA));

        clientBButton.onClick.RemoveAllListeners();
        clientBButton.onClick.AddListener(() => OnClientSelected(clientB));
    }

    void OnClientSelected(ClientData selectedClient)
    {
        GameManager.Instance.SetSelectedClient(selectedClient);

        // Hide the choice panel after selection
        gameObject.SetActive(false);
    }
}



