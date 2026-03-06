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

    [Header("Detection Memory")]
    public float loseSightDelay = 1.5f;
    private float loseSightTimer;

    [Header("Alert Settings")]
    public float alertDuration = 1.5f;
    public AudioClip alertGrunt;

    private bool isAlerting;
    private bool hasAlerted;

    [Header("Catch Settings")]
    public float catchRange = 2f;
    public float restartDelay = 2f;
    public GameObject restartScreen;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 7f;
    public float maxChaseDistance = 25f;

    [Header("Idle Settings")]
    public float idleTime = 2f;
    public float lookAroundAngle = 60f;
    public float lookSpeed = 3f;

    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

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

    private Vector3 lastKnownPlayerPosition;

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

        ChangeState(State.Patrol);
        GoToNextWaypoint();
    }

    void Update()
    {
        if (gameOver || waypoints == null || waypoints.Length < 2)
            return;

        currentTarget = DetectPlayer();

        if (currentTarget != null)
            lastKnownPlayerPosition = currentTarget.position;

        float distanceToPlayer = currentTarget ?
            Vector3.Distance(transform.position, currentTarget.position) :
            Mathf.Infinity;

        float distanceFromStart = Vector3.Distance(transform.position, patrolStartPoint);

        HandleSuspicion();

        if (!isCatching)
        {
            if (distanceToPlayer <= catchRange && currentState == State.Chase)
            {
                ChangeState(State.Catch);
            }
            else if (suspicion >= suspicionThreshold && !hasAlerted)
            {
                StartCoroutine(AlertBeforeChase());
            }
            else if (suspicion > 0 && currentState != State.Chase)
            {
                ChangeState(State.Suspicious);
            }
            else if (distanceFromStart > maxChaseDistance)
            {
                ChangeState(State.Returning);
            }
            else if (suspicion <= 0)
            {
                ChangeState(State.Patrol);
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

        float speed = agent.velocity.magnitude;

        bool isMoving = speed > 0.15f;

        animator.SetBool("isWalking", isMoving && currentState != State.Chase);
        animator.SetBool("isRunning", isMoving && currentState == State.Chase);
        animator.SetBool("isIdle", !isMoving);

        if (!isCatching)
            RotateTowardsMovementDirection();
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

    IEnumerator AlertBeforeChase()
    {
        isAlerting = true;
        hasAlerted = true;

        agent.ResetPath();

        animator.SetTrigger("Alert");

        if (alertGrunt != null)
            AudioSource.PlayClipAtPoint(alertGrunt, transform.position);

        yield return new WaitForSeconds(alertDuration);

        ChangeState(State.Chase);

        isAlerting = false;
    }

    public bool IsChasing()
    {
        return currentState == State.Chase;
    }

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

    void HandleSuspicion()
    {
        if (currentTarget != null)
        {
            loseSightTimer = loseSightDelay;
            suspicion += suspicionIncreaseRate * Time.deltaTime;
        }
        else
        {
            loseSightTimer -= Time.deltaTime;

            if (loseSightTimer <= 0)
                suspicion -= suspicionDecreaseRate * Time.deltaTime;
        }

        suspicion = Mathf.Clamp(suspicion, 0, suspicionThreshold);
    }

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

    void Suspicious()
    {
        agent.speed = walkSpeed;
        isIdle = false;

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);
        }
    }

    void ReturnToPatrol()
    {
        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            suspicion = 0;
            hasAlerted = false;
            ChangeState(State.Patrol);
            GoToNextWaypoint();
        }
        else
        {
            agent.SetDestination(patrolStartPoint);
        }
    }

    void ChasePlayer()
    {
        agent.speed = runSpeed;

        if (currentTarget)
        {
            lastKnownPlayerPosition = currentTarget.position;
            agent.SetDestination(currentTarget.position);
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);

            if (!agent.pathPending && agent.remainingDistance < 1f)
            {
                suspicion = suspicionThreshold * 0.5f;
                ChangeState(State.Suspicious);
            }
        }
    }

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

    void StartIdle()
    {
        isIdle = true;
        idleTimer = idleTime;

        agent.ResetPath();

        float randomAngle = Random.Range(-lookAroundAngle, lookAroundAngle);

        targetLookRotation =
            Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
    }

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
                AudioSource.PlayClipAtPoint(
                    FootstepAudioClips[index],
                    transform.position,
                    FootstepAudioVolume
                );
            }
        }
    }
}