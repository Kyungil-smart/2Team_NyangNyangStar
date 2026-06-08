using Core.Managers;
using DG.Tweening;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class EmptyPopupUI : UIPopup
{
    [Header("DOTween 설정")]
    [SerializeField] private Transform _panel;
    [SerializeField] private float _popupScale = 0.85f;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _background;

    public override void Init()
    {
        Bind<Button>(typeof(EmptyPopupButtons));

        _closeButton = GetButton((int)EmptyPopupButtons.CloseButton);
        _background = GetButton((int)EmptyPopupButtons.Background);

        BindButtons();
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseEmptyPopup);
        if (_background != null) _background.onClick.AddListener(CloseEmptyPopup);
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseEmptyPopup);
        if (_background != null) _background.onClick.RemoveListener(CloseEmptyPopup);
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

public enum EmptyPopupButtons
{
    CloseButton,
    Background
}