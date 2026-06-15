using Data.LibrarySystem;
using UnityEngine;

namespace Core.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                Init();
                return _instance;
            }
        }

        private DataManager _dataManager = new();
        private AddressableManager _addressableManager = new();
        private AudioManager _audioManager = new();
        private GameSceneManager _gameSceneManager = new();
        private UiManager _uiManager = new();

        public static DataManager Data => Instance == null ? null : Instance._dataManager;
        public static AddressableManager Addressable => Instance == null ? null : Instance._addressableManager;
        public static GameSceneManager Scene => Instance == null ? null : Instance._gameSceneManager;
        public static AudioManager Audio => Instance == null ? null : Instance._audioManager;
        public static UiManager UI => Instance == null ? null : Instance._uiManager;

        private static bool _isQuitting;

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void GameQuit()
        {
            Clear();
            Application.Quit();
        }

        public static void Init()
        {
            if (_instance != null)
                return;

            GameObject go = GameObject.Find("@GameManager");

            if (go == null)
                go = new GameObject("@GameManager");

            _instance = go.GetComponent<GameManager>();

            if (_instance == null)
                _instance = go.AddComponent<GameManager>();

            DontDestroyOnLoad(go);

            DebugTool.Log("게임 매니저 초기화 시작", DebugType.Game);

            _instance._addressableManager.Init();
            _instance._dataManager.Init();
            _instance._audioManager.Init();
            _instance._gameSceneManager.Init();
            _instance._uiManager.Init();

            DebugTool.Log("모든 매니저 초기화 완료 ", DebugType.Game);
        }

        public static void ClearSession()
        {
            if (_instance == null)
                return;

            _instance._uiManager?.Clear();
            _instance._dataManager?.Clear();

            if (LocalDataAccess.Instance != null)
                LocalDataAccess.Instance.ClearSession();

            if (FireStoreManager.Instance != null)
                FireStoreManager.Instance.ClearSession();

            _instance._addressableManager?.Clear();

            _instance._addressableManager = new AddressableManager();
            _instance._dataManager = new DataManager();
            _instance._uiManager = new UiManager();

            _instance._addressableManager.Init();
            _instance._dataManager.Init();
            _instance._uiManager.Init();

            DebugTool.Log("세션 데이터 초기화 완료", DebugType.Game);
        }

        public static void Clear()
        {
            if (_instance == null)
                return;

            _instance._uiManager?.Clear();
            _instance._dataManager?.Clear();
            _instance._audioManager?.Clear();
            _instance._gameSceneManager?.Clear();
            _instance._addressableManager?.Clear();

            DebugTool.Log("모든 매니저 제거 완료 ", DebugType.Game);
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
