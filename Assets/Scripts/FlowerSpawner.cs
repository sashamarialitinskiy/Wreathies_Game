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
    public List<FlowerEntry> flowerPrefabs;   // Red, Blue, Green, Purple...
    public Transform flowerParent;
    public List<Transform> spawnPositions;    // 24 spawn points total

    [Header("Depth Settings")]
    [Tooltip("Z used for spawned flowers so clicks hit them before the wreath.")]
    public float flowerZ = -0.1f;

    private readonly List<GameObject> spawnedFlowers = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnAllFlowers()
    {
        ClearAllFlowers();

        const int positionsPerColor = 6;

        for (int i = 0; i < flowerPrefabs.Count; i++)
        {
            var entry = flowerPrefabs[i];

            for (int j = 0; j < positionsPerColor; j++)
            {
                int index = i * positionsPerColor + j;
                if (index >= spawnPositions.Count) continue;

                Transform pos = spawnPositions[index];

                // --- KEY CHANGE: force flower Z so it's in front of the wreath ---
                Vector3 p = pos.position;
                p.z = flowerZ;

                GameObject go = Instantiate(entry.prefab, p, Quaternion.identity, flowerParent);

                // Safety: if prefab messes with Z, force it again.
                var t = go.transform;
                t.position = new Vector3(t.position.x, t.position.y, flowerZ);
                // ---------------------------------------------------------------

                var data = go.GetComponent<FlowerData>();
                if (data) data.flowerColor = entry.color;

                spawnedFlowers.Add(go);
            }
        }
    }

    public void ClearAllFlowers()
    {
        foreach (var flower in spawnedFlowers)
            if (flower) Destroy(flower);
        spawnedFlowers.Clear();
    }
}
