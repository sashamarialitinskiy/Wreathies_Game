using System.Collections.Generic;
using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    public static FlowerSpawner Instance;   // Quick way for other scripts to find this spawner

    [System.Serializable]
    public class FlowerEntry
    {
        public GameManager.FlowerColor color;  // Which color this flower is (Red/Blue/Yellow/Purple)
        public GameObject prefab;              // The prefab to spawn for that color
    }

    [Header("Flower Settings")]
    public List<FlowerEntry> flowerPrefabs;   // The menu of flower types we can spawn (one entry per color)
    public Transform flowerParent;            // A container in the hierarchy to keep spawned flowers organized
    public List<Transform> spawnPositions;    // Exact positions on the table where flowers appear (should hold 24)

    [Header("Depth Settings")]
    [Tooltip("Z used for spawned flowers so clicks hit them before the wreath.")]
    public float flowerZ = -0.1f;             // How “in front” the flowers sit in 2D space (so they’re easy to grab)

    private readonly List<GameObject> spawnedFlowers = new(); // We remember what we spawned so we can clear it later

    private void Awake()
    {
        // Make this easy to access from anywhere: FlowerSpawner.Instance
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // Safety: if a duplicate appears, remove it
    }

    public void SpawnAllFlowers()
    {
        // Start fresh each round: remove anything from the last round
        ClearAllFlowers();

        const int positionsPerColor = 6; // We place 6 flowers for each color

        // Go through each flower color we’ve set up in the Inspector
        for (int i = 0; i < flowerPrefabs.Count; i++)
        {
            var entry = flowerPrefabs[i];

            // Spawn 6 flowers of this color
            for (int j = 0; j < positionsPerColor; j++)
            {
                int index = i * positionsPerColor + j;     // Which spawn point to use
                if (index >= spawnPositions.Count) continue; // Guard: do nothing if we run out of positions

                Transform pos = spawnPositions[index];

                // Put the flower slightly “in front” of the wreath so it’s easy to click/tap
                Vector3 p = pos.position;
                p.z = flowerZ;

                // Create the flower at that spot under the parent container
                GameObject go = Instantiate(entry.prefab, p, Quaternion.identity, flowerParent);

                // Extra safety: some prefabs might change their own Z; force it back
                var t = go.transform;
                t.position = new Vector3(t.position.x, t.position.y, flowerZ);

                // Tag the spawned object with its color so other scripts know what it is
                var data = go.GetComponent<FlowerData>();
                if (data) data.flowerColor = entry.color;

                // Track it so we can delete all flowers quickly when the round resets
                spawnedFlowers.Add(go);
            }
        }
    }

    public void ClearAllFlowers()
    {
        // Delete everything we spawned last time
        foreach (var flower in spawnedFlowers)
            if (flower) Destroy(flower);

        // Empty our list so we’re ready to spawn a fresh set
        spawnedFlowers.Clear();
    }
}

