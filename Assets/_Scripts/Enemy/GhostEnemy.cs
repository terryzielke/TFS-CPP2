using UnityEngine;

// Floating ghost enemy. Wanders around where it spawned, chases the closest player
// that comes near, and hits players it touches. The idle clip handles the up/down bob.
[RequireComponent(typeof(Rigidbody))]
public class GhostEnemy : MonoBehaviour
{

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 360f; // degrees per second
    [Header("Chase Settings")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float loseRadius = 25f; // bigger than detectionRadius so it doesn't flicker at the edge
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    private bool isSeenByPlayer = false;
    private bool isFacingPlayer = false;

    private void start()
    {
        // Initialize any necessary components or variables here

    }

}
