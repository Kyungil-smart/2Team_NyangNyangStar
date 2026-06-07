using Core.Managers;
using Data.LibrarySystem;
using System.Collections;
using UI;
using UnityEngine;
using Util;

public class MainUIInitializer : BaseScene
{
    private void Start()
    {
        GameManager.Data.LoadSheets();
        StartCoroutine(LoadMainUI());
    }

    private IEnumerator LoadMainUI()
    {
        while(LocalDataAccess.Instance == null ||
              LocalDataAccess.Instance.Game == null ||
              !LocalDataAccess.Instance.Game.IsReady)
            yield return null;
        
        GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI);
        GameManager.Audio.PlayBgm("Test_BGM");
    }
}
