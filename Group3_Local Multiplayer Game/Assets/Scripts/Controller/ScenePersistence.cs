using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePersistenceManager : MonoBehaviour
{
    public static ScenePersistenceManager Instance;
    public string PreviousSceneName { get; private set; }
    private string currentSceneName;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        currentSceneName = SceneManager.GetActiveScene().name;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        PreviousSceneName = currentSceneName;
        currentSceneName = scene.name;

        Debug.Log("Came from: " + PreviousSceneName + ", arrived in: " + currentSceneName);
    }
}
