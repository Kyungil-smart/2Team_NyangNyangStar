using Core.Managers;
using DG.Tweening;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NotebookPopupUI : UIPopup
{
    private const int CatColumnCount = 3;

    [Header("DOTween 설정")]
    [SerializeField] private Transform _panel;
    [SerializeField] private float _popupScale = 0.85f;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _background;

    [Header("고양이 상세보기 버튼")]
    [SerializeField] private Button _catBackground;

    [Header("고양이 슬롯")]
    [SerializeField] private RectTransform _content;

    private NotebookPopupSprite _sprite;
    private SelectedCatPopupUI _selectedCatPopup;

    public override void Init()
    {
        Bind<Button>(typeof(NotebookPopupButtons));

        _closeButton = GetButton((int)NotebookPopupButtons.CloseButton);
        _background = GetButton((int)NotebookPopupButtons.Background);
        _catBackground = GetButton((int)NotebookPopupButtons.CatBackground1);

        RefreshGridCellSize();

        BindButtons();
        InitPopup(KeyContainer.Prefabs.SelectedCatPopupUI, _catBackground);

        _sprite = GetComponent<NotebookPopupSprite>();
        _sprite.Init();
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseNotebookPopup);
        if (_background != null) _background.onClick.AddListener(CloseNotebookPopup);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_catBackground);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<SelectedCatPopupUI>(key, onLoaded => 
        {
            _selectedCatPopup = onLoaded;
            onLoaded.SetNotebookPopup(this);
            AddPopupButton(button, onLoaded); 
        }, 
        false);
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

    private void RefreshGridCellSize()
    {
        if (_content == null) return;

        GridLayoutGroup grid = _content.GetComponent<GridLayoutGroup>();

        if (grid == null) return;

        float contentWidth = _content.rect.width;

        float padding = grid.padding.left + grid.padding.right;
        float spacing = grid.spacing.x * (CatColumnCount - 1);

        float cellWidth = (contentWidth - padding - spacing) / CatColumnCount;
        float cellHeight = cellWidth * 6f / 5f;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CatColumnCount;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    private void CloseNotebookPopup()
    {
        if (_panel == null) return;
        _panel.DOScale(Vector3.one * _popupScale, _popupScaleDuration)
            .SetEase(Ease.OutSine)
            .OnComplete(() => gameObject.SetActive(false));

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    public void HideNotebookPopup() => gameObject.SetActive(false);

    public void SetPhotoAlert(bool isOn)
    {
        _sprite?.SetRedPoint(isOn);
        _selectedCatPopup?.SetPhotoAlert(isOn);
    }
}

public enum NotebookPopupButtons
{
    CloseButton,
    Background,
    CatBackground1
}
