using UnityEngine;

[CreateAssetMenu(menuName = "Client/New Client")]
[System.Serializable]
public class ClientData : ScriptableObject
{
    public string clientName;
    public Sprite clientImage;
}


