using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem;

public class StackManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerStackInfo
    {
        public GameObject playerObject;
        public PlayerInput playerInput;
        public ThirdPersonController controller;
        public PlayerInputHandler inputHandler;
        public int playerIndex;
        public bool isTop;
    }

    [Header("Stack Settings")]
    public GameObject stackedCharacterPrefab;
    public float stackHeightOffset = 1.5f;
    private bool unstackInProgress = false;
    public PlayerInputManager playerInputManager;

    [Header("Debug")]
    public bool stackActive = false;

    private PlayerStackInfo stackedBottomPlayer;
    private PlayerStackInfo stackedTopPlayer;

    private List<PlayerStackInfo> activePlayers = new List<PlayerStackInfo>();
    private GameObject currentStackedCharacter;
    private StackedController stackedController;
    private StackedInputHandler stackedInputHandler;


    public static StackManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterPlayer(GameObject player, int playerIndex)
    {
        PlayerStackInfo info = new PlayerStackInfo
        {
            playerObject = player,
            playerInput = player.GetComponent<PlayerInput>(),
            controller = player.GetComponent<ThirdPersonController>(),
            inputHandler = player.GetComponent<PlayerInputHandler>(),
            playerIndex = playerIndex,
            isTop = false
        };

        activePlayers.Add(info);
        //Debug.Log($"Player {playerIndex} registered for stacking");
    }

    private void Update()
    {
        if (activePlayers.Count < 2 || stackActive)
        {
            //UIManager.Instance.HideAllPrompts();
            return;
        }

        var p1 = activePlayers[0].playerObject.transform;
        var p2 = activePlayers[1].playerObject.transform;

        float dist = Vector3.Distance(p1.position, p2.position);

        if (dist < 3f)
        {
            activePlayers[0].playerObject.GetComponent<PlayerUI>().StackPromptDisplay(true);
            activePlayers[1].playerObject.GetComponent<PlayerUI>().StackPromptDisplay(true);
        }
        else
        {
            activePlayers[0].playerObject.GetComponent<PlayerUI>().StackPromptDisplay(false);
            activePlayers[1].playerObject.GetComponent<PlayerUI>().StackPromptDisplay(false);
        }
            
    }

    

    public void AttemptStack(GameObject requestingPlayer, GameObject otherPlayer)
    {
        if (stackActive)
        {
            Debug.Log("Already stacked!");
            return;
        }

        PlayerStackInfo bottomPlayer = null;
        PlayerStackInfo topPlayer = null;

        // Check for who is the requesting player
        foreach (var info in activePlayers)
        {
            if (info.playerObject == requestingPlayer)
                bottomPlayer = info;
            else if (info.playerObject == otherPlayer)
                topPlayer = info;
        }

        if (bottomPlayer == null || topPlayer == null)
        {
            Debug.LogError("Could not find both players for stacking");
            return;
        }

        // Checking player Distance here
        float distance = Vector3.Distance(bottomPlayer.playerObject.transform.position,
                                         topPlayer.playerObject.transform.position);

        if (distance > 3f)
        {
            Debug.Log("Players are too far apart to stack");
            //UIManager.Instance.ShowTooFarPrompt();
            return;
        }

        PerformStack(bottomPlayer, topPlayer);
    }


    private void PerformStack(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        ThirdPersonController.SetMovement(false);

        Vector3 stackPosition = bottomPlayer.playerObject.transform.position;
        stackPosition.y += stackHeightOffset;


        SetChildrenActive(bottomPlayer.playerObject, false);
        SetChildrenActive(topPlayer.playerObject, false);

        DisableComponents(bottomPlayer, topPlayer);

        currentStackedCharacter = Instantiate(stackedCharacterPrefab, stackPosition, Quaternion.identity);
        StackedController.SetMovement(false);

        stackedController = currentStackedCharacter.GetComponent<StackedController>();
        stackedInputHandler = currentStackedCharacter.GetComponent<StackedInputHandler>();
        stackedController.Initialize(bottomPlayer, topPlayer);

        stackedBottomPlayer = bottomPlayer;
        stackedTopPlayer = topPlayer;
        stackedBottomPlayer.isTop = false;
        stackedTopPlayer.isTop = true;

        // Visually hiding the mesh for the VFX 
        SetMeshesActive(currentStackedCharacter, false);

        // Play VFX Particle, then reveal stacked character
        StartCoroutine(PlaySmokeThenShowMeshes(currentStackedCharacter, "MagicPoof"));
        StartCoroutine(EnableStackedPlayersAfterVFX());

        stackActive = true;

        //Debug.Log("Stack formed successfully!");
    }

    public void SetMeshesActive(GameObject obj, bool active)
    {
        MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mesh in meshes)
            mesh.enabled = active;
    }



    private IEnumerator PlaySmokeThenShowMeshes(GameObject obj, string particleName)
    {
        Debug.Log("PlayerCheck: "+ obj);
        if (obj == null) yield break;

        ParticleSystem[] particles = null;


        try
        {
            particles = obj.GetComponentsInChildren<ParticleSystem>(true);
        }
        catch
        {
            yield break;
        }

        if (particles == null || particles.Length == 0)
            yield break;

        ParticleSystem triggerPS = null;

        foreach (var ps in particles)
        {
            if (ps == null || ps.gameObject == null)
                continue;

            ps.gameObject.SetActive(true);

            if (ps.name == particleName)
                triggerPS = ps;

            ps.Play();

            
        }

        // Wait until the trigger particle has started playing
        if (triggerPS != null && triggerPS.gameObject != null) // Check if triggerPS and its gameObject are not null
        {
            float startDelay = triggerPS.main.startDelay.constant;
            yield return new WaitForSeconds(startDelay + 0.05f);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        if (obj != null)
        {
            SetMeshesActive(obj, true);
        }
    }

    private IEnumerator EnablePlayersAfterVFX()
    {

        yield return new WaitForSeconds(0.5f);

        if (stackedBottomPlayer != null && stackedTopPlayer != null)
        {
            EnableComponents(stackedBottomPlayer, stackedTopPlayer);
        }

        ThirdPersonController.SetMovement(true);

        unstackInProgress = false;
        stackActive = false;
    }
    private IEnumerator EnableStackedPlayersAfterVFX()
    {
        yield return new WaitForSeconds(1.2f);
        StackedController.SetMovement(true);
    }


    private void SetChildrenActive(GameObject obj, bool active)
    {
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            obj.transform.GetChild(i).gameObject.SetActive(active);
        }
    }

    public void DisableComponents(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        playerInputManager.splitScreen = false;

       bottomPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       bottomPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;

       topPlayer.playerObject.GetComponent<CharacterController>().enabled = false;
       topPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = false;
    }

    public void EnableComponents(PlayerStackInfo bottomPlayer, PlayerStackInfo topPlayer)
    {
        playerInputManager.splitScreen = true;

        bottomPlayer.playerObject.GetComponent<CharacterController>().enabled = true;
        bottomPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = true;

        topPlayer.playerObject.GetComponent<CharacterController>().enabled = true;
        topPlayer.playerObject.GetComponent<ThirdPersonController>().enabled = true;
    }


    public void Unstack()
    {

        if (!stackActive || unstackInProgress)
            return;

        unstackInProgress = true;

        if (currentStackedCharacter == null)
        {
            stackActive = false;
            unstackInProgress = false;
            return;
        }

        // Safe check for destroyed object
        if (currentStackedCharacter == null)
        {
            stackActive = false;
            unstackInProgress = false;
            return;
        }

        Vector3 basePos = currentStackedCharacter.transform.position;

        float separation = 1.0f;

        Vector3 bottomPos = basePos + Vector3.left * separation;

        Vector3 topPos = basePos + Vector3.right * separation;

        if (stackedBottomPlayer.playerObject != null)
        {
            stackedBottomPlayer.playerObject.transform.position = bottomPos;
            SetChildrenActive(stackedBottomPlayer.playerObject, true);
            SetMeshesActive(stackedBottomPlayer.playerObject, true);
            StartCoroutine(PlaySmokeThenShowMeshes(stackedBottomPlayer.playerObject, "HitSmoke"));
        }

        if (stackedTopPlayer.playerObject != null)
        {
            stackedTopPlayer.playerObject.transform.position = topPos;
            SetChildrenActive(stackedTopPlayer.playerObject, true);
            SetMeshesActive(stackedTopPlayer.playerObject, true);
            StartCoroutine(PlaySmokeThenShowMeshes(stackedTopPlayer.playerObject, "HitSmoke"));
        }

        StartCoroutine(EnablePlayersAfterVFX());

        EnableComponents(activePlayers[0], activePlayers[1]);

        if (currentStackedCharacter != null)
            Destroy(currentStackedCharacter);

        currentStackedCharacter = null;
        stackActive = false;

        unstackInProgress = false;

        //Debug.Log("Unstacked");
    }
}