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
            SceneManager.LoadScene((int)index);
        }

        public void LoadNextStage()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(CurrentSceneIndex() + 1);
        }

        // 씬 재시작
        public void ReloadScene()
        {
            SceneManager.LoadScene(CurrentSceneIndex());
        }

        public int CurrentSceneIndex()
        {
            DebugTool.Log($"{SceneManager.GetActiveScene().buildIndex}", DebugType.Game);
            return SceneManager.GetActiveScene().buildIndex;
        }
    
        // 타이틀 씬 이동
        public void LoadTitle()
        {
            SceneManager.LoadScene((int)SceneIndex.TitleScene);
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
            
            DebugTool.Log("게임 씬 매니저 제거 완료 ", DebugType.Game);
        }
    }
}