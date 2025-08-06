using UnityEngine;

[CreateAssetMenu(fileName = "NewBowData", menuName = "Data/BowData")]
public class BowData : ScriptableObject
{
    public Sprite bowSprite;
    public string bowName; // leave blank if you don't want to use it
}
