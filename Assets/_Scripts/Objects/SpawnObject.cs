using System.Collections.Generic;
using UnityEngine;

// Marks a spawn point. ObjectSpawner picks from these at the start of the level.
public class SpawnObject : MonoBehaviour
{
    // Every active spawn point in the scene
    public static readonly List<SpawnObject> All = new List<SpawnObject>();

    [SerializeField] private float spawnHeight = 1f;

    public Vector3 SpawnPosition => transform.position + Vector3.up * spawnHeight;

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(SpawnPosition, 0.25f);
        Gizmos.DrawLine(transform.position, SpawnPosition);
    }
}
