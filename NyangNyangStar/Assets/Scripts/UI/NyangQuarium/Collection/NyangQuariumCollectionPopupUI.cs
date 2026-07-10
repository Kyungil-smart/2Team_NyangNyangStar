using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UI.NyangQuarium;
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

    [Header("수집 진척도")]
    [SerializeField] private TMP_Text _progressText;

    [Header("물고기 슬롯")]
    [SerializeField] private RectTransform _content;
    [SerializeField] private FishSlotUI _fishSlotPrefab;

    [Header("물고기 정보 팝업")]
    [SerializeField] private NyangQuariumFishInfoPopupUI _infoPopup;

    private NyangQuariumFirestoreSO _nyangquariumSo;
    private NyangQuariumCollectionPopupSprite _sprite;
    private readonly List<FishSlotUI> _fishSlots = new();
    private readonly Dictionary<FishType, List<NyangQuariumFishData>> _fishTypeDic = new();
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

        _progressText = GetText((int)NyangQuariumCollectionPopupTexts.ProgressText);

        _infoPopup = GetObject((int)NyangQuariumCollectionPopupObjects.NyangQuariumFishInfoPopup)
            .GetComponent<NyangQuariumFishInfoPopupUI>();

        RefreshFishGridCellSize();

        BindButtons();

        _sprite = GetComponent<NyangQuariumCollectionPopupSprite>();
        _sprite.Init();

        InitInfoPopup();
        InitFishTypeDictionary();
        InitAsync();
    }

    private void InitInfoPopup()
    {
        _infoPopup.SetCollectionPopup(this);
        _infoPopup.gameObject.SetActive(false);
    }

    private async void InitAsync()
    {
        _nyangquariumSo = await NyangQuariumFirestoreSO.WaitForReadyAsync();

        bool loaded = await _nyangquariumSo.LoadOrCreateFromServerAsync();
        if (!loaded) return;

        CreateFishSlots();
        _isInitialized = true;
        ShowFishType(_currentFishType);
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;

        RefreshFishSlots();
        ShowFishType(_currentFishType);
    }

    private void OnDisable()
    {
        if (!_isInitialized) return;

        NyangquariumMainUIManager.Active?.NotifyPopupContentClosed(gameObject);
    }

    private void RefreshFishGridCellSize()
    {
        if (_content == null) return;

        GridLayoutGroup grid = _content.GetComponent<GridLayoutGroup>();

        if (grid == null) return;

        float contentWidth = _content.rect.width;

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
    }

    private void OnDestroy()
    {
        RemovePopupButton(_background);
        RemovePopupButton(_closeButton);
        RemovePopupButton(_freshwaterFishButton);
        RemovePopupButton(_saltwaterFishButton);
        RemovePopupButton(_brackishWaterFishButton);
        RemovePopupButton(_environmentsButton);
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

    private void InitFishTypeDictionary()
    {
        _fishTypeDic.Clear();

        foreach (NyangQuariumFishData fishData in _fishSO.FishData)
        {
            if (!_fishTypeDic.ContainsKey(fishData.FishType))
                _fishTypeDic[fishData.FishType] = new List<NyangQuariumFishData>();

            _fishTypeDic[fishData.FishType].Add(fishData);
        }
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

    public bool IsFishUnlocked(int fishId) => _nyangquariumSo.IsUnlocked(fishId);
   
    public void OnClickFishSlot(NyangQuariumFishData fishData)
    {
        if (!_fishTypeDic.TryGetValue(fishData.FishType, out List<NyangQuariumFishData> fishList)) return;

        int targetIndex = fishList.FindIndex(fish => fish.FishId == fishData.FishId);

        _infoPopup.SetFishList(fishList, targetIndex);
        _infoPopup.gameObject.SetActive(true);
    }
}

public enum NyangQuariumCollectionPopupButtons
{
    Background,
    CloseButton,
    FreshwaterFishButton,
    SaltwaterFishButton,
    BrackishWaterFishButton,
    EnvironmentsButton
}

public enum NyangQuariumCollectionPopupObjects
{
    NyangQuariumFishInfoPopup
}

public enum NyangQuariumCollectionPopupTexts
{
    ProgressText
}
