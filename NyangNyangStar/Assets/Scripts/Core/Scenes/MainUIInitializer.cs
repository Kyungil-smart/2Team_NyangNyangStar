using Core.Managers;
using UI;
using UnityEngine;
using Util;

public class MainUIInitializer : MonoBehaviour
{
    private void Start()
    {
        GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI);
    }
}
