using Core.Managers;
using UI;
using UnityEngine;
using Util;

public class UITest : MonoBehaviour
{
    private bool Volume;
    private bool BGM;
    
    private void Start()
    {
        //GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.TestUI);
        GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI);
        //GameManager.Audio.PlayBgm(KeyContainer.Audio.TitleBGM);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            GameManager.UI.ClosePopupUI();
    }
}    
