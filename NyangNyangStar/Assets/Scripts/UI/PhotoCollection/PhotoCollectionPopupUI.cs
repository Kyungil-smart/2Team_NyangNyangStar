using Core.Managers;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PhotoCollectionPopupUI : UIPopup
{
    [Header("닫기 버튼")]
    [Tooltip("뒤로 가기")][SerializeField] private Button _backButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;
    [Tooltip("배경")][SerializeField] private Button _background;

    [Header("사진 필터 버튼")]
    [SerializeField] private Button _filterButton;

    [Header("사진 필터")]
    [Tooltip("필터 패널")][SerializeField] private GameObject _filterPanel;
    [Tooltip("필터 닫기 버튼")][SerializeField] private Button _filterCloseButton;

    private SelectedCatPopupUI _selectedCatPopup;
    private PhotoCollectionPopupSprite _sprite;

    public override void Init()
    {
        Bind<Button>(typeof(PhotoCollectionPopupButtons));
        Bind<GameObject>(typeof(PhotoCollectionPopupObjects));

        _closeButton = GetButton((int)PhotoCollectionPopupButtons.CloseButton);
        _background = GetButton((int)PhotoCollectionPopupButtons.Background);
        _backButton = GetButton((int)PhotoCollectionPopupButtons.BackButton);
        _filterButton = GetButton((int)PhotoCollectionPopupButtons.FilterButton);
        _filterCloseButton = GetButton((int)PhotoCollectionPopupButtons.FilterCloseButton);

        _filterPanel = GetObject((int)PhotoCollectionPopupObjects.FilterPanel);

        BindButtons();

        _sprite = GetComponent<PhotoCollectionPopupSprite>();
        _sprite.Init();
    }

    private void OnEnable()
    {
       _filterPanel.SetActive(false);
    }

    public void SetSelectedCatPopup(SelectedCatPopupUI popup)
    {
        _selectedCatPopup = popup;
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(() => CloseAllPopups());
        if (_background != null) _background.onClick.AddListener(() => CloseAllPopups());
        if (_backButton != null) _backButton.onClick.AddListener(ClosePhotoCollectionPopup);
        if (_filterButton != null) _filterButton.onClick.AddListener(ToggleFilterPanel);
        if (_filterCloseButton != null) _filterCloseButton.onClick.AddListener(ToggleFilterPanel);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_backButton);
        RemovePopupButton(_filterButton);
        RemovePopupButton(_filterCloseButton);
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    private void ToggleFilterPanel()
    {
        if (_filterPanel == null) return;

        _filterPanel.SetActive(!_filterPanel.activeSelf);
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void ClosePhotoCollectionPopup()
    {
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void CloseAllPopups()
    {
        _selectedCatPopup.HideSelectedCatPopup();
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }
}

public enum PhotoCollectionPopupButtons
{
    CloseButton,
    Background,
    BackButton,
    FilterButton,
    FilterCloseButton
}

public enum PhotoCollectionPopupObjects
{
    FilterPanel
}