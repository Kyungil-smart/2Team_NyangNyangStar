using Core.Managers;
using Services.AddressableKey;
using UI;
using UnityEngine;

public class UITest : MonoBehaviour
{
    private bool Volume;
    private bool BGM;
    
    private void Start()
    {
        //GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.TestUI);
        GameManager.UI.ShowSceneUI<UIScene>(KeyContainer.Prefabs.MainUI);
        GameManager.Audio.PlayBgm(KeyContainer.Audio.TitleBGM);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            Volume = !Volume;
            if(Volume)
                GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.VolumePopupUI);
            else
                GameManager.UI.ClosePopupUI(KeyContainer.Prefabs.VolumePopupUI);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            BGM = !BGM;
            if(BGM)
                GameManager.UI.ShowPopupUI<UIPopup>(KeyContainer.Prefabs.BGMPopupUI);
            else
                GameManager.UI.ClosePopupUI(KeyContainer.Prefabs.BGMPopupUI);
        }
        if(Input.GetKeyDown(KeyCode.Space))
            GameManager.UI.ClosePopupUI();
    }
}    
