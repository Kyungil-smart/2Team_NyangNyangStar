using Core.Managers;
using UI;
using UnityEngine;
using Util;

public class MainUIInitializer : BaseScene
{
    private void Start()
    {
        GameManager.Data.LoadSheets();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI);
        }
    }
}
