using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class WreathZone : MonoBehaviour
{
    public static WreathZone Instance;

    private List<GameObject> flowersInWreath = new List<GameObject>();
    private CircleCollider2D wreathCollider;

    [Header("Bow Placement")]
    public Transform bowAnchor;           
    public GameObject bowPrefab;          
    private GameObject currentBow;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        wreathCollider = GetComponent<CircleCollider2D>();

        if (wreathCollider == null)
        {
            Debug.LogError("WreathZone: Missing CircleCollider2D component!");
        }
        else
        {
            wreathCollider.isTrigger = true;
        }
    }

    public bool IsInsideWreath(Vector3 position)
    {
        return wreathCollider != null && wreathCollider.OverlapPoint(position);
    }

    public void AddFlower(GameObject flower)
    {
        if (!flowersInWreath.Contains(flower))
            flowersInWreath.Add(flower);
    }

    public void RemoveFlower(GameObject flower)
    {
        if (flowersInWreath.Contains(flower))
            flowersInWreath.Remove(flower);
    }

    public List<GameObject> GetFlowers()
    {
        return flowersInWreath;
    }

    public void ClearWreath()
    {
        foreach (var flower in flowersInWreath)
        {
            if (flower != null)
                Destroy(flower);
        }

        flowersInWreath.Clear();

        
        if (currentBow != null)
        {
            Destroy(currentBow);
            currentBow = null;
        }
    }

   public void AttachBow(Sprite bowSprite)
{
    if (bowAnchor == null || bowPrefab == null)
    {
        Debug.LogWarning("WreathZone: bowAnchor or bowPrefab is not assigned!");
        return;
    }

    // Remove existing bow
    if (currentBow != null)
    {
        Destroy(currentBow);
    }

    currentBow = Instantiate(bowPrefab, bowAnchor.position, Quaternion.identity, bowAnchor);

    // Set the sprite
    SpriteRenderer sr = currentBow.GetComponent<SpriteRenderer>();
    if (sr != null)
    {
        sr.sprite = bowSprite;

        currentBow.transform.localScale = new Vector3(0.4f, 3.5f, 1f); // 👈 Adjust this as needed
    }
    else
    {
        Debug.LogError("WreathZone: The bow prefab is missing a SpriteRenderer component!");
    }
}
}
