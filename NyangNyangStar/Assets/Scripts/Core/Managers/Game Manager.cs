using UnityEngine;

/*
@Managers (GameObject, DontDestroyOnLoad)
└── Managers.cs  ← 단일 진입점 싱글톤

서브 매니저들 (Plain C# Class, ISubManager)
├── AddressableManager
├── AudioManager
├── DataManager
└── UIManager
*/

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance { get { Init(); return _instance; } }

    private AudioManager _audio = new AudioManager();
    private GameSceneManager _gameSceneManager = new GameSceneManager();
    
    // TODO UI, SceneChangeManger, DataManager 추가

    public static AudioManager Audio => Instance._audio;
    public static GameSceneManager Scene => Instance._gameSceneManager;

    private static void Init()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("@GameManager");
        
        if (go == null)
        {
            go = new GameObject("@GameManager");
            go.AddComponent<GameManager>();

            _instance = go.GetComponent<GameManager>();
            DontDestroyOnLoad(go.gameObject);

            // Data.Init();
            Scene.Init();
            Audio.Init();
            // UI.Init();
        }    
    }

    public static void clear()
    {
        Audio.Clear();
        Scene.Clear();
    }
}

