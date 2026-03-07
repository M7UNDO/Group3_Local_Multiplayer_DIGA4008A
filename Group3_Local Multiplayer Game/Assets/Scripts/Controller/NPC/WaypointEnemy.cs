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

    [Header("Animation Settings")]
    public float walkAnimationSpeed = 1f; // Adjust this to match your walk animation
    public float runAnimationSpeed = 1f;  // Adjust this to match your run animation
    public float acceleration = 10f;      // How fast the animation blends

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
    private float currentSpeedPercent;
    private float targetSpeedPercent;

    private enum State { Patrol, Suspicious, Chase, Catch, Returning }
    private State currentState;

    public BackgroundMusicManager musicManager;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Configure NavMeshAgent for smooth movement
        agent.acceleration = 8f;
        agent.angularSpeed = 360f;

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

        HandleSuspicion();

        if (isAlerting)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            UpdateAnimation(0f); // Idle during alert
            return;
        }

        // State management
        if (!isCatching)
        {
            if (distanceToPlayer <= catchRange && currentState == State.Chase)
            {
                ChangeState(State.Catch);
            }
            else if (suspicion >= suspicionThreshold && !hasAlerted && !isAlerting)
            {
                StartCoroutine(AlertBeforeChase());
            }
            else
            {
                // Determine state based on suspicion
                State newState;
                if (currentState == State.Chase)
                {
                    newState = (suspicion <= suspicionThreshold * 0.3f) ? State.Suspicious : State.Chase;
                }
                else if (suspicion > 0)
                {
                    newState = State.Suspicious;
                }
                else
                {
                    newState = State.Patrol;
                }

                if (newState != currentState)
                    ChangeState(newState);
            }
        }

        // Execute current state behavior
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

        // Calculate target speed percentage based on state
        if (currentState == State.Chase)
        {
            targetSpeedPercent = 1f; // Run
        }
        else if (agent.velocity.magnitude > 0.1f)
        {
            targetSpeedPercent = 0.5f; // Walk
        }
        else
        {
            targetSpeedPercent = 0f; // Idle
        }

        UpdateAnimation(targetSpeedPercent);

        if (!isCatching && !isAlerting)
            RotateTowardsMovementDirection();
    }

    void UpdateAnimation(float targetSpeed)
    {
      
        currentSpeedPercent = Mathf.MoveTowards(currentSpeedPercent, targetSpeed,
            acceleration * Time.deltaTime);

  
        animator.SetFloat("Speed", currentSpeedPercent);

       
        if (currentState == State.Chase)
        {
     
            animator.speed = runAnimationSpeed;
            agent.speed = runSpeed;
        }
        else if (agent.velocity.magnitude > 0.1f)
        {
            
            animator.speed = walkAnimationSpeed;
            agent.speed = walkSpeed;
        }
        else
        {
      
            animator.speed = 1f;
        }
    }

    void ChangeState(State newState)
    {
        if (currentState == newState) return;

      
        Debug.Log($"Changing state from {currentState} to {newState}");

        isIdle = false;
        currentState = newState;


        switch (newState)
        {
            case State.Chase:
                agent.isStopped = false;

                if (musicManager != null)
                    musicManager.EnemyStartedChase();

                break;

            case State.Patrol:
            case State.Suspicious:
            case State.Returning:

                agent.isStopped = false;
                if (musicManager != null)
                    musicManager.EnemyStoppedChase();

                break;

            case State.Catch:

                agent.isStopped = true;

                if (currentState == State.Chase && musicManager != null)
                    musicManager.EnemyStoppedChase();

                break;
        }
    }

    IEnumerator AlertBeforeChase()
    {
        isAlerting = true;
        hasAlerted = true;

        
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        UpdateAnimation(0f);

     
        animator.SetTrigger("Alert");

        if (alertGrunt != null)
            AudioSource.PlayClipAtPoint(alertGrunt, transform.position);

        yield return new WaitForSeconds(alertDuration);


        agent.isStopped = false;
        ChangeState(State.Chase);
        isAlerting = false;
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

        if (suspicion <= suspicionThreshold * 0.25f)
        {
            hasAlerted = false;
        }
    }

    void Patrol()
    {
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

        agent.isStopped = true;
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
        targetLookRotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
    }

    void RotateTowardsMovementDirection()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
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

    public bool IsChasing()
    {
        return currentState == State.Chase;
    }
}