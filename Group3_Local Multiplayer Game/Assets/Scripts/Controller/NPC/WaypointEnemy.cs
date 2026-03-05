using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WaypointEnemy : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform waypointGroup;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public float visionAngle = 60f;

    [Header("Layers")]
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Catch Settings")]
    public float catchRange = 2f;
    public float restartDelay = 2f;
    public GameObject restartScreen;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 7f;
    public float maxChaseDistance = 25f;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private bool isCatching;
    private bool gameOver;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Vector3 patrolStartPoint;

    private enum State { Patrol, Chase, Catch, Returning }
    private State currentState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (waypointGroup != null && waypointGroup.childCount > 1)
        {
            waypoints = new Transform[waypointGroup.childCount];
            for (int i = 0; i < waypointGroup.childCount; i++)
                waypoints[i] = waypointGroup.GetChild(i);

            patrolStartPoint = waypoints[0].position;
        }
        else
        {
            Debug.LogError("Waypoint group not assigned or has too few children.");
        }

        currentState = State.Patrol;
        GoToNextWaypoint();
    }

    void Update()
    {
        if (gameOver || waypoints == null || waypoints.Length < 2)
            return;

        currentTarget = DetectPlayer();

        float distanceToPlayer = currentTarget ?
            Vector3.Distance(transform.position, currentTarget.position) :
            Mathf.Infinity;

        float distanceFromStart = Vector3.Distance(transform.position, patrolStartPoint);

        // Determine AI state
        if (!isCatching)
        {
            if (distanceToPlayer <= catchRange)
                currentState = State.Catch;
            else if (currentTarget != null && distanceFromStart <= maxChaseDistance)
                currentState = State.Chase;
            else if (distanceFromStart > maxChaseDistance)
                currentState = State.Returning;
            else
                currentState = State.Patrol;
        }

        // Execute state behavior
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                ChasePlayer();
                break;

            case State.Catch:
                CatchPlayer();
                break;

            case State.Returning:
                ReturnToPatrol();
                break;
        }

        // Animator logic
        animator.SetBool("isWalking", currentState == State.Patrol || currentState == State.Returning);
        animator.SetBool("isRunning", currentState == State.Chase);

        if (!isCatching)
            RotateTowardsMovementDirection();
    }

    // ---------------- PLAYER DETECTION ----------------
    Transform DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);

        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Transform player = hit.transform;
            Vector3 playerCenter = player.position + Vector3.up * 1f;
            Vector3 dirToPlayer = (playerCenter - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, playerCenter);
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle > visionAngle / 2f)
                continue;

            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distance, obstacleMask))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }
        }

        return closestPlayer;
    }

    // ---------------- PATROL ----------------
    void Patrol()
    {
        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    // ---------------- RETURN ----------------
    void ReturnToPatrol()
    {
        agent.speed = walkSpeed;

        if (agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            currentState = State.Patrol;
            GoToNextWaypoint();
        }
        else
        {
            agent.SetDestination(patrolStartPoint);
        }
    }

    // ---------------- CHASE ----------------
    void ChasePlayer()
    {
        agent.speed = runSpeed;

        if (currentTarget && agent.isOnNavMesh)
            agent.SetDestination(currentTarget.position);
    }

    // ---------------- CAUGHT ----------------
    void CatchPlayer()
    {
        if (isCatching || currentTarget == null)
            return;

        isCatching = true;
        gameOver = true;
        agent.ResetPath();

        Vector3 lookPos = new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z);
        transform.rotation = Quaternion.LookRotation(lookPos - transform.position);

        animator.SetTrigger("Attack"); // Yelling animation

        StartCoroutine(RestartGame());
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(restartDelay);

        if (restartScreen != null)
            restartScreen.SetActive(true);
    }

    // ---------------- ROTATION ----------------
    void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Vision cone
        Vector3 left = Quaternion.Euler(0, -visionAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, visionAngle / 2, 0) * transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + left * detectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRadius);

        // Waypoints
        if (waypointGroup != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypointGroup.childCount; i++)
            {
                Transform wp = waypointGroup.GetChild(i);
                Gizmos.DrawWireSphere(wp.position, 0.4f);
                if (i + 1 < waypointGroup.childCount)
                    Gizmos.DrawLine(wp.position, waypointGroup.GetChild(i + 1).position);
            }
        }
    }
#endif
}