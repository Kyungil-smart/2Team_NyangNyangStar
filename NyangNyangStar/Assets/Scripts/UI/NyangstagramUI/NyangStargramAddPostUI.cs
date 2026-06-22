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

    [Header("사진 데이터")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;

    [Header("앨범 슬롯")]
    [SerializeField] private Transform _albumButtonRoots;
    [SerializeField] private NyangStargramAlbumSlotUI _albumSlotPrefab;

    private readonly Dictionary<string, NyangStargramAlbumSlotUI> _albumSlotDic = new();
    private readonly HashSet<string> _uploadedPhotoIds = new();

    private NyangstagramMainUI _mainUI;
    private NyangStargramAlbumSlotUI _selectedSlot;
    private NyangNyangSnapSavedPhotoData _selectedPhotoData;
    private Sprite _selectedPhotoSprite;
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
        _albumButtonRoots = UIBase.FindChild<Transform>(gameObject, "AlbumContent", true);

        BindButtons();
        BindDropdown();

        _nyangStargramAddPostUISprite = GetComponent<NyangStargramAddPostUISprite>();
        _nyangStargramAddPostUISprite.Init();

        DebugTool.Log("NyangStargramAddPostUI Init 실행됨", DebugType.UI, this);
    }

    public void SetMainUI(NyangstagramMainUI mainUI)
    {
        _mainUI = mainUI;
    }

    private void OnEnable()
    {
        _selectedSlot = null;
        _selectedPhotoSprite = null;
        _hasSelectedPhoto = false;

        if (_newPostImage != null)
            _newPostImage.sprite = null;

        RefreshAlbumSlots();
    }

    private async void RefreshAlbumSlots()
    {
        await _photoAlbumSO.UpdateFromServerAsync(false);

        List<NyangNyangSnapSavedPhotoData> sortedPhotos = _photoAlbumSO.Photos
            .OrderByDescending(x => x.createdAt)
            .ToList();

        HashSet<string> currentPhotoIds = new();

        for (int i = 0; i < sortedPhotos.Count; i++)
        {
            NyangNyangSnapSavedPhotoData photoData = sortedPhotos[i];

            currentPhotoIds.Add(photoData.photoId);

            if (!_albumSlotDic.TryGetValue(photoData.photoId, out NyangStargramAlbumSlotUI slot))
            {
                slot = CreateAlbumSlot(photoData.photoId);
            }

            bool isUploaded = _uploadedPhotoIds.Contains(photoData.photoId);

            slot.SetData(photoData, isUploaded);
            slot.transform.SetSiblingIndex(i);
        }

        RemoveDeletedAlbumSlots(currentPhotoIds);
    }

    private NyangStargramAlbumSlotUI CreateAlbumSlot(string photoId)
    {
        NyangStargramAlbumSlotUI slot = Instantiate(_albumSlotPrefab, _albumButtonRoots);

        slot.Init();
        slot.SetAddPostUI(this);

        _albumSlotDic.Add(photoId, slot);

        return slot;
    }

    private void RemoveDeletedAlbumSlots(HashSet<string> currentPhotoIds)
    {
        List<string> removeIds = new();

        foreach (KeyValuePair<string, NyangStargramAlbumSlotUI> pair in _albumSlotDic)
        {
            if (!currentPhotoIds.Contains(pair.Key))
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

    public void SelectAlbumImage(NyangStargramAlbumSlotUI slot, NyangNyangSnapSavedPhotoData photoData, Sprite sprite)
    {
        if (_selectedSlot != null)
            _selectedSlot.SetSelected(false);

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        _selectedPhotoData = photoData;
        _selectedPhotoSprite = sprite;
        _hasSelectedPhoto = true;

        _newPostImage.sprite = sprite;
        _newPostImage.color = Color.white;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        DebugTool.Log($"선택한 사진: {photoData.photoId}", DebugType.UI, this);
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

        _mainUI.AddPost(_selectedPhotoData, _selectedPhotoSprite);

        _uploadedPhotoIds.Add(_selectedPhotoData.photoId);

        if (_selectedSlot != null)
        {
            _selectedSlot.SetUploaded(true);
            _selectedSlot = null;
        }

        DebugTool.Log($"게시물 업로드: {_selectedPhotoData.photoId}", DebugType.UI, this);

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
        gameObject.SetActive(false);
        //_panel.DOAnchorPos(new Vector2(0f, -1000f), _popupScaleDuration)
        //    .SetEase(Ease.OutSine)
        //    .OnComplete(() => gameObject.SetActive(false));
    }
}

public enum NyangStargramAddPostUIButton
{
    BackButton,
    NyangstagramCloseButton,
    NewImageUploadButton
}