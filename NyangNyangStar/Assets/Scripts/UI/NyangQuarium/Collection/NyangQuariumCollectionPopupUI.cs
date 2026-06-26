using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

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
    [SerializeField] private Transform _content;
    [SerializeField] private FishSlotUI _fishSlotPrefab;

    [Header("미해금 안내창")]
    [SerializeField] private GameObject _lockedPopup;

    private NyangQuariumCollectionPopupSprite _sprite;
    private readonly List<FishSlotUI> _fishSlots = new();
    private FishType _currentFishType = FishType.Freshwater;
    private bool _isInitialized;
    private NyangQuariumFishInfoPopupUI _infoPopup;

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

        _lockedPopup = GetObject((int)NyangQuariumCollectionPopupObjects.LockedPopup);

        RefreshPhotoGridCellSize();

        BindButtons();

        _sprite = GetComponent<NyangQuariumCollectionPopupSprite>();
        _sprite.Init();

        InitInfoPopup();

        _lockedPopup.SetActive(false);
    }

    private void InitInfoPopup()
    {
        GameManager.UI.ShowPopupUI<NyangQuariumFishInfoPopupUI>(KeyContainer.Prefabs.NyangQuariumFishInfoPopupUI,
            onLoaded =>
            {
                _infoPopup = onLoaded;
                _infoPopup.SetCollectionPopup(this);

                CreateFishSlots();

                _isInitialized = true;
                ShowFishType(_currentFishType);
            },
            false);
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;

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
        if (_freshwaterFishButton != null) _freshwaterFishButton.onClick.AddListener(() => ShowFishType(FishType.Freshwater));
        if (_saltwaterFishButton != null) _saltwaterFishButton.onClick.AddListener(() => ShowFishType(FishType.Saltwater));
        if (_brackishWaterFishButton != null) _brackishWaterFishButton.onClick.AddListener(() => ShowFishType(FishType.BrackishWater));
        if (_environmentsButton != null) _environmentsButton.onClick.AddListener(() => ShowFishType(FishType.Environments));
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
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void CreateFishSlots()
    {
        foreach (NyangQuariumFishData fishData in _fishSO.FishData)
        {
            FishSlotUI slot = Instantiate(_fishSlotPrefab, _content);
            slot.Init();
            slot.SetInfoPopup(_infoPopup);
            slot.SetData(fishData);

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

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void UpdateProgress(FishType fishType)
    {
        int totalCount = _fishSO.GetFishCount(fishType);
        int unlockedCount = 0;

        foreach (NyangQuariumFishData fishData in _fishSO.FishData)
        {
            if (fishData.FishType != fishType) continue;

            // TODO : 유저 물고기 해금 데이터에서 체크
            //if()
            unlockedCount++;
        }

        float progress = totalCount == 0 ? 0f : (float)unlockedCount / totalCount;

        _sprite.SetProgress(progress);
        _progressText.text = $"({unlockedCount}/{totalCount})";
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
    LockedPopup
}

public enum NyangQuariumCollectionPopupTexts
{
    ProgressText
}