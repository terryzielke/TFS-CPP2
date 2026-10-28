using UnityEngine;

// Floating ghost enemy. Wanders around where it spawned, chases the closest player
// that comes near, and hits players it touches. The idle clip handles the up/down bob.
[RequireComponent(typeof(Rigidbody))]
public class GhostEnemy : MonoBehaviour
{
    // Component references
    private Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 360f; // degrees per second
    [Header("Chase Settings")]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float loseRadius = 25f; // bigger than detectionRadius so it doesn't flicker at the edge
    [SerializeField] private float facingAngle = 30f; // degrees either side of forward that count as "facing"
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [Header("Visibility Settings")]
    [SerializeField] private float fadeSpeed = 2f; // alpha per second

    private Transform target;
    private bool isSeenByPlayer = false;
    private bool isFacingPlayer = false;
    private Renderer[] renderers;
    private float currentAlpha = 1f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Ghosts float, and we steer rotation ourselves
        rb.useGravity = false;
        rb.freezeRotation = true;
        // Get all renderers in this object and its children so we can fade them out
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        // Fade out when not facing the player, fade back in when facing them
        float targetAlpha = isFacingPlayer ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                Color color = mat.color; // maps to _BaseColor on URP Lit
                color.a = currentAlpha;
                mat.color = color;
            }
        }
    }

    private void FixedUpdate()
    {
        // (1) Is a player inside the detection radius?
        target = FindClosestPlayer();

        if (target == null)
        {
            isFacingPlayer = false;
            isSeenByPlayer = false;
            return;
        }

        // (2) Is the ghost facing the player?
        isFacingPlayer = IsFacing(transform, target.position);

        // (4) Is the player facing the ghost?
        isSeenByPlayer = IsFacing(target, transform.position);

        // (3) Turn toward and move toward the player
        MoveTowardTarget();
    }

    // Returns the closest player within detectionRadius, or null if none are close enough
    private Transform FindClosestPlayer()
    {
        Transform closest = null;
        float closestDistance = detectionRadius;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closest = player.transform;
            }
        }

        return closest;
    }

    // True if 'looker' is pointing at 'point' within facingAngle (ignores height difference)
    private bool IsFacing(Transform looker, Vector3 point)
    {
        Vector3 toPoint = point - looker.position;
        toPoint.y = 0f;

        Vector3 forward = looker.forward;
        forward.y = 0f;

        return Vector3.Angle(forward, toPoint) <= facingAngle;
    }

    private void MoveTowardTarget()
    {

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // stay at our own height

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, lookRotation, turnSpeed * Time.fixedDeltaTime));

        // if isSeenByPlayer is true, we don't move toward the player, but we still turn to face them
        if (isSeenByPlayer) return;

        rb.MovePosition(rb.position + direction.normalized * movementSpeed * Time.fixedDeltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
