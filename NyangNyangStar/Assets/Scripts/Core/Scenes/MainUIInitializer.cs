using Core.Managers;
using Data.LibrarySystem;
using System.Collections;
using UI;
using UI.Base;
using UI.Transition;
using UnityEngine;
using Util;

public class MainUIInitializer : BaseScene
{
    private void Start()
    {
        StartCoroutine(LoadMainUI());
    }

    private IEnumerator LoadMainUI()
    {
        while (LocalDataAccess.Instance == null ||
               LocalDataAccess.Instance.Game == null ||
               !LocalDataAccess.Instance.Game.IsReady)
        {
            yield return null;
        }

        GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI, _ =>
        {
            if (ScreenTransitionManager.Instance != null)
                ScreenTransitionManager.Instance.Reveal();
        });

        GameManager.Audio.PlayBgm("BGM_Main");
    }
}
