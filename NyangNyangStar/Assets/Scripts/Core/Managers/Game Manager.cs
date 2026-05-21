using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    
    [Header("매니저 프리팹")]
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private SceneChangeController _sceneChangeController;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Init()
    {
        GenerateManager<GameSceneManager>();
        GenerateManager(_audioManager);
        GenerateManager(_sceneChangeController);
    }

    private void Start()
    {
        DebugTool.Log("게임 시작", DebugType.Game, this);
        SceneChangeController.Instance.SetActivateImage(false);
    }

    private void GenerateManager<T>() where T : Component
    {
        if (FindAnyObjectByType<T>() != null)
        {
            DebugTool.Warning($"{typeof(T).Name} 을 로드하지 못했습니다.", DebugType.Game, this);
            return;
        }
        
        var go = new GameObject(typeof(T).Name);
        go.AddComponent<T>();
        DontDestroyOnLoad(go);
    }
    
    private void GenerateManager<T>(T managerPrefab) where T : Component
    {
        if (FindAnyObjectByType<T>() != null)
        {
            DebugTool.Warning($"{typeof(T).Name} 프리팹을 로드하지 못했습니다.", DebugType.Game, this);
            return;
        }
        
        T manager = Instantiate(managerPrefab);
        manager.gameObject.name = typeof(T).Name;
        DontDestroyOnLoad(manager.gameObject);
    }
}

