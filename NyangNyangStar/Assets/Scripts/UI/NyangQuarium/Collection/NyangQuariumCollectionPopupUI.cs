using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumCollectionPopupUI : UIPopup
{
    private const int FishColumnCount = 4;

    [Header("닫기 버튼")]
    [Tooltip("배경")][SerializeField] private Button _background;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;

    [Header("탭 패널")]
    [Tooltip("담수어 버튼")][SerializeField] private Button _freshwaterFishButton;
    [Tooltip("해수어 버튼")][SerializeField] private Button _saltwaterFishButton;
    [Tooltip("기수어 버튼")][SerializeField] private Button _brackishWaterFishButton;
    [Tooltip("자연요소 버튼")][SerializeField] private Button _environmentsButton;

    [Header("물고기 데이터")]
    [SerializeField] private NyangQuariumFishSO _fishSO;

    [Header("도감 해금 데이터")]
    [SerializeField] private NyangQuariumFirestoreSO _nyangquariumSo;

    [Header("수집 진척도")]
    [SerializeField] private TMP_Text _progressText;

    [Header("물고기 슬롯")]
    [SerializeField] private Transform _content;
    [SerializeField] private FishSlotUI _fishSlotPrefab;

    [Header("물고기 정보 팝업")]
    [SerializeField] private NyangQuariumFishInfoPopupUI _infoPopup;

    [Header("미해금 안내창")]
    [SerializeField] private GameObject _lockedPopup;
    [SerializeField] private Button _lockedPopupCloseButton;

    private NyangQuariumCollectionPopupSprite _sprite;
    private readonly List<FishSlotUI> _fishSlots = new();
    private FishType _currentFishType = FishType.Freshwater;
    private bool _isInitialized;

    public override void Init()
    {
        Bind<Button>(typeof(NyangQuariumCollectionPopupButtons));
        Bind<GameObject>(typeof(NyangQuariumCollectionPopupObjects));
        Bind<TMP_Text>(typeof(NyangQuariumCollectionPopupTexts));

        _background = GetButton((int)NyangQuariumCollectionPopupButtons.Background);
        _closeButton = GetButton((int)NyangQuariumCollectionPopupButtons.CloseButton);
        _freshwaterFishButton = GetButton((int)NyangQuariumCollectionPopupButtons.FreshwaterFishButton);
        _saltwaterFishButton = GetButton((int)NyangQuariumCollectionPopupButtons.SaltwaterFishButton);
        _brackishWaterFishButton = GetButton((int)NyangQuariumCollectionPopupButtons.BrackishWaterFishButton);
        _environmentsButton = GetButton((int)NyangQuariumCollectionPopupButtons.EnvironmentsButton);
        _lockedPopupCloseButton = GetButton((int)NyangQuariumCollectionPopupButtons.LockedPopupCloseButton);

        _progressText = GetText((int)NyangQuariumCollectionPopupTexts.ProgressText);

        _infoPopup = GetObject((int)NyangQuariumCollectionPopupObjects.NyangQuariumFishInfoPopup)
            .GetComponent<NyangQuariumFishInfoPopupUI>();
        _lockedPopup = GetObject((int)NyangQuariumCollectionPopupObjects.LockedPopup);

        RefreshPhotoGridCellSize();

        BindButtons();

        _sprite = GetComponent<NyangQuariumCollectionPopupSprite>();
        _sprite.Init();

        InitInfoPopup();

        CreateFishSlots();
        _isInitialized = true;
        ShowFishType(_currentFishType);

        _lockedPopup.SetActive(false);
    }

    private void InitInfoPopup()
    {
        _infoPopup.Init();
        _infoPopup.SetCollectionPopup(this);
        _infoPopup.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;

        RefreshFishSlots();
        ShowFishType(_currentFishType);
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
        float spacing = grid.spacing.x * (FishColumnCount - 1);

        float cellWidth = (contentWidth - padding - spacing) / FishColumnCount;
        float cellHeight = cellWidth * 2;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = FishColumnCount;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    private void BindButtons()
    {
        if (_background != null) _background.onClick.AddListener(CloseNyangQuariumCollectionPopup);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseNyangQuariumCollectionPopup);
        if (_freshwaterFishButton != null) _freshwaterFishButton.onClick.AddListener(() => 
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            ShowFishType(FishType.Freshwater);
        });
        if (_saltwaterFishButton != null) _saltwaterFishButton.onClick.AddListener(() => 
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            ShowFishType(FishType.Saltwater); 
        });
        if (_brackishWaterFishButton != null) _brackishWaterFishButton.onClick.AddListener(() => 
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            ShowFishType(FishType.BrackishWater);
        });
        if (_environmentsButton != null) _environmentsButton.onClick.AddListener(() => 
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            ShowFishType(FishType.Environments);
        });
        if (_lockedPopupCloseButton != null) _lockedPopupCloseButton.onClick.AddListener(() => 
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            _lockedPopup.SetActive(false); 
        });
    }

    private void OnDestroy()
    {
        RemovePopupButton(_background);
        RemovePopupButton(_closeButton);
        RemovePopupButton(_freshwaterFishButton);
        RemovePopupButton(_saltwaterFishButton);
        RemovePopupButton(_brackishWaterFishButton);
        RemovePopupButton(_environmentsButton);
        RemovePopupButton(_lockedPopupCloseButton);
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    private void CloseNyangQuariumCollectionPopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        gameObject.SetActive(false);
    }

    private void CreateFishSlots()
    {
        foreach (NyangQuariumFishData fishData in _fishSO.FishData)
        {
            FishSlotUI slot = Instantiate(_fishSlotPrefab, _content);
            slot.Init();
            slot.SetCollectionPopup(this);
            slot.SetData(fishData, _nyangquariumSo.IsUnlocked(fishData.FishId));

            _fishSlots.Add(slot);
        }
    }

    private void ShowFishType(FishType fishType)
    {
        _currentFishType = fishType;

        foreach (FishSlotUI slot in _fishSlots)
        {
            slot.gameObject.SetActive(slot.FishType == fishType);
        }

        _sprite.SetSelectedTab(fishType);
        UpdateProgress(fishType);
    }

    private void UpdateProgress(FishType fishType)
    {
        NyangQuariumCollectionProgress progress = _nyangquariumSo.GetProgress(_fishSO, fishType);

        _sprite.SetProgress(progress.Ratio);
        _progressText.text = $"({progress.UnlockedCount}/{progress.TotalCount})";
    }

    private void RefreshFishSlots()
    {
        foreach (FishSlotUI slot in _fishSlots)
        {
            slot.RefreshUnlockState(_nyangquariumSo.IsUnlocked(slot.FishId));
        }
    }

    public void OnClickFishSlot(NyangQuariumFishData fishData, bool isUnlocked)
    {
        if (!isUnlocked)
        {
            ShowLockedPopup(fishData);
            return;
        }

        _infoPopup.SetData(fishData);
        _infoPopup.gameObject.SetActive(true);
    }

    public void ShowLockedPopup(NyangQuariumFishData fishData)
    {
        _sprite.SetLockedFishImage(fishData.FishKey);
        _lockedPopup.SetActive(true);
    }
}

public enum NyangQuariumCollectionPopupButtons
{
    Background,
    CloseButton,
    FreshwaterFishButton,
    SaltwaterFishButton,
    BrackishWaterFishButton,
    EnvironmentsButton,
    LockedPopupCloseButton
}

public enum NyangQuariumCollectionPopupObjects
{
    NyangQuariumFishInfoPopup,
    LockedPopup
}

public enum NyangQuariumCollectionPopupTexts
{
    ProgressText
}