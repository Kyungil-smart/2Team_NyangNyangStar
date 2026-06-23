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
    private const int PhotoColumnCount = 4;

    [Header("닫기 버튼")]
    [Tooltip("뒤로 가기")][SerializeField] private Button _backButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;
    [Tooltip("배경")][SerializeField] private Button _background;

    [Header("사진 필터 버튼")]
    [SerializeField] private Button _filterButton;

    [Header("사진 필터")]
    [Tooltip("필터 패널")][SerializeField] private GameObject _filterPanel;
    [Tooltip("필터 닫기 버튼")][SerializeField] private Button _filterCloseButton;

    [Header("별 필터 버튼(Toggle)")]
    [SerializeField] private Toggle _star1Toggle;
    [SerializeField] private Toggle _star2Toggle;
    [SerializeField] private Toggle _star3Toggle;
    [SerializeField] private Toggle _star4Toggle;
    [SerializeField] private Toggle _star5Toggle;

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    [Header("사진 슬롯")]
    [SerializeField] private Transform _content;
    [SerializeField] private CatPhotoSlotUI _catPhotoSlotPrefab;

    private readonly Dictionary<string, CatPhotoSlotUI> _photoDic = new();
    private SelectedCatPopupUI _selectedCatPopup;
    private PhotoCollectionPopupSprite _sprite;
    private PhotoDetailPopupUI _detailPopup;
    private Toggle[] _starToggles;

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

        _starToggles = new Toggle[] { _star1Toggle, _star2Toggle, _star3Toggle, _star4Toggle, _star5Toggle };
        BindFilterToggles();

        RefreshPhotoGridCellSize();

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
                _detailPopup.SetPhotoCollectionPopup(this);
                _detailPopup.OnDeleted += RefreshPhotoSlots;
            },
            false);
    }

    private void OnEnable()
    {
        if (_filterPanel != null)
            _filterPanel.SetActive(false);
        MainUI.Instance?.SetPhotoAlert(false);

        RefreshPhotoGridCellSize();
        RefreshPhotoSlots();
    }

    private void RefreshPhotoGridCellSize()
    {
        if (_content == null) return;

        RectTransform contentRect = _content as RectTransform;
        GridLayoutGroup grid = _content.GetComponent<GridLayoutGroup>();

        if (contentRect == null || grid == null)
            return;

        float contentWidth = contentRect.rect.width;

        float padding = grid.padding.left + grid.padding.right;
        float spacing = grid.spacing.x * (PhotoColumnCount - 1);

        float cellWidth = (contentWidth - padding - spacing) / PhotoColumnCount;
        float cellHeight = cellWidth * 16f / 9f;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = PhotoColumnCount;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    public void SetSelectedCatPopup(SelectedCatPopupUI popup)
    {
        _selectedCatPopup = popup;
    }

    private async void RefreshPhotoSlots()
    {
        try
        {
            await RefreshPhotoSlotsAsync();
            DebugTool.Log("사진 목록 새로고침", DebugType.UI, this);
        }
        catch (System.Exception e)
        {
            DebugTool.Warning($"[PhotoCollectionPopupUI] 사진 목록 새로고침 실패: {e.Message}", DebugType.UI, this);
        }
    }

    private async Task RefreshPhotoSlotsAsync()
    {
        if (!EnsurePhotoAlbumReady())
            return;

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

            if (slot == null) continue;

            slot.SetDetailPopup(_detailPopup);
            slot.SetData(photoData);
            slot.transform.SetSiblingIndex(i);
        }

        RemoveDeletedSlots(serverPhotoIds);
        ApplyStarFilter();
    }

    private bool EnsurePhotoAlbumReady()
    {
        if (_photoAlbumSO == null)
        {
            DebugTool.Warning("[PhotoCollectionPopupUI] PhotoAlbumSO가 연결되지 않았습니다.", DebugType.UI, this);
            return false;
        }

        if (_photoAlbumSO.TryEnsureDatabaseReady())
            return true;

        DebugTool.Warning("[PhotoCollectionPopupUI] Firestore 준비 전이라 사진 목록 갱신을 건너뜁니다.", DebugType.UI, this);
        return false;
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
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseAllPopups);
        if (_background != null) _background.onClick.AddListener(CloseAllPopups);
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

    private void BindFilterToggles()
    {
        foreach (Toggle toggle in _starToggles)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                DebugTool.Log($"{toggle.name} : {isOn}", DebugType.UI, this);
                ApplyStarFilter();
                GameManager.Audio.PlaySfx("Main_SFX_Touch");
            });
        }
    }

    private void ApplyStarFilter()
    {
        List<int> selectedStars = new();

        for (int i = 0; i < _starToggles.Length; i++)
        {
            if (_starToggles[i].isOn)
                selectedStars.Add(i + 1);
        }

        foreach (CatPhotoSlotUI slot in _photoDic.Values)
        {
            slot.gameObject.SetActive(selectedStars.Contains(slot.StarCount));
        }
    }

    private void ClosePhotoCollectionPopup()
    {
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void CloseAllPopups()
    {
        HidePhotoCollectionPopup();

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    public void HidePhotoCollectionPopup()
    {
        _selectedCatPopup.HideSelectedCatPopup();
        gameObject.SetActive(false);
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
