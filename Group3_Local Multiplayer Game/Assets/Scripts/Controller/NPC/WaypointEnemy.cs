using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WaypointEnemy : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform waypointGroup;
    public AISensor sensor;

    [Header("Suspicion System")]
    public float suspicion = 0f;
    public float suspicionIncreaseRate = 25f;
    public float suspicionDecreaseRate = 10f;
    public float suspicionThreshold = 100f;

    [Header("Alert Settings")]
    public float alertDuration = 1.5f;
    public AudioClip alertGrunt;

    private bool isAlerting;

    [Header("Catch Settings")]
    public float catchRange = 2f;
    public float restartDelay = 2f;
    public GameObject restartScreen;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 7f;
    public float maxChaseDistance = 25f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Idle Settings")]
    public float idleTime = 2f;
    public float lookAroundAngle = 60f;
    public float lookSpeed = 3f;

    private NavMeshAgent agent;
    private Transform currentTarget;

    private bool isCatching;
    private bool gameOver;

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private Vector3 patrolStartPoint;

    private bool isIdle;
    private float idleTimer;
    private Quaternion targetLookRotation;

    private enum State { Patrol, Suspicious, Chase, Catch, Returning }
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

        HandleSuspicion();

        if (!isCatching)
        {
            if (distanceToPlayer <= catchRange && currentState == State.Chase)
            {
                currentState = State.Catch;
            }
            else if (suspicion >= suspicionThreshold && !isAlerting)
            {
                StartCoroutine(AlertBeforeChase());
            }
            else if (suspicion > 0)
            {
                currentState = State.Suspicious;
            }
            else if (distanceFromStart > maxChaseDistance)
            {
                currentState = State.Returning;
            }
            else
            {
                currentState = State.Patrol;
            }
        }

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Suspicious:
                Suspicious();
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

        animator.SetBool("isWalking", !isIdle && (currentState == State.Patrol || currentState == State.Returning));
        animator.SetBool("isRunning", currentState == State.Chase);
        animator.SetBool("isIdle", isIdle);

        if (!isCatching)
            RotateTowardsMovementDirection();
    }

    IEnumerator AlertBeforeChase()
    {
        isAlerting = true;

        agent.ResetPath();

        animator.SetTrigger("Alert");

        if (alertGrunt != null)
            AudioSource.PlayClipAtPoint(alertGrunt, transform.position);

        yield return new WaitForSeconds(alertDuration);

        currentState = State.Chase;

        isAlerting = false;
    }

    public bool IsChasing()
    {
        return currentState == State.Chase;
    }

    // ---------------- PLAYER DETECTION ----------------

    Transform DetectPlayer()
    {
        if (sensor == null || sensor.Objects.Count == 0)
            return null;

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject obj in sensor.Objects)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = obj.transform;
            }
        }

        return closest;
    }

    // ---------------- SUSPICION ----------------

    void HandleSuspicion()
    {
        if (currentTarget != null)
        {
            suspicion += suspicionIncreaseRate * Time.deltaTime;
        }
        else
        {
            suspicion -= suspicionDecreaseRate * Time.deltaTime;
        }

        suspicion = Mathf.Clamp(suspicion, 0, suspicionThreshold);
    }

    // ---------------- PATROL ----------------

    void Patrol()
    {
        agent.speed = walkSpeed;

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetLookRotation,
                Time.deltaTime * lookSpeed
            );

            if (idleTimer <= 0f)
            {
                isIdle = false;
                GoToNextWaypoint();
            }

            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartIdle();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    // ---------------- SUSPICIOUS ----------------

    void Suspicious()
    {
        agent.speed = walkSpeed;

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
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

        Vector3 lookPos = new Vector3(
            currentTarget.position.x,
            transform.position.y,
            currentTarget.position.z
        );

        transform.rotation = Quaternion.LookRotation(lookPos - transform.position);

        animator.SetTrigger("Attack");

        StartCoroutine(RestartGame());
    }

    IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(restartDelay);

        if (restartScreen != null)
            restartScreen.SetActive(true);
    }

    // ---------------- IDLE LOOK AROUND ----------------

    void StartIdle()
    {
        isIdle = true;
        idleTimer = idleTime;

        agent.ResetPath();

        float randomAngle = Random.Range(-lookAroundAngle, lookAroundAngle);

        targetLookRotation =
            Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
    }

    // ---------------- ROTATION ----------------

    void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(agent.velocity.normalized);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }
    }
}