using System.Collections.Generic;
using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    public static FlowerSpawner Instance;

    [System.Serializable]
    public class FlowerEntry
    {
        public GameManager.FlowerColor color;
        public GameObject prefab;
    }

    [Header("Flower Settings")]
    public List<FlowerEntry> flowerPrefabs; // e.g., Red, Blue, Green, Purple
    public Transform flowerParent;
    public List<Transform> spawnPositions;  // Should contain 24 spawn points total

    private List<GameObject> spawnedFlowers = new();

    private void Awake()
    {
        // Singleton pattern so GameManager can access FlowerSpawner.Instance
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SpawnAllFlowers()
    {
        ClearAllFlowers();

        int positionsPerColor = 6;

        for (int i = 0; i < flowerPrefabs.Count; i++)
        {
            var entry = flowerPrefabs[i];

            for (int j = 0; j < positionsPerColor; j++)
            {
                int index = i * positionsPerColor + j;

                if (index >= spawnPositions.Count)
                    continue;

                Transform pos = spawnPositions[index];
                GameObject go = Instantiate(entry.prefab, pos.position, Quaternion.identity, flowerParent);

                FlowerData data = go.GetComponent<FlowerData>();
                if (data != null)
                    data.flowerColor = entry.color;

                spawnedFlowers.Add(go);
            }
        }
    }

    public void ClearAllFlowers()
    {
        foreach (GameObject flower in spawnedFlowers)
        {
            if (flower != null)
                Destroy(flower);
        }

        spawnedFlowers.Clear();
    }
}
