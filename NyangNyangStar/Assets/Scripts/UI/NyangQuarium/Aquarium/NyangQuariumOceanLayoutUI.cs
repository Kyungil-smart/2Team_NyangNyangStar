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

    [Tooltip("수조 유리 반응형 버튼")]
    [SerializeField] private Button _windowButton;

    [Tooltip("담수 수조로 변경하는 버튼")]
    [SerializeField] private Button _changeButton;

    [Header("해수 버튼")]
    [Tooltip("해수 통합 인벤토리 버튼")]
    [SerializeField] private Button _oceanFishButton;

    [Tooltip("현재 미사용 버튼")]
    [SerializeField] private Button _oceanWaterWeedButton;

    [Header("해수 통합 인벤토리")]
    [Tooltip("해수 통합 인벤토리 패널")]
    [SerializeField] private GameObject _oceanwaterLayoutPanel;

    [Tooltip("비워두면 패널의 RectTransform을 자동으로 사용합니다.")]
    [SerializeField] private RectTransform _inventoryRect;

    [Tooltip("열린 위치에서 아래쪽으로 이동할 거리")]
    [SerializeField] private float _inventoryClosedOffsetY = 610f;

    [Tooltip("인벤토리 열기/닫기 시간")]
    [SerializeField] private float _inventoryMoveDuration = 0.3f;

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

    public override void Init()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        _entryMode = NyangquariumEntryContext.Current;
        Bind<Button>(typeof(NyangQuariumOceanLayoutUIButton));

        _nyangQuariumOceanLayoutUISprite =
            GetComponent<NyangQuariumOceanLayoutUISprite>();

        _nyangQuariumOceanLayoutUISprite?.Init();

        BindButtons();
        ResolveInventoryRect();
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
        _windowButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.WindowButton);
        _changeButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.ChangeButton);
        _oceanFishButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanFishButton);
        _oceanWaterWeedButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanWaterWeedButton);
    }

    private void ResolveInventoryRect()
    {
        if (_inventoryRect == null && _oceanwaterLayoutPanel != null)
            _inventoryRect = _oceanwaterLayoutPanel.GetComponent<RectTransform>();

        if (_inventoryRect != null)
            _inventoryOpenedPosition = _inventoryRect.anchoredPosition;
    }

    private void AddButtonListeners()
    {
        AddBackButton();
        AddChangeButton();
        AddOceanFishButton();
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

        if (_oceanwaterLayoutPanel != null)
            _oceanwaterLayoutPanel.SetActive(false);

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

            if (_isInventoryOpened || _isInventoryAnimating)
            {
                CloseInventory();
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

        _isChangingLayout = true;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        PrepareLinkedLayout(_freshLayoutUI);

        _freshLayoutUI.gameObject.SetActive(true);
        _freshLayoutUI.PlayOpenAnimation();

        gameObject.SetActive(false);

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

    private void OpenInventory()
    {
        if (_isInventoryOpened || _isInventoryAnimating)
            return;

        _isInventoryAnimating = true;
        _oceanwaterLayoutPanel.SetActive(true);

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

    private void CloseInventory()
    {
        if (!_isInventoryOpened && !_isInventoryAnimating)
            return;

        _isInventoryOpened = false;
        _isInventoryAnimating = true;

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
        if (_windowButton != null)
            _windowButton.gameObject.SetActive(isActive);

        if (_changeButton != null)
            _changeButton.gameObject.SetActive(isActive);

        if (_oceanFishButton != null)
            _oceanFishButton.gameObject.SetActive(isActive);

        if (_oceanWaterWeedButton != null)
            _oceanWaterWeedButton.gameObject.SetActive(isActive);

        if (_backButton != null)
            _backButton.gameObject.SetActive(true);
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

        if (_oceanwaterLayoutPanel != null)
            _oceanwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }
}

public enum NyangQuariumOceanLayoutUIButton
{
    BackButton,
    WindowButton,
    ChangeButton,
    OceanFishButton,
    OceanWaterWeedButton
}
