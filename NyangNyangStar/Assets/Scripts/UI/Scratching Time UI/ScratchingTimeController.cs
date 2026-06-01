using System;
using Services.Enums;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스크래칭 타임 선택 화면 UI를 관리합니다.
/// 단계 선택 버튼과 일일/주간 START 버튼 입력을 매니저에 전달합니다.
/// </summary>
public class ScratchingTimeController : UIBase
{
    [Header("Close Button")]
    [SerializeField] private Button _closeButton;

    [Header("Stage Button")]
    [SerializeField] private Button[] _stageButtons = new Button[4];

    [Header("Daily Stage UI")]
    [SerializeField] private TMP_Text _dailyRemainingCountText;
    [SerializeField] private TMP_Text _dailyCountSlashText;
    [SerializeField] private TMP_Text _dailyMaxCountText;
    [SerializeField] private Button _dailyStartButton;

    [Header("Weekly Stage UI")]
    [SerializeField] private TMP_Text _weeklyRemainingCountText;
    [SerializeField] private TMP_Text _weeklyCountSlashText;
    [SerializeField] private TMP_Text _weeklyMaxCountText;
    [SerializeField] private Button _weeklyStartButton;

    public Action OnCloseClicked;
    public Action<int> OnStageSelected;
    public Action<StageType> OnStageStartClicked;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        RegisterButtonEvents();
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
    }

    /// <summary>
    /// 단계 버튼과 일일/주간 START 버튼 이벤트를 등록합니다.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(RaiseCloseClicked);

        if (_stageButtons != null)
        {
            for (int i = 0; i < _stageButtons.Length; i++)
            {
                int stage = i + 1;
                _stageButtons[i].onClick.AddListener(() => SelectStage(stage));
            }
        }

        if (_dailyStartButton != null)
            _dailyStartButton.onClick.AddListener(() => StartSelectedStage(StageType.Daily));

        if (_weeklyStartButton != null)
            _weeklyStartButton.onClick.AddListener(() => StartSelectedStage(StageType.Weekly));
    }

    /// <summary>
    /// 등록된 단계 버튼과 일일/주간 START 버튼 이벤트를 해제합니다.
    /// </summary>
    private void UnregisterButtonEvents()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(RaiseCloseClicked);

        if (_stageButtons != null)
        {
            foreach (Button button in _stageButtons)
            {
                if (button == null)
                    continue;

                button.onClick.RemoveAllListeners();
            }
        }

        if (_dailyStartButton != null)
            _dailyStartButton.onClick.RemoveAllListeners();

        if (_weeklyStartButton != null)
            _weeklyStartButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 선택 화면을 표시합니다.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 선택 화면을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 선택 화면을 초기 상태로 되돌립니다.
    /// 단계 버튼만 활성화하고 일일/주간 START 버튼은 비활성화합니다.
    /// </summary>
    public void InitView()
    {
        SetStageButtonsInteractable(true);
        SetStageStartButtonsInteractable(false, false);
        ClearStageCardInfo();
    }

    /// <summary>
    /// 닫기 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    private void RaiseCloseClicked()
    {
        ClearSelectedButton();
        OnCloseClicked?.Invoke();
    }

    /// <summary>
    /// 단계 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    /// <param name="stage">선택한 스테이지 단계</param>
    private void SelectStage(int stage)
    {
        ClearSelectedButton();
        OnStageSelected?.Invoke(stage);
    }

    /// <summary>
    /// 선택한 단계의 일일/주간 START 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    /// <param name="stageType">시작할 스테이지 타입</param>
    private void StartSelectedStage(StageType stageType)
    {
        ClearSelectedButton();
        OnStageStartClicked?.Invoke(stageType);
    }

    /// <summary>
    /// 버튼 클릭 후 Space 또는 Enter로 같은 버튼이 다시 눌리지 않도록 선택 상태를 해제합니다.
    /// </summary>
    private void ClearSelectedButton()
    {
        EventSystem.current?.SetSelectedGameObject(null);
    }

    /// <summary>
    /// 선택한 단계 기준으로 일일/주간 카드의 남은 횟수와 보상 경험치를 출력합니다.
    /// </summary>
    /// <param name="dailyRemainingCount">일일 스테이지 남은 도전 횟수</param>
    /// <param name="dailyMaxCount">일일 스테이지 최대 도전 횟수</param>
    /// <param name="canStartDaily">일일 스테이지 시작 가능 여부</param>
    /// <param name="weeklyRemainingCount">주간 스테이지 남은 도전 횟수</param>
    /// <param name="weeklyMaxCount">주간 스테이지 최대 도전 횟수</param>
    /// <param name="canStartWeekly">주간 스테이지 시작 가능 여부</param>
    public void PrintStageCardInfo(
        int dailyRemainingCount,
        int dailyMaxCount,
        bool canStartDaily,
        int weeklyRemainingCount,
        int weeklyMaxCount,
        bool canStartWeekly)
    {
        SetText(_dailyRemainingCountText, dailyRemainingCount.ToString());
        SetText(_dailyCountSlashText, "/");
        SetText(_dailyMaxCountText, dailyMaxCount.ToString());

        SetText(_weeklyRemainingCountText, weeklyRemainingCount.ToString());
        SetText(_weeklyCountSlashText, "/");
        SetText(_weeklyMaxCountText, weeklyMaxCount.ToString());

        SetStageStartButtonsInteractable(canStartDaily, canStartWeekly);
    }

    /// <summary>
    /// 일일/주간 카드의 출력 정보를 기본값으로 초기화합니다.
    /// </summary>
    public void ClearStageCardInfo()
    {
        SetText(_dailyRemainingCountText, "-");
        SetText(_dailyCountSlashText, "/");
        SetText(_dailyMaxCountText, "-");

        SetText(_weeklyRemainingCountText, "-");
        SetText(_weeklyCountSlashText, "/");
        SetText(_weeklyMaxCountText, "-");
    }

    /// <summary>
    /// 단계 버튼 전체의 활성화 상태를 변경합니다.
    /// </summary>
    /// <param name="value">활성화 여부</param>
    public void SetStageButtonsInteractable(bool value)
    {
        if (_stageButtons == null)
            return;

        foreach (Button button in _stageButtons)
        {
            if (button == null)
                continue;

            button.interactable = value;
        }
    }

    /// <summary>
    /// 지정한 단계 버튼 하나의 활성화 상태를 변경합니다.
    /// </summary>
    /// <param name="stage">변경할 단계 값</param>
    /// <param name="value">활성화 여부</param>
    public void SetStageButtonInteractable(int stage, bool value)
    {
        if (_stageButtons == null)
            return;

        int index = stage - 1;

        if (index < 0 || index >= _stageButtons.Length)
            return;

        if (_stageButtons[index] == null)
            return;

        _stageButtons[index].interactable = value;
    }

    /// <summary>
    /// 일일/주간 START 버튼의 활성화 상태를 변경합니다.
    /// </summary>
    /// <param name="canStartDaily">일일 스테이지 START 버튼 활성화 여부</param>
    /// <param name="canStartWeekly">주간 스테이지 START 버튼 활성화 여부</param>
    public void SetStageStartButtonsInteractable(bool canStartDaily, bool canStartWeekly)
    {
        if (_dailyStartButton != null)
            _dailyStartButton.interactable = canStartDaily;

        if (_weeklyStartButton != null)
            _weeklyStartButton.interactable = canStartWeekly;
    }

    /// <summary>
    /// 선택 화면의 모든 버튼 활성화 상태를 변경합니다.
    /// </summary>
    /// <param name="value">활성화 여부</param>
    public void SetAllButtonsInteractable(bool value)
    {
        SetStageButtonsInteractable(value);
        SetStageStartButtonsInteractable(value, value);
    }

    /// <summary>
    /// 텍스트 컴포넌트가 연결되어 있을 때만 문자열을 출력합니다.
    /// </summary>
    /// <param name="text">출력 대상 텍스트</param>
    /// <param name="value">출력할 문자열</param>
    private void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    public override void Init()
    {
        Bind<Button>(typeof(StageButtons));
        Bind<TMP_Text>(typeof(StageTexts));
        
        GetButtons();
        GetTexts();
    }

    private void GetButtons()
    {
        _closeButton = Get<Button>((int)StageButtons.CloseButton);
        _stageButtons[0] = Get<Button>((int)StageButtons.Stage1);
        _stageButtons[1] = Get<Button>((int)StageButtons.Stage2);
        _stageButtons[2] = Get<Button>((int)StageButtons.Stage3);
        _stageButtons[3] = Get<Button>((int)StageButtons.Stage4);
        _dailyStartButton = Get<Button>((int)StageButtons.DailyStartButton);
        _weeklyStartButton = Get<Button>((int)StageButtons.WeeklyStartButton);
    }

    private void GetTexts()
    {
        _dailyRemainingCountText = Get<TMP_Text>((int)StageTexts.DailyRemainChallengeCount);
        _dailyCountSlashText = Get<TMP_Text>((int)StageTexts.DailyChallengeCountSlash);
        _dailyMaxCountText = Get<TMP_Text>((int)StageTexts.DailyMaxChallengeCount);
        _weeklyRemainingCountText = Get<TMP_Text>((int)StageTexts.WeeklyRemainChallengeCount);
        _weeklyCountSlashText = Get<TMP_Text>((int)StageTexts.WeeklyChallengeCountSlash);
        _weeklyMaxCountText = Get<TMP_Text>((int)StageTexts.WeeklyMaxChallengeCount);
    }

    private enum StageButtons
    {
        CloseButton,
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        DailyStartButton,
        WeeklyStartButton
    }

    private enum StageTexts
    {
        DailyRemainChallengeCount,
        DailyChallengeCountSlash,
        DailyMaxChallengeCount,
        WeeklyRemainChallengeCount,
        WeeklyChallengeCountSlash,
        WeeklyMaxChallengeCount
    }
}
