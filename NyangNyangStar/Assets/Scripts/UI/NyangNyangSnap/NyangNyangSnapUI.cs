using Core.Managers;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapUI : UIPopup
{
    [Tooltip("시작 패널")][SerializeField] private GameObject _startPanel;
    [Tooltip("시작 버튼")][SerializeField] private GameObject _startButton;

    [Header("버튼")]
    [Tooltip("뒤로가기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("사진 버튼")][SerializeField] private Button _photoButton;
    [Tooltip("세팅 버튼")][SerializeField] private Button _settingsButton;
    [Tooltip("간식 패널 버튼")][SerializeField] private Button _snackPanelButton;
    [Tooltip("장난감 패널 버튼")][SerializeField] private Button _toyPanelButton;

    private NyangNyangSnapSprite _sprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapButtons));
        _backButton = Get<Button>((int)NyangNyangSnapButtons.BackButton);
        _photoButton = Get<Button>((int)NyangNyangSnapButtons.PhotoButton);
        _settingsButton = Get<Button>((int)NyangNyangSnapButtons.SettingsButton);
        _snackPanelButton = Get<Button>((int)NyangNyangSnapButtons.SnackPanelButton);
        _toyPanelButton = Get<Button>((int)NyangNyangSnapButtons.ToyPanelButton);

        InitPopups();

        _sprite = GetComponent<NyangNyangSnapSprite>();
        _sprite.Init();
    }

    private void InitPopups()
    {
        //InitPopup(KeyContainer.Prefabs.NyangNyangSnapPopupUI, _photoButton);
        AddCloseNyangNyangSnapButton(_backButton);
        InitPopup(KeyContainer.Prefabs.SettingsPopupUI, _settingsButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapSnackPopupUI, _snackPanelButton);
        InitPopup(KeyContainer.Prefabs.NyangNyangSnapToyPopupUI, _toyPanelButton);


    }

    private void OnDisable()
    {
        //RemovePopupButton(_photoButton);
        //RemovePopupButton(_backButton);
        //RemovePopupButton(_settingsButton);
        //RemovePopupButton(_snackPanelButton);
        //RemovePopupButton(_toyPanelButton);
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
    private void AddCloseNyangNyangSnapButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public void OpenPopup(int stage)
    {
        _sprite.SetBackground(stage);
        gameObject.SetActive(true);
        _startPanel.SetActive(true);
        _startButton.SetActive(true);
    }
}

public enum NyangNyangSnapButtons
{
    BackButton,
    PhotoButton,
    SettingsButton,
    SnackPanelButton,
    ToyPanelButton
}
