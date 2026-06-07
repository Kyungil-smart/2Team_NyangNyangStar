using System;
using UnityEngine;

/*
@Managers (GameObject, DontDestroyOnLoad)
└── Managers.cs  ← 단일 진입점 싱글톤

서브 매니저들 (Plain C# Class, ISubManager)
├── DataManager
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
    
        // TODO : DataManager 추가

        public static DataManager Data => Instance == null ? null : Instance._dataManager;
        public static AddressableManager Addressable => Instance == null ? null : Instance._addressableManager;
        public static GameSceneManager Scene => Instance == null ? null : Instance._gameSceneManager;
        public static AudioManager Audio => Instance == null ? null : Instance._audioManager;
        public static UiManager UI => Instance == null ? null : Instance._uiManager;
        
        private static bool _isQuitting = false;

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
        
        public void GameQuit()
        {
            GameManager.Clear();
            Application.Quit();
        }

        public static void Init()
        {
            if (_instance != null) return;

            GameObject go = GameObject.Find("@GameManager");

            if (go == null)
                go = new GameObject("@GameManager");

            _instance = go.GetComponent<GameManager>();

            if(_instance == null)
                _instance = go.AddComponent<GameManager>();
            
            DontDestroyOnLoad(go.gameObject);
            
            DebugTool.Log("게임 매니저 초기화 시작", DebugType.Game);

            _instance._addressableManager.Init();
            _instance._dataManager.Init();
            _instance._audioManager.Init();
            _instance._gameSceneManager.Init();
            _instance._uiManager.Init();
            
            DebugTool.Log("모든 매니저 초기화 완료 ", DebugType.Game);
        }

        public static void Clear()
        {
            if (_instance == null)
                return;
            
            if(_instance._uiManager != null)
                _instance._uiManager.Clear();
            if(_instance._audioManager != null)
                _instance._audioManager.Clear();
            if(_instance._gameSceneManager != null)
                _instance._gameSceneManager.Clear();
            if(_instance._addressableManager != null)
                _instance._addressableManager.Clear();
            
            DebugTool.Log("모든 매니저 제거 완료 ", DebugType.Game);
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}

