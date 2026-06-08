using Core.Managers;
using System.Collections.Generic;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapToyUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("장난감 버튼")][SerializeField] private Button _toyButton;
    [Tooltip("뒤로 가기 패널")][SerializeField] private Button _backPanel;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapToyButtons));
        _toyButton = Get<Button>((int)NyangNyangSnapToyButtons.ToyButton);
        _backPanel = Get<Button>((int)NyangNyangSnapToyButtons.BackPanel);


        InitPopups();
    }

    private void InitPopups()
    {
        //InitPopup(KeyContainer.Prefabs.NyangNyangSnapSnackPopupUI, _snackButton);
        if (_backPanel != null) _backPanel.onClick.AddListener(ClosePopup);

    }
    private void OnDisable()
    {
        //RemovePopupButton(_snackButton);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
        });
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }
    private void ClosePopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        gameObject.SetActive(false);
    }

}

public enum NyangNyangSnapToyButtons
{
    ToyButton,
    BackPanel
}
