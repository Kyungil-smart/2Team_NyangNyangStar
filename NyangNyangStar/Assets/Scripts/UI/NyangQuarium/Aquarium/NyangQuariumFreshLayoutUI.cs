using Core.Managers;
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

    [Tooltip("수조 유리 반응형 버튼")]
    [SerializeField] private Button _windowButton;

    [Tooltip("해수 수조로 변경하는 버튼")]
    [SerializeField] private Button _changeButton;

    [Header("담수 버튼")]
    [Tooltip("담수어 목록 버튼")]
    [SerializeField] private Button _freshWaterFishButton;

    [Tooltip("담수 수초 목록 버튼")]
    [SerializeField] private Button _freshWaterWeedButton;

    [Header("담수 물고기 패널")]
    [Tooltip("담수어 레이아웃 패널 닫기 버튼")]
    [SerializeField] private Button _freshwaterLayoutCloseButton;

    [Tooltip("담수어 목록이 표시되는 패널")]
    [SerializeField] private GameObject _freshwaterLayoutPanel;

    [Header("연결 레이아웃")]
    [Tooltip("해수 수조 레이아웃 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
    [SerializeField] private UIPopup _oceanLayoutUI;

    [Header("Entry Mode")]
    [SerializeField] private GameObject[] _layoutModeOnlyObjects;
    [SerializeField] private GameObject[] _waterGazeModeOnlyObjects;

    private bool _isFishPanelOpened;
    private NyangquariumEntryMode _entryMode = NyangquariumEntryMode.Layout;

    public NyangquariumEntryMode EntryMode => _entryMode;
    public bool IsLayoutMode => _entryMode == NyangquariumEntryMode.Layout;
    public bool IsWaterGazeMode => _entryMode == NyangquariumEntryMode.WaterGaze;

    public override void Init()
    {
        ActiveInstance = this;
        _entryMode = NyangquariumEntryContext.Current;
        Bind<Button>(typeof(NyangQuariumFreshLayoutUIButton));

        BindButtons();
        AddButtonListeners();
        InitializeUI();
        ApplyEntryMode();
        ResolveOceanLayoutUI();

        DebugTool.Log("[NyangQuariumFreshLayoutUI] 초기화 완료", DebugType.UI, this);
    }

    private void BindButtons()
    {
        _backButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.BackButton);
        _windowButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.WindowButton);
        _changeButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.ChangeButton);
        _freshWaterFishButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.FreshWaterFishButton);
        _freshWaterWeedButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.FreshWaterWeedButton);
        _freshwaterLayoutCloseButton = Get<Button>((int)NyangQuariumFreshLayoutUIButton.FreshwaterLayoutCloseButton);
    }

    private void AddButtonListeners()
    {
        AddBackButton();
        AddChangeButton();
        AddFreshWaterFishButton();
        AddFreshwaterLayoutCloseButton();
    }

    private void InitializeUI()
    {
        _isFishPanelOpened = false;

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

        _oceanLayoutUI.gameObject.SetActive(false);
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

            if (_isFishPanelOpened)
            {
                CloseFishPanelImmediately();
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
            DebugTool.Warning("[NyangQuariumFreshLayoutUI] ChangeButton을 찾을 수 없습니다.", DebugType.UI, this);
            return;
        }

        _changeButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_oceanLayoutUI == null)
            {
                DebugTool.Warning("[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI가 준비되지 않았습니다.", DebugType.UI, this);
                return;
            }

            PrepareLinkedLayout(_oceanLayoutUI);
            _oceanLayoutUI.gameObject.SetActive(true);
            _oceanLayoutUI.PlayOpenAnimation();

            gameObject.SetActive(false);

            DebugTool.Log("[NyangQuariumFreshLayoutUI] 해수 레이아웃 UI로 변경", DebugType.UI, this);
        });
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
        if (_freshWaterFishButton == null || _freshwaterLayoutPanel == null)
        {
            DebugTool.Warning("[NyangQuariumFreshLayoutUI] 담수어 버튼 또는 패널이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _freshWaterFishButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_isFishPanelOpened)
                return;

            _isFishPanelOpened = true;

            SetMainUIActive(false);
            _freshwaterLayoutPanel.SetActive(true);

            DebugTool.Log("[NyangQuariumFreshLayoutUI] 담수어 레이아웃 패널 활성화", DebugType.UI, this);
        });
    }

    private void AddFreshwaterLayoutCloseButton()
    {
        if (_freshwaterLayoutCloseButton == null || _freshwaterLayoutPanel == null)
        {
            DebugTool.Warning("[NyangQuariumFreshLayoutUI] 담수어 패널 닫기 버튼이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _freshwaterLayoutCloseButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (!_isFishPanelOpened)
                return;

            DebugTool.Log("[NyangQuariumFreshLayoutUI] 담수어 패널 닫기 요청", DebugType.UI, this);
        });
    }

    public void CompleteFreshwaterPanelClose()
    {
        if (_freshwaterLayoutPanel == null)
            return;

        _freshwaterLayoutPanel.SetActive(false);
        _isFishPanelOpened = false;

        SetMainUIActive(true);

        DebugTool.Log("[NyangQuariumFreshLayoutUI] 담수어 패널 닫기 완료", DebugType.UI, this);
    }

    private void CloseFishPanelImmediately()
    {
        if (_freshwaterLayoutPanel != null)
            _freshwaterLayoutPanel.SetActive(false);

        _isFishPanelOpened = false;

        SetMainUIActive(true);
    }

    private void SetMainUIActive(bool isActive)
    {
        if (_windowButton != null)
            _windowButton.gameObject.SetActive(isActive);

        if (_changeButton != null)
            _changeButton.gameObject.SetActive(isActive);

        if (_freshWaterFishButton != null)
            _freshWaterFishButton.gameObject.SetActive(isActive);

        if (_freshWaterWeedButton != null)
            _freshWaterWeedButton.gameObject.SetActive(isActive);

        if (_backButton != null)
            _backButton.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        _isFishPanelOpened = false;

        if (_freshwaterLayoutPanel != null)
            _freshwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(ActiveInstance, this))
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
    FreshwaterLayoutCloseButton
}
