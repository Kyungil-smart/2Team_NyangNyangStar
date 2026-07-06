using Services.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Managers
{
    public class GameSceneManager : ISubManager
    {
        private GameObject _root;
    
        // 게임 씬 이동
        public void ChangeScene(SceneIndex index)
        {
            TryLoadScene((int)index);
        }

        public void LoadNextScene()
        {
            Time.timeScale = 1f;
            TryLoadScene(CurrentSceneIndex() + 1);
            DebugTool.Log("다음 씬으로 이동", DebugType.Game);
        }

        public void LoadPreviousScene()
        {
            Time.timeScale = 1f;
            TryLoadScene(CurrentSceneIndex() - 1);
        }

        // 씬 재시작
        public void ReloadScene()
        {
            TryLoadScene(CurrentSceneIndex());
        }

        public int CurrentSceneIndex()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }
    
        // 타이틀 씬 이동
        public void LoadTitle()
        {
            TryLoadScene((int)SceneIndex.TitleScene);
        }

        private bool TryLoadScene(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                DebugTool.Warning($"Invalid scene build index: {buildIndex}", DebugType.Game);
                return false;
            }

            SceneManager.LoadScene(buildIndex);
            return true;
        }

        public void Init()
        {
            _root = GameObject.Find("@Scene");

            if (_root == null)
            {
                _root = new GameObject { name = "@Scene" };
                Object.DontDestroyOnLoad(_root);
            }
            DebugTool.Log("게임 씬 매니저 초기화 완료 ", DebugType.Game);
        }

        public void Clear()
        {
            if (_root == null)
                return;
        
            Object.Destroy(_root);
            _root = null;
            
            DebugTool.Log("게임 씬 매니저 제거 완료 ", DebugType.Game);
        }
    }
}
