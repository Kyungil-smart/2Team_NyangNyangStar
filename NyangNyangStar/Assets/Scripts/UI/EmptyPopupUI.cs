using UI;
using UnityEngine;
using UnityEngine.UI;

public class EmptyPopupUI : UIPopup
{
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
        if (_closeButton != null) _closeButton.onClick.AddListener(ClosePopup);
        if (_background != null) _background.onClick.AddListener(ClosePopup);
    }

    private void OnDisable()
    {
        if (_closeButton != null) _closeButton.onClick.RemoveListener(ClosePopup);
        if (_background != null) _background.onClick.RemoveListener(ClosePopup);
    }
}

public enum EmptyPopupButtons
{
    CloseButton,
    Background
}
