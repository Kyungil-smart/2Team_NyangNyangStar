using Core.Managers;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramAddPostUI : UIPopup
{
    private const int AlbumColumnCount = 4;

    [Header("DoTween 설정")]
    [SerializeField] private RectTransform _panel;
    [SerializeField] private float _popupScaleDuration = 0.1f;

    [Header("버튼")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _nyangstagramCloseButton;
    [SerializeField] private Button _newImageUploadButton;

    [Header("드롭다운")]
    [SerializeField] private TMP_Dropdown _albumDropdown;

    [Header("선택 이미지")]
    [SerializeField] private Image _newPostImage;
    [SerializeField] private Color _newPostColor;
    [SerializeField] private TMP_Text _noImageText;

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapRuntimePhotoSO _runtimePhotoSO;

    [Header("앨범 슬롯")]
    [SerializeField] private Transform _albumContent;
    [SerializeField] private NyangStargramAlbumSlotUI _albumSlotPrefab;

    private readonly Dictionary<string, NyangStargramAlbumSlotUI> _albumSlotDic = new();
    private readonly HashSet<string> _uploadedPhotoIds = new();

    private NyangstagramMainUI _mainUI;
    private NyangStargramAlbumSlotUI _selectedSlot;
    private NyangNyangSnapRuntimePhotoData _selectedPhotoData;
    private bool _hasSelectedPhoto;

    private NyangStargramAddPostUISprite _nyangStargramAddPostUISprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramAddPostUIButton));

        _backButton = Get<Button>((int)NyangStargramAddPostUIButton.BackButton);
        _nyangstagramCloseButton = Get<Button>((int)NyangStargramAddPostUIButton.NyangstagramCloseButton);
        _newImageUploadButton = Get<Button>((int)NyangStargramAddPostUIButton.NewImageUploadButton);

        _albumDropdown = UIBase.FindChild<TMP_Dropdown>(gameObject, "Dropdown", true);
        _newPostImage = UIBase.FindChild<Image>(gameObject, "New Post Image", true);
        _albumContent = UIBase.FindChild<Transform>(gameObject, "AlbumContent", true);
        _noImageText = UIBase.FindChild<TMP_Text>(gameObject, "NoImageText", true);

        RefreshAlbumGridCellSize();

        BindButtons();
        BindDropdown();

        _nyangStargramAddPostUISprite = GetComponent<NyangStargramAddPostUISprite>();
        _nyangStargramAddPostUISprite.Init();

        DebugTool.Log("NyangStargramAddPostUI Init 실행됨", DebugType.UI, this);
    }

    private void RefreshAlbumGridCellSize()
    {
        RectTransform contentRect = _albumContent as RectTransform;
        GridLayoutGroup grid = _albumContent.GetComponent<GridLayoutGroup>();

        if (contentRect == null || grid == null) return;

        float contentWidth = contentRect.rect.width;
        float padding = grid.padding.left + grid.padding.right;
        float spacing = grid.spacing.x * (AlbumColumnCount - 1);

        float cellSize = (contentWidth - padding - spacing) / AlbumColumnCount;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = AlbumColumnCount;
        grid.cellSize = new Vector2(cellSize, cellSize);
    }

    public void SetMainUI(NyangstagramMainUI mainUI)
    {
        _mainUI = mainUI;
    }

    private void OnEnable()
    {
        _selectedSlot = null;
        _hasSelectedPhoto = false;

        if (_newPostImage != null)
            _newPostImage.sprite = null;
            _newPostImage.color = _newPostColor;
        
        if (_noImageText != null)
            _noImageText.gameObject.SetActive(true);

        RefreshAlbumGridCellSize();
        RefreshAlbumSlots();
    }

    private void RefreshAlbumSlots()
    {
        // 정렬
        List<NyangNyangSnapRuntimePhotoData> sortedPhotos = _runtimePhotoSO.RuntimePhotos
            .OrderByDescending(photo => photo.CreatedAt)
            .ToList();

        // 서버에 저장된 사진 ID
        HashSet<string> serverPhotoIds = new();

        for (int i = 0; i < sortedPhotos.Count; i++)
        {
            NyangNyangSnapRuntimePhotoData photoData = sortedPhotos[i];

            serverPhotoIds.Add(photoData.PhotoId);

            if (!_albumSlotDic.TryGetValue(photoData.PhotoId, out NyangStargramAlbumSlotUI slot))
            {
                slot = CreateAlbumSlot(photoData.PhotoId);
            }

            bool isUploaded = _uploadedPhotoIds.Contains(photoData.PhotoId);

            slot.SetAddPostUI(this);
            slot.SetData(photoData, isUploaded);
            slot.transform.SetSiblingIndex(i);
        }

        RemoveDeletedAlbumSlots(serverPhotoIds);
    }

    private NyangStargramAlbumSlotUI CreateAlbumSlot(string photoId)
    {
        NyangStargramAlbumSlotUI slot = Instantiate(_albumSlotPrefab, _albumContent);
        slot.Init();

        _albumSlotDic.Add(photoId, slot);

        return slot;
    }

    private void RemoveDeletedAlbumSlots(HashSet<string> serverPhotoIds)
    {
        List<string> removeIds = new();

        foreach (KeyValuePair<string, NyangStargramAlbumSlotUI> pair in _albumSlotDic)
        {
            if (!serverPhotoIds.Contains(pair.Key))
                removeIds.Add(pair.Key);
        }

        foreach (string photoId in removeIds)
        {
            if (_albumSlotDic[photoId] != null)
                Destroy(_albumSlotDic[photoId].gameObject);

            _albumSlotDic.Remove(photoId);
            _uploadedPhotoIds.Remove(photoId);
        }
    }

    public void SelectAlbumImage(NyangStargramAlbumSlotUI slot, NyangNyangSnapRuntimePhotoData photoData, Sprite sprite)
    {
        if (_selectedSlot != null)
            _selectedSlot.SetSelected(false);

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        _selectedPhotoData = photoData;
        _hasSelectedPhoto = true;

        _noImageText.gameObject.SetActive(false);

        _newPostImage.sprite = sprite;
        _newPostImage.color = Color.white;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        DebugTool.Log($"선택한 사진: {photoData.PhotoId}", DebugType.UI, this);
    }

    private void BindButtons()
    {
        AddHideSelfButton(_backButton);
        AddCloseAllButton(_nyangstagramCloseButton);
        AddUploadButton(_newImageUploadButton);
    }

    private void BindDropdown()
    {
        if (_albumDropdown == null) return;

        _albumDropdown.ClearOptions();

        List<string> options = new()
        {
            "모든 사진",
            "뭉치",
            "일상"
        };

        _albumDropdown.AddOptions(options);
        _albumDropdown.onValueChanged.RemoveAllListeners();
        _albumDropdown.onValueChanged.AddListener(OnChangeAlbumFilter);
    }

    private void OnChangeAlbumFilter(int index)
    {
        if (_albumDropdown == null) return;
        if (index < 0 || index >= _albumDropdown.options.Count) return;

        string selectedOption = _albumDropdown.options[index].text;
        DebugTool.Log($"앨범 필터 선택: {selectedOption}", DebugType.UI, this);
    }

    private void AddHideSelfButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(CloseAddPostPopup);
    }

    private void AddCloseAllButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            NyangstagramUIRouter.RequestCloseAll();
        });
    }

    private void AddUploadButton(Button button)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickUploadButton);
    }

    private void OnClickUploadButton()
    {
        if (!_hasSelectedPhoto)
            return;

        if (_mainUI == null)
        {
            DebugTool.Warning("NyangstagramMainUI 연결이 없습니다.", DebugType.UI, this);
            return;
        }

        _mainUI.AddPost(_selectedPhotoData);

        _uploadedPhotoIds.Add(_selectedPhotoData.PhotoId);

        if (_selectedSlot != null)
        {
            _selectedSlot.SetUploaded(true);
            _selectedSlot = null;
        }

        DebugTool.Log($"게시물 업로드: {_selectedPhotoData.PhotoId}", DebugType.UI, this);

        _hasSelectedPhoto = false;

        CloseAddPostPopup();
    }

    public override void PlayOpenAnimation()
    {
        if (_panel == null) return;

        //_panel.anchoredPosition = new Vector2(0f, -1000f);
        //_panel.DOAnchorPos(Vector2.zero, _popupScaleDuration)
        //    .SetEase(Ease.OutSine);
    }

    private void CloseAddPostPopup()
    {
        if (_panel == null) return;

        if (_mainUI != null)
        {
            _mainUI.RestoreMainTab();
        }

        gameObject.SetActive(false);
    }

}


public enum NyangStargramAddPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    NewImageUploadButton
}
