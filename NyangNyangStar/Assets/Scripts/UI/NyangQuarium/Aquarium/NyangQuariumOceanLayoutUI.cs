using Core.Managers;
using DG.Tweening;
using UI.Base;
using UI.NyangQuarium;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumOceanLayoutUI : UIPopup, INyangquariumEntryReceiver
{
    [Header("공통 버튼")]
    [Tooltip("뒤로가기 버튼")]
    [SerializeField] private Button _backButton;

    [Tooltip("담수 수조로 변경하는 버튼")]
    [SerializeField] private Button _changeButton;

    [Header("해수 버튼")]
    [Tooltip("해수 통합 인벤토리 버튼")]
    [SerializeField] private Button _oceanFishButton;

    [Tooltip("현재 미사용 버튼")]
    [SerializeField] private Button _oceanWaterWeedButton;

    [Tooltip("해수 통합 인벤토리 닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("해수 통합 인벤토리")]
    [Tooltip("해수 통합 인벤토리 패널")]
    [SerializeField] private GameObject _oceanwaterLayoutPanel;

    [Header("해수 필터 패널")]
    [Tooltip("필터 패널을 열고 닫는 버튼")]
    [SerializeField] private Button _filterButton;

    [Tooltip("전체, 해수, 기수 필터가 들어 있는 패널")]
    [SerializeField] private GameObject _filterTab;

    [Tooltip("자연 요소 필터 목록이 들어 있는 패널")]
    [SerializeField] private GameObject _natureFilterTab;

    [Tooltip("현재 인벤토리 카테고리를 확인할 목록 UI")]
    [SerializeField] private NyangQuariumFishInventoryListUI _inventoryListUI;

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
    [Tooltip("담수 수조 레이아웃 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
    [SerializeField] private UIPopup _freshLayoutUI;

    [Header("Entry Mode")]
    [SerializeField] private GameObject[] _layoutModeOnlyObjects;
    [SerializeField] private GameObject[] _waterGazeModeOnlyObjects;

    private bool _isInventoryOpened;
    private bool _isInventoryAnimating;
    private bool _isInitialized;
    private bool _isChangingLayout;
    private Vector2 _inventoryOpenedPosition;
    private NyangquariumEntryMode _entryMode = NyangquariumEntryMode.Layout;

    public NyangquariumEntryMode EntryMode => _entryMode;
    public bool IsLayoutMode => _entryMode == NyangquariumEntryMode.Layout;
    public bool IsWaterGazeMode => _entryMode == NyangquariumEntryMode.WaterGaze;

    private NyangQuariumOceanLayoutUISprite _nyangQuariumOceanLayoutUISprite;
    private NyangQuariumUIVisibilityToggle _uiVisibilityToggle;
    private NyangQuariumOutsideTouchArea _outsideTouchArea;


    [SerializeField] private Button _fishTabButton;

    public override void Init()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        _entryMode = NyangquariumEntryContext.Current;
        Bind<Button>(typeof(NyangQuariumOceanLayoutUIButton));

        _nyangQuariumOceanLayoutUISprite = GetComponent<NyangQuariumOceanLayoutUISprite>();

        _nyangQuariumOceanLayoutUISprite?.Init();

        _uiVisibilityToggle = GetComponent<NyangQuariumUIVisibilityToggle>();

        BindButtons();
        ResolveInventoryRect();
        BindOutsideTouchArea();
        RefreshFishGridCellSize();
        AddButtonListeners();
        InitializeUI();
        ApplyEntryMode();
        ResolveFreshLayoutUI();

        DebugTool.Log(
            "[NyangQuariumOceanLayoutUI] 초기화 완료",
            DebugType.UI,
            this);
    }
    private void BindButtons()
    {
        _backButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.BackButton);
        _changeButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.ChangeButton);
        _oceanFishButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanFishButton);
        _oceanWaterWeedButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanWaterWeedButton);
        _closeButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.CloseButton);
    }

    private void ResolveInventoryRect()
    {
        if (_inventoryRect == null && _oceanwaterLayoutPanel != null)
            _inventoryRect = _oceanwaterLayoutPanel.GetComponent<RectTransform>();

        if (_inventoryListUI == null && _oceanwaterLayoutPanel != null)
        {
            _inventoryListUI =
                _oceanwaterLayoutPanel.GetComponentInChildren<
                    NyangQuariumFishInventoryListUI>(true);
        }

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
        AddOceanFishButton();
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
        {
            _filterTab.SetActive(false);
        }

        if (_natureFilterTab != null)
        {
            _natureFilterTab.SetActive(false);
        }

        if (_oceanwaterLayoutPanel != null)
        {
            _oceanwaterLayoutPanel.SetActive(false);
        }

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

    private void ResolveFreshLayoutUI()
    {
        if (_freshLayoutUI == null)
            _freshLayoutUI = NyangQuariumFreshLayoutUI.ActiveInstance;

        if (_freshLayoutUI == null)
            _freshLayoutUI = FindSiblingLayout<NyangQuariumFreshLayoutUI>();

        if (_freshLayoutUI == null)
            return;

        NyangquariumMainUIManager.Active?.RegisterOwnedContent(_freshLayoutUI, false);
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
            DebugTool.Warning("[NyangQuariumOceanLayoutUI] BackButton을 찾을 수 없습니다.", DebugType.UI, this);
            return;
        }

        _backButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            // 배치 패널이 열려 있거나 애니메이션 중이면 BackButton은 동작하지 않습니다.
            // 배치 패널은 OceanwaterLayoutPanel 내부 CloseButton으로만 닫습니다.
            if (_isInventoryOpened || _isInventoryAnimating)
            {
                DebugTool.Log(
                    "[NyangQuariumOceanLayoutUI] 배치 패널이 열려 있어 BackButton 입력을 무시합니다.",
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

    private void AddChangeButton()
    {
        if (_changeButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumOceanLayoutUI] ChangeButton을 찾을 수 없습니다.",
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

        if (_freshLayoutUI == null)
            ResolveFreshLayoutUI();

        if (_freshLayoutUI == null)
        {
            DebugTool.Warning(
                "[NyangQuariumOceanLayoutUI] 담수 레이아웃 UI가 준비되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (NyangquariumMainUIManager.Active != null)
        {
            bool transitionStarted =
                NyangquariumMainUIManager.Active.OpenOwnedContentWithTransition(
                _freshLayoutUI,
                _entryMode,
                playClickSfx: false);

            if (!transitionStarted)
                return;

            _isChangingLayout = true;
        }
        else
        {
            _isChangingLayout = true;
            PrepareLinkedLayout(_freshLayoutUI);
            _freshLayoutUI.gameObject.SetActive(true);
            _freshLayoutUI.PlayOpenAnimation();
            gameObject.SetActive(false);
        }

        DebugTool.Log(
            "[NyangQuariumOceanLayoutUI] 담수 레이아웃 UI로 변경",
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

    private void AddOceanFishButton()
    {
        if (_oceanFishButton == null || _oceanwaterLayoutPanel == null || _inventoryRect == null)
        {
            DebugTool.Warning("[NyangQuariumOceanLayoutUI] 해수 인벤토리 버튼 또는 패널이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _oceanFishButton.onClick.AddListener(() =>
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
                "[NyangQuariumOceanLayoutUI] CloseButton을 찾을 수 없습니다.",
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
                "[NyangQuariumOceanLayoutUI] FilterButton이 연결되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        if (_filterTab == null)
        {
            DebugTool.Warning(
                "[NyangQuariumOceanLayoutUI] FilterTab이 연결되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        _filterButton.onClick.RemoveListener(OnClickFilterButton);
        _filterButton.onClick.AddListener(OnClickFilterButton);
    }

    private void OnClickFilterButton()
    {
        GameObject currentFilterTab = GetCurrentFilterTab();

        if (currentFilterTab == null)
        {
            DebugTool.Warning(
                "[NyangQuariumOceanLayoutUI] 현재 카테고리의 FilterTab이 연결되지 않았습니다.",
                DebugType.UI,
                this);
            return;
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_filterTab != null && _filterTab != currentFilterTab)
            _filterTab.SetActive(false);

        if (_natureFilterTab != null &&
            _natureFilterTab != currentFilterTab)
        {
            _natureFilterTab.SetActive(false);
        }

        currentFilterTab.SetActive(
            !currentFilterTab.activeSelf);
    }

    private GameObject GetCurrentFilterTab()
    {
        if (_inventoryListUI == null && _oceanwaterLayoutPanel != null)
        {
            _inventoryListUI =
                _oceanwaterLayoutPanel.GetComponentInChildren<
                    NyangQuariumFishInventoryListUI>(true);
        }

        if (_inventoryListUI == null)
            return _filterTab;

        return _inventoryListUI.CurrentCategory ==
               NyangQuariumPlacementCategory.Nature
            ? _natureFilterTab
            : _filterTab;
    }

    private void CloseFilterTabs()
    {
        if (_filterTab != null)
            _filterTab.SetActive(false);

        if (_natureFilterTab != null)
            _natureFilterTab.SetActive(false);
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
        _oceanwaterLayoutPanel.SetActive(true);

        SelectDefaultFishTabButton();

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
                DebugTool.Log("[NyangQuariumOceanLayoutUI] 해수 통합 인벤토리 열기 완료", DebugType.UI, this);
            });
    }

    /// <summary>
    /// 해수 배치 패널을 열 때 기본 카테고리 탭을 관상어 버튼 선택 상태로 만듭니다.
    /// Button 컴포넌트의 Selected Color를 그대로 사용합니다.
    /// </summary>
    private void SelectDefaultFishTabButton()
    {
        if (_fishTabButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumOceanLayoutUI] FishTabButton이 연결되지 않아 기본 탭 선택 색상을 적용할 수 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        _fishTabButton.Select();

        DebugTool.Log(
            "[NyangQuariumOceanLayoutUI] 배치 패널 기본 선택 탭: 관상어",
            DebugType.UI,
            this);
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

        CloseFilterTabs();

        _inventoryRect.DOKill();
        _inventoryRect
            .DOAnchorPos(GetInventoryClosedPosition(), _inventoryMoveDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _isInventoryAnimating = false;
                _oceanwaterLayoutPanel.SetActive(false);
                DebugTool.Log("[NyangQuariumOceanLayoutUI] 해수 통합 인벤토리 닫기 완료", DebugType.UI, this);
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

        if (_oceanFishButton != null)
            _oceanFishButton.gameObject.SetActive(isActive);

        if (_oceanWaterWeedButton != null)
            _oceanWaterWeedButton.gameObject.SetActive(isActive);

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

        CloseFilterTabs();

        if (_oceanwaterLayoutPanel != null)
            _oceanwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }

    private void OnDestroy()
    {
        if (_outsideTouchArea != null)
            _outsideTouchArea.Clicked -= HandleOutsideTouch;
    }
}

public enum NyangQuariumOceanLayoutUIButton
{
    BackButton,
    WindowButton,
    ChangeButton,
    OceanFishButton,
    OceanWaterWeedButton,
    CloseButton
}
