using Core.Managers;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangQuariumOceanLayoutUI : UIPopup
{
    [Header("공통 버튼")]
    [Tooltip("뒤로가기 버튼")]
    [SerializeField] private Button _backButton;

    [Tooltip("수조 유리 반응형 버튼")]
    [SerializeField] private Button _windowButton;

    [Tooltip("담수 수조로 변경하는 버튼")]
    [SerializeField] private Button _changeButton;

    [Header("해수 버튼")]
    [Tooltip("해수어 목록 버튼")]
    [SerializeField] private Button _oceanFishButton;

    [Tooltip("해수 수초 목록 버튼")]
    [SerializeField] private Button _oceanWaterWeedButton;

    [Header("해수 물고기 패널")]
    [Tooltip("해수어 레이아웃 패널 닫기 버튼")]
    [SerializeField] private Button _oceanwaterLayoutCloseButton;

    [Tooltip("해수어 목록이 표시되는 패널")]
    [SerializeField] private GameObject _oceanwaterLayoutPanel;

    private UIPopup _freshLayoutUI;
    private bool _isFishPanelOpened;

    public override void Init()
    {
        Bind<Button>(typeof(NyangQuariumOceanLayoutUIButton));

        BindButtons();
        AddButtonListeners();
        InitializeUI();
        InitFreshLayoutUI();

        DebugTool.Log("[NyangQuariumOceanLayoutUI] 초기화 완료", DebugType.UI, this);
    }

    private void BindButtons()
    {
        _backButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.BackButton);
        _windowButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.WindowButton);
        _changeButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.ChangeButton);
        _oceanFishButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanFishButton);
        _oceanWaterWeedButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanWaterWeedButton);
        _oceanwaterLayoutCloseButton = Get<Button>((int)NyangQuariumOceanLayoutUIButton.OceanwaterLayoutCloseButton);
    }

    private void AddButtonListeners()
    {
        AddBackButton();
        AddChangeButton();
        AddOceanFishButton();
        AddOceanwaterLayoutCloseButton();
    }

    private void InitializeUI()
    {
        _isFishPanelOpened = false;

        if (_oceanwaterLayoutPanel != null)
            _oceanwaterLayoutPanel.SetActive(false);

        SetMainUIActive(true);
    }

    private void InitFreshLayoutUI()
    {
        GameManager.UI.ShowPopupUI<UIPopup>(
            KeyContainer.Prefabs.NyangQuariumFreshLayoutUI,
            popup => _freshLayoutUI = popup,
            false);
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

            if (_isFishPanelOpened)
            {
                CloseFishPanelImmediately();
                return;
            }

            gameObject.SetActive(false);
        });
    }

    private void AddChangeButton()
    {
        if (_changeButton == null)
        {
            DebugTool.Warning("[NyangQuariumOceanLayoutUI] ChangeButton을 찾을 수 없습니다.", DebugType.UI, this);
            return;
        }

        _changeButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_freshLayoutUI == null)
            {
                DebugTool.Warning("[NyangQuariumOceanLayoutUI] 담수 레이아웃 UI가 준비되지 않았습니다.", DebugType.UI, this);
                return;
            }

            _freshLayoutUI.gameObject.SetActive(true);
            _freshLayoutUI.PlayOpenAnimation();

            gameObject.SetActive(false);

            DebugTool.Log("[NyangQuariumOceanLayoutUI] 담수 레이아웃 UI로 변경", DebugType.UI, this);
        });
    }

    private void AddOceanFishButton()
    {
        if (_oceanFishButton == null || _oceanwaterLayoutPanel == null)
        {
            DebugTool.Warning("[NyangQuariumOceanLayoutUI] 해수어 버튼 또는 패널이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _oceanFishButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_isFishPanelOpened)
                return;

            _isFishPanelOpened = true;

            SetMainUIActive(false);
            _oceanwaterLayoutPanel.SetActive(true);

            DebugTool.Log("[NyangQuariumOceanLayoutUI] 해수어 레이아웃 패널 활성화", DebugType.UI, this);
        });
    }

    private void AddOceanwaterLayoutCloseButton()
    {
        if (_oceanwaterLayoutCloseButton == null || _oceanwaterLayoutPanel == null)
        {
            DebugTool.Warning("[NyangQuariumOceanLayoutUI] 해수어 패널 닫기 버튼이 연결되지 않았습니다.", DebugType.UI, this);
            return;
        }

        _oceanwaterLayoutCloseButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (!_isFishPanelOpened)
                return;

            DebugTool.Log("[NyangQuariumOceanLayoutUI] 해수어 패널 닫기 요청", DebugType.UI, this);
        });
    }

    public void CompleteOceanwaterPanelClose()
    {
        if (_oceanwaterLayoutPanel == null)
            return;

        _oceanwaterLayoutPanel.SetActive(false);
        _isFishPanelOpened = false;

        SetMainUIActive(true);

        DebugTool.Log("[NyangQuariumOceanLayoutUI] 해수어 패널 닫기 완료", DebugType.UI, this);
    }

    private void CloseFishPanelImmediately()
    {
        if (_oceanwaterLayoutPanel != null)
            _oceanwaterLayoutPanel.SetActive(false);

        _isFishPanelOpened = false;

        SetMainUIActive(true);
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
        _isFishPanelOpened = false;

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
    OceanWaterWeedButton,
    OceanwaterLayoutCloseButton
}