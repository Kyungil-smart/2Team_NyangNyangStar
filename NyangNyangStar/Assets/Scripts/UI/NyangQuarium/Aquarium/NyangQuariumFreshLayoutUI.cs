using Core.Managers;
using DG.Tweening;
using UI.Base;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangQuariumFreshLayoutUI : UIPopup, INyangquariumEntryReceiver
{
    public static NyangQuariumFreshLayoutUI ActiveInstance { get; private set; }

    [Header("공통 버튼")]
    [Tooltip("뒤로가기 버튼")]
    [SerializeField] private Button _backButton;

    [Tooltip("해수 수조로 변경하는 버튼")]
    [SerializeField] private Button _changeButton;

    [Header("담수 버튼")]
    [Tooltip("담수 통합 인벤토리 버튼")]
    [SerializeField] private Button _freshWaterFishButton;

    [Tooltip("현재 미사용 버튼")]
    [SerializeField] private Button _freshWaterWeedButton;

    [Tooltip("담수 통합 인벤토리 닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("담수 통합 인벤토리")]
    [Tooltip("담수 통합 인벤토리 패널")]
    [SerializeField] private GameObject _freshwaterLayoutPanel;

    [Header("담수 필터 패널")]
    [Tooltip("필터 패널을 열고 닫는 버튼")]
    [SerializeField] private Button _filterButton;

    [Tooltip("필터 목록이 들어 있는 패널")]
    [SerializeField] private GameObject _filterTab;

    [Tooltip("비워두면 패널의 RectTransform을 자동으로 사용합니다.")]
    [SerializeField] private RectTransform _inventoryRect;

    [Tooltip("열린 위치에서 아래쪽으로 이동할 거리")]
    [SerializeField] private float _inventoryClosedOffsetY = 610f;

    [Tooltip("인벤토리 열기/닫기 시간")]
    [SerializeField] private float _inventoryMoveDuration = 0.3f;

    [Header("물고기 그리드")]
    [Tooltip("물고기 아이템들이 들어가는 FishLayoutGroup의 RectTransform")]
    [SerializeField] private RectTransform _fishLayoutGroup;

    [Tooltip("한 줄에 표시할 물고기 아이템 개수")]
    [Min(1)]
    [SerializeField] private int _fishColumnCount = 4;

    [Tooltip("아이템 높이 비율. 2이면 높이가 너비의 2배입니다.")]
    [Min(0.1f)]
    [SerializeField] private float _fishCellHeightRatio = 2f;

    [Header("연결 레이아웃")]
    [Tooltip("해수 수조 레이아웃 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
    [SerializeField] private UIPopup _oceanLayoutUI;

    [Header("Entry Mode")]
    [SerializeField] private GameObject[] _layoutModeOnlyObjects;
    [SerializeField] private GameObject[] _waterGazeModeOnlyObjects;

    private bool _isInventoryOpened;
    private bool _isInventoryAnimating;
    private Vector2 _inventoryOpenedPosition;
    private NyangquariumEntryMode _entryMode = NyangquariumEntryMode.Layout;

    public NyangquariumEntryMode EntryMode => _entryMode;
    public bool IsLayoutMode => _entryMode == NyangquariumEntryMode.Layout;
    public bool IsWaterGazeMode => _entryMode == NyangquariumEntryMode.WaterGaze;

    private bool _isInitialized;
    private bool _isChangingLayout;

    private NyangQuariumFreshLayoutUISprite _nyangQuariumFreshLayoutUISprite;

    private NyangQuariumUIVisibilityToggle _uiVisibilityToggle;
    private NyangQuariumOutsideTouchArea _outsideTouchArea;
    public override void Init()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        ActiveInstance = this;
        _entryMode = NyangquariumEntryContext.Current;
        Bind<Button>(typeof(NyangQuariumFreshLayoutUIButton));


        _nyangQuariumFreshLayoutUISprite = GetComponent<NyangQuariumFreshLayoutUISprite>();

        _nyangQuariumFreshLayoutUISprite?.Init();

        _uiVisibilityToggle = GetComponent<NyangQuariumUIVisibilityToggle>();



        BindButtons();
        ResolveInventoryRect();
        BindOutsideTouchArea();
        RefreshFishGridCellSize();
        AddButtonListeners();
        InitializeUI();
        ApplyEntryMode();
        ResolveOceanLayoutUI();

        DebugTool.Log("[NyangQuariumFreshLayoutUI] 초기화 완료", DebugType.UI, this);
    }

    private void BindButtons()
    {
        _backButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.BackButton);
        _changeButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.ChangeButton);
        _freshWaterFishButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.FreshWaterFishButton);
        _freshWaterWeedButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.FreshWaterWeedButton);
        _closeButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.CloseButton);
    }

    private void ResolveInventoryRect()
    {
        if (_inventoryRect == null && _freshwaterLayoutPanel != null)
            _inventoryRect = _freshwaterLayoutPanel.GetComponent<RectTransform>();

        if (_inventoryRect != null)
            _inventoryOpenedPosition = _inventoryRect.anchoredPosition;
    }

    private void BindOutsideTouchArea()
    {
        if (_outsideTouchArea == null)
        {
            _outsideTouchArea =
                GetComponentInChildren<NyangQuariumOutsideTouchArea>(true);
        }

        if (_outsideTouchArea == null)
        {
            DebugTool.Warning(
                $"[{GetType().Name}] 바깥 터치 영역을 찾을 수 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        _outsideTouchArea.Clicked -= HandleOutsideTouch;
        _outsideTouchArea.Clicked += HandleOutsideTouch;
    }

    private void HandleOutsideTouch()
    {
        if (!_isInventoryOpened || _isInventoryAnimating)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        CloseInventory();

        DebugTool.Log(
            $"[{GetType().Name}] 배치 패널 바깥 터치로 인벤토리 닫기",
            DebugType.UI,
            this);
    }

    private void AddButtonListeners()
    {
        AddBackButton();
        AddChangeButton();
        AddFreshWaterFishButton();
        AddCloseButton();
        AddFilterButton();
    }

    private void InitializeUI()
    {
        _isInventoryOpened = false;
        _isInventoryAnimating = false;

        if (_inventoryRect != null)
        {
            _inventoryRect.DOKill();
            _inventoryRect.anchoredPosition = GetInventoryClosedPosition();
        }

        if (_filterTab != null)
            _filterTab.SetActive(false);

        if (_freshwaterLayoutPanel != null)
            _freshwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }

    public void SetNyangquariumEntryMode(NyangquariumEntryMode entryMode)
    {
        _entryMode = entryMode;
        ApplyEntryMode();
    }

    private void ApplyEntryMode()
    {
        SetModeObjectsActive(_layoutModeOnlyObjects, IsLayoutMode);
        SetModeObjectsActive(_waterGazeModeOnlyObjects, IsWaterGazeMode);
    }

    private static void SetModeObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
            return;

        foreach (GameObject target in objects)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }

    private void ResolveOceanLayoutUI()
    {
        if (_oceanLayoutUI == null)
            _oceanLayoutUI = FindSiblingLayout<NyangQuariumOceanLayoutUI>();

        if (_oceanLayoutUI == null)
            return;

        //_oceanLayoutUI.gameObject.SetActive(false);
        NyangquariumMainUIManager.Active?.RegisterOwnedContent(_oceanLayoutUI, false);
    }

    private T FindSiblingLayout<T>() where T : UIPopup
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform.root;
        return searchRoot != null ? searchRoot.GetComponentInChildren<T>(true) : null;
    }

    private void AddBackButton()
    {
        if (_backButton == null)
        {
            DebugTool.Warning("[NyangQuariumFreshLayoutUI] BackButton을 찾을 수 없습니다.", DebugType.UI, this);
            return;
        }

        _backButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            // 배치 패널이 열려 있거나 애니메이션 중이면 BackButton은 동작하지 않습니다.
            // 배치 패널은 FreshwaterLayoutPanel 내부 CloseButton으로만 닫습니다.
            if (_isInventoryOpened || _isInventoryAnimating)
            {
                DebugTool.Log(
                    "[NyangQuariumFreshLayoutUI] 배치 패널이 열려 있어 BackButton 입력을 무시합니다.",
                    DebugType.UI,
                    this);
                return;
            }

            if (NyangquariumMainUIManager.Active != null)
                NyangquariumMainUIManager.Active.ReturnToMain();
            else
                gameObject.SetActive(false);
        });
    }

    //private void AddChangeButton()
    //{
    //    if (_changeButton == null)
    //    {
    //        DebugTool.Warning("[NyangQuariumFreshLayoutUI] ChangeButton을 찾을 수 없습니다.", DebugType.UI, this);
    //        return;
    //    }

    //    _changeButton.onClick.AddListener(() =>
    //    {
    //        GameManager.Audio.PlaySfx("Main_SFX_Touch");

    //        if (_oceanLayoutUI == null)
    //        {
    //            DebugTool.Warning("[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI가 준비되지 않았습니다.", DebugType.UI, this);
    //            return;
    //        }

    //        PrepareLinkedLayout(_oceanLayoutUI);
    //        _oceanLayoutUI.gameObject.SetActive(true);
    //        _oceanLayoutUI.PlayOpenAnimation();
    //        gameObject.SetActive(false);

    //        DebugTool.Log("[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI로 변경", DebugType.UI, this);
    //    });
    //}
    private void AddChangeButton()
    {
        if (_changeButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshLayoutUI] ChangeButton을 찾을 수 없습니다.",
                DebugType.UI,
                this);
            return;
        }

        _changeButton.onClick.RemoveListener(OnClickChangeLayout);
        _changeButton.onClick.AddListener(OnClickChangeLayout);
    }
    private void OnClickChangeLayout()
    {
        if (_isChangingLayout)
            return;

        if (_oceanLayoutUI == null)
            ResolveOceanLayoutUI();

        if (_oceanLayoutUI == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI가 준비되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        _isChangingLayout = true;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        PrepareLinkedLayout(_oceanLayoutUI);

        _oceanLayoutUI.gameObject.SetActive(true);
        _oceanLayoutUI.PlayOpenAnimation();

        gameObject.SetActive(false);

        DebugTool.Log(
            "[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI로 변경",
            DebugType.UI,
            this);
    }

    private void PrepareLinkedLayout(UIPopup popup)
    {
        if (popup == null)
            return;

        NyangquariumEntryContext.Set(_entryMode);

        if (NyangquariumMainUIManager.Active != null)
        {
            NyangquariumMainUIManager.Active.PrepareOwnedContent(popup, _entryMode);
            return;
        }

        NotifyEntryMode(popup.gameObject);
    }

    private void NotifyEntryMode(GameObject content)
    {
        if (content == null)
            return;

        MonoBehaviour[] behaviours = content.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is INyangquariumEntryReceiver receiver)
                receiver.SetNyangquariumEntryMode(_entryMode);
        }
    }

    private void AddFreshWaterFishButton()
    {
        if (_freshWaterFishButton == null || _freshwaterLayoutPanel == null || _inventoryRect == null)
        {
            DebugTool.Warning("[NyangQuariumFreshLayoutUI] 담수 인벤토리 버튼 또는 패널이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _freshWaterFishButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            OpenInventory();
        });
    }

    private void AddCloseButton()
    {
        if (_closeButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshLayoutUI] CloseButton을 찾을 수 없습니다.",
                DebugType.UI,
                this);
            return;
        }

        _closeButton.onClick.RemoveListener(OnClickCloseButton);
        _closeButton.onClick.AddListener(OnClickCloseButton);
    }
    private void AddFilterButton()
    {
        if (_filterButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshLayoutUI] FilterButton이 연결되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        if (_filterTab == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshLayoutUI] FilterTab이 연결되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        _filterButton.onClick.RemoveListener(OnClickFilterButton);
        _filterButton.onClick.AddListener(OnClickFilterButton);
    }

    private void OnClickFilterButton()
    {
        if (_filterButton == null)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        bool isOpen = !_filterTab.activeSelf;
        _filterTab.SetActive(isOpen);

        DebugTool.Log(
            $"[NyangQuariumFreshLayoutUI] 필터 탭 {(isOpen ? "열기" : "닫기")}",
            DebugType.UI,
            this);
    }

    private void OnClickCloseButton()
    {
        if (!_isInventoryOpened && !_isInventoryAnimating)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        CloseInventory();
    }

    private void OpenInventory()
    {
        if (_isInventoryOpened || _isInventoryAnimating)
            return;

        _isInventoryAnimating = true;
        _freshwaterLayoutPanel.SetActive(true);
        RefreshFishGridCellSize();

        _inventoryRect.DOKill();
        _inventoryRect.anchoredPosition = GetInventoryClosedPosition();
        _inventoryRect
            .DOAnchorPos(_inventoryOpenedPosition, _inventoryMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _isInventoryOpened = true;
                _isInventoryAnimating = false;
                DebugTool.Log("[NyangQuariumFreshLayoutUI] 담수 통합 인벤토리 열기 완료", DebugType.UI, this);
            });
    }

    /// <summary>
    /// FishLayoutGroup 너비를 기준으로 아이템 크기를 자동 계산합니다.
    /// </summary>
    private void RefreshFishGridCellSize()
    {
        if (_fishLayoutGroup == null)
            return;

        GridLayoutGroup grid =
            _fishLayoutGroup.GetComponent<GridLayoutGroup>();

        if (grid == null)
        {
            DebugTool.Warning(
                $"[{GetType().Name}] FishLayoutGroup에 GridLayoutGroup이 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_fishLayoutGroup);

        float contentWidth = _fishLayoutGroup.rect.width;

        if (contentWidth <= 0f)
            return;

        int columnCount = Mathf.Max(1, _fishColumnCount);
        float horizontalPadding =
            grid.padding.left + grid.padding.right;
        float horizontalSpacing =
            grid.spacing.x * (columnCount - 1);

        float availableWidth =
            contentWidth - horizontalPadding - horizontalSpacing;

        if (availableWidth <= 0f)
            return;

        float cellWidth =
            availableWidth / columnCount;
        float cellHeight =
            cellWidth * Mathf.Max(0.1f, _fishCellHeightRatio);

        grid.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount =
            columnCount;
        grid.cellSize =
            new Vector2(cellWidth, cellHeight);
    }

    private void CloseInventory()
    {
        if (!_isInventoryOpened && !_isInventoryAnimating)
            return;

        _isInventoryOpened = false;
        _isInventoryAnimating = true;

        if (_filterTab != null)
            _filterTab.SetActive(false);

        _inventoryRect.DOKill();
        _inventoryRect
            .DOAnchorPos(GetInventoryClosedPosition(), _inventoryMoveDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _isInventoryAnimating = false;
                _freshwaterLayoutPanel.SetActive(false);
                DebugTool.Log("[NyangQuariumFreshLayoutUI] 담수 통합 인벤토리 닫기 완료", DebugType.UI, this);
            });
    }

    private Vector2 GetInventoryClosedPosition()
    {
        return _inventoryOpenedPosition + Vector2.down * _inventoryClosedOffsetY;
    }

    private void SetMainUIActive(bool isActive)
    {

        if (_changeButton != null)
            _changeButton.gameObject.SetActive(isActive);

        if (_freshWaterFishButton != null)
            _freshWaterFishButton.gameObject.SetActive(isActive);

        if (_freshWaterWeedButton != null)
            _freshWaterWeedButton.gameObject.SetActive(isActive);

        if (_backButton != null)
            _backButton.gameObject.SetActive(true);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        RefreshFishGridCellSize();
    }

    private void OnDisable()
    {
        _isChangingLayout = false;
        _isInventoryOpened = false;
        _isInventoryAnimating = false;

        if (_inventoryRect != null)
        {
            _inventoryRect.DOKill();
            _inventoryRect.anchoredPosition = GetInventoryClosedPosition();
        }

        if (_filterTab != null)
            _filterTab.SetActive(false);

        if (_freshwaterLayoutPanel != null)
            _freshwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }

    private void OnDestroy()
    {
        if (_outsideTouchArea != null)
            _outsideTouchArea.Clicked -= HandleOutsideTouch;


        if (ActiveInstance == this)
            ActiveInstance = null;
    }
}

public enum NyangQuariumFreshLayoutUIButton
{
    BackButton,
    WindowButton,
    ChangeButton,
    FreshWaterFishButton,
    FreshWaterWeedButton,
    CloseButton
}
