using Core.Managers;
using DG.Tweening;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NotebookPopupUI : UIPopup
{
    [Header("DOTween 설정")]
    [SerializeField] private Transform _panel;
    [SerializeField] private float _popupScale = 0.85f;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _background;

    [Header("고양이 상세보기 버튼")]
    [SerializeField] private Button _catBackground;

    public override void Init()
    {
        Bind<Button>(typeof(NotebookPopupButtons));

        _closeButton = GetButton((int)NotebookPopupButtons.CloseButton);
        _background = GetButton((int)NotebookPopupButtons.Background);
        _catBackground = GetButton((int)NotebookPopupButtons.CatBackground1);

        BindButtons();
        InitPopup(KeyContainer.Prefabs.SelectedCatPopupUI, _catBackground);
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseEmptyPopup);
        if (_background != null) _background.onClick.AddListener(CloseEmptyPopup);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_catBackground);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() =>
        {
            popup.gameObject.SetActive(true);
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
        });
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;

        _panel.localScale = Vector3.one * _popupScale;
        _panel.DOScale(1f, _popupScaleDuration)
            .SetEase(Ease.OutSine);
    }

    private void CloseEmptyPopup()
    {
        if (_panel == null) return;
        _panel.DOScale(Vector3.one * _popupScale, _popupScaleDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() => gameObject.SetActive(false));

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }
}

public enum NotebookPopupButtons
{
    CloseButton,
    Background,
    CatBackground1
}
