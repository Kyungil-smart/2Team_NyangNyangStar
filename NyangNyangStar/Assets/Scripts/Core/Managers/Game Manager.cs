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

namespace Core.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance 
        {
            get
            {
                Init(); 
                return _instance;
            } 
        }

        private AudioManager _audioManager = new AudioManager();
        private GameSceneManager _gameSceneManager = new GameSceneManager();
        private AddressableManager _addressableManager = new AddressableManager();
    
        // TODO UI, SceneChangeManger, DataManager 추가

        public static AudioManager AudioManager => Instance._audioManager;
        public static GameSceneManager Scene => Instance._gameSceneManager;
        public static AddressableManager Addressable => Instance._addressableManager;
    

        private static void Init()
        {
            if (_instance != null) return;

            GameObject go = GameObject.Find("@GameManager");
        
            if (go == null)
            {
                go = new GameObject("@GameManager");
                go.AddComponent<GameManager>();

                _instance = go.GetComponent<GameManager>();
                DontDestroyOnLoad(go.gameObject);

                // Data.Init();
                Addressable.Init();
                Scene.Init();
                AudioManager.Init();
                // UI.Init();
            }    
            DebugTool.Log("모든 매니저 초기화 완료 ", DebugType.Game);
        }

        public static void Clear()
        {
            _instance._audioManager.Clear();
            _instance._gameSceneManager.Clear();
            _instance._addressableManager.Clear();
            
            DebugTool.Log("모든 매니저 제거 완료 ", DebugType.Game);
        }
    }
}

