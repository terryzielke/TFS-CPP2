using System.Collections.Generic;
using UnityEngine;

// Spawns a number of prefabs at random SpawnObject points, one prefab per point.
public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private int spawnCount = 3;

    private void Start()
    {
        if (objectPrefab == null)
        {
            Debug.LogWarning("ObjectSpawner has no prefab assigned.");
            return;
        }

        // Copy the points so shuffling doesn't touch the shared list
        List<SpawnObject> points = new List<SpawnObject>(SpawnObject.All);

        if (points.Count < spawnCount)
            Debug.LogWarning($"Only {points.Count} spawn points found, but {spawnCount} objects were requested.");

        // Fisher-Yates shuffle so each point is picked at most once
        for (int i = points.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (points[i], points[j]) = (points[j], points[i]);
        }

        int count = Mathf.Min(spawnCount, points.Count);
        for (int i = 0; i < count; i++)
        {
            SpawnObject point = points[i];
            Instantiate(objectPrefab, point.SpawnPosition, point.transform.rotation);
        }
    }
}
