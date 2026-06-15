using Core.Managers;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class SelectedCatPopupUI : UIPopup
{
    [Header("닫기 버튼")]
    [Tooltip("뒤로 가기")][SerializeField] private Button _backButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;
    [Tooltip("배경")][SerializeField] private Button _background;

    [Header("탭 패널")]
    [Tooltip("정보 버튼")][SerializeField] private Button _infoButton;
    [Tooltip("메모 버튼")][SerializeField] private Button _memoButton;
    [Tooltip("통계 버튼")][SerializeField] private Button _statsButton;
    [Tooltip("정보 탭")][SerializeField] private GameObject _infoPanel;
    [Tooltip("메모 탭")][SerializeField] private GameObject _memoPanel;
    [Tooltip("통계 탭")][SerializeField] private GameObject _statsPanel;

    [Header("사진 도감 버튼")]
    [SerializeField] private Button _collectionButton;

    private NotebookPopupUI _notebookPopup;

    public override void Init()
    {
        Bind<Button>(typeof(SelectedCatPopupButtons));
        Bind<GameObject>(typeof(SelectedCatPopupObjects));

        _closeButton = GetButton((int)SelectedCatPopupButtons.CloseButton);
        _background = GetButton((int)SelectedCatPopupButtons.Background);
        _backButton = GetButton((int)SelectedCatPopupButtons.BackButton);
        _infoButton = GetButton((int)SelectedCatPopupButtons.InfoButton);
        _memoButton = GetButton((int)SelectedCatPopupButtons.MemoButton);
        _statsButton = GetButton((int)SelectedCatPopupButtons.StatsButton);
        _collectionButton = GetButton((int)SelectedCatPopupButtons.CollectionButton);

        _infoPanel = GetObject((int)SelectedCatPopupObjects.InfoTextPanel);
        _memoPanel = GetObject((int)SelectedCatPopupObjects.MemoTextPanel);
        _statsPanel = GetObject((int)SelectedCatPopupObjects.StatsTextPanel);

        BindButtons();
        InitPopup(KeyContainer.Prefabs.PhotoCollectionPopupUI, _collectionButton);
    }

    private void OnEnable()
    {
        OpenTab(_infoPanel);
    }

    public void SetNotebookPopup(NotebookPopupUI popup)
    {
        _notebookPopup = popup;
    }

    private void BindButtons()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(() => CloseAllPopups());
        if (_background != null) _background.onClick.AddListener(() => CloseAllPopups());
        if (_backButton != null) _backButton.onClick.AddListener(CloseSelectedCatPopup);
        if (_infoButton != null) _infoButton.onClick.AddListener(() => OpenTab(_infoPanel));
        if (_memoButton != null) _memoButton.onClick.AddListener(() => OpenTab(_memoPanel));
        if (_statsButton != null) _statsButton.onClick.AddListener(() => OpenTab(_statsPanel));
    }

    private void OnDestroy()
    {
        RemovePopupButton(_closeButton);
        RemovePopupButton(_background);
        RemovePopupButton(_backButton);
        RemovePopupButton(_infoButton);
        RemovePopupButton(_memoButton);
        RemovePopupButton(_statsButton);
        RemovePopupButton(_collectionButton);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<PhotoCollectionPopupUI>(key, onLoaded => 
        {
            onLoaded.SetSelectedCatPopup(this);
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

    private void OpenTab(GameObject target)
    {
        _infoPanel.SetActive(false);
        _memoPanel.SetActive(false);
        _statsPanel.SetActive(false);

        target.SetActive(true);
    }

    private void CloseSelectedCatPopup()
    {
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void CloseAllPopups()
    {
        HideSelectedCatPopup();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    public void HideSelectedCatPopup()
    {
        _notebookPopup.HideNotebookPopup();
        gameObject.SetActive(false);
    }
}

public enum SelectedCatPopupButtons
{
    CloseButton,
    Background,
    BackButton,
    InfoButton,
    MemoButton,
    StatsButton,
    //AlbumButton,
    CollectionButton,
    //NyangGalleryButton,
    //WardrobeButton
}

public enum SelectedCatPopupObjects
{
    InfoTextPanel,
    MemoTextPanel,
    StatsTextPanel
}