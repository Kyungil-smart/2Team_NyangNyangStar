using Core.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

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

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    [Header("사진 슬롯")]
    [SerializeField] private Transform _content;
    [SerializeField] private CatPhotoSlotUI _catPhotoSlotPrefab;

    private readonly Dictionary<string, CatPhotoSlotUI> _photoDic = new();
    private SelectedCatPopupUI _selectedCatPopup;
    private PhotoCollectionPopupSprite _sprite;
    private PhotoDetailPopupUI _detailPopup;

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
        InitPhotoDetailPopup();

        _sprite = GetComponent<PhotoCollectionPopupSprite>();
        _sprite.Init();

        _filterPanel.SetActive(false);
    }

    private void InitPhotoDetailPopup()
    {
        GameManager.UI.ShowPopupUI<PhotoDetailPopupUI>(KeyContainer.Prefabs.PhotoDetailPopupUI,
            onLoaded =>
            {
                _detailPopup = onLoaded;
                _detailPopup.OnDeleted += RefreshPhotoSlots;
            },
            false);
    }

    private void OnEnable()
    {
        if (_filterPanel != null)
            _filterPanel.SetActive(false);

        RefreshPhotoSlots();
    }

    public void SetSelectedCatPopup(SelectedCatPopupUI popup)
    {
        _selectedCatPopup = popup;
    }

    private async void RefreshPhotoSlots()
    {
        await RefreshPhotoSlotsAsync();

        DebugTool.Log("사진 목록 새로고침", DebugType.UI, this);
    }

    private async Task RefreshPhotoSlotsAsync()
    {
        // 서버에서 가져오기
        await _photoAlbumSO.UpdateFromServerAsync(false);

        // 정렬
        List<NyangNyangSnapSavedPhotoData> sortedPhotos = _photoAlbumSO.Photos
            .OrderByDescending(x => x.starCount)
            .ThenByDescending(x => x.createdAt)
            .ToList();

        // 서버에서 가져온 사진 id
        HashSet<string> serverPhotoIds = new();

        for (int i = 0; i < sortedPhotos.Count; i++)
        {
            NyangNyangSnapSavedPhotoData photoData = sortedPhotos[i];

            if (string.IsNullOrEmpty(photoData.photoId)) continue;

            serverPhotoIds.Add(photoData.photoId);

            if (!_photoDic.TryGetValue(photoData.photoId, out CatPhotoSlotUI slot))
            {
                slot = CreateSlot(photoData.photoId);
            }

            if(slot == null) continue;

            slot.SetDetailPopup(_detailPopup);
            slot.SetData(photoData);
            slot.transform.SetSiblingIndex(i);
        }

        RemoveDeletedSlots(serverPhotoIds);
    }

    private CatPhotoSlotUI CreateSlot(string photoId)
    {
        CatPhotoSlotUI slot = Instantiate(_catPhotoSlotPrefab, _content);
        slot.Init();

        _photoDic.Add(photoId, slot);

        return slot;
    }

    private void RemoveDeletedSlots(HashSet<string> serverPhotoIds)
    {
        List<string> removeIds = new();

        foreach (KeyValuePair<string, CatPhotoSlotUI> pair in _photoDic)
        {
            if (!serverPhotoIds.Contains(pair.Key))
                removeIds.Add(pair.Key);
        }

        foreach (string photoId in removeIds)
        {
            if (_photoDic[photoId] != null)
                Destroy(_photoDic[photoId].gameObject);

            _photoDic.Remove(photoId);
        }
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