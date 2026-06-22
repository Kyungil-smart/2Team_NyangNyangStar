using Core.Managers;
using System;
using DG.Tweening;
using Services.Enums;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

// 스크래칭 타임 선택 화면 UI를 관리
// 단계 선택 버튼과 일일/주간 START 버튼 입력을 매니저에 전달
public class ScratchingTimeController : UIBase
{
    [Header("Close Button")]
    [SerializeField] public Button _closeButton;

    [Header("Close Animation")]
    [Tooltip("비우면 이 UI 루트 전체가 줄어듭니다. EventPopupPanel만 지정하면 배경은 그대로라 체감이 거의 없습니다.")]
    [SerializeField] private Transform _closeRoot;
    [Tooltip("닫힐 때 도달하는 스케일 (1=원크기, 0.8=80%). 값을 크게 바꿔야 눈에 띕니다.")]
    [SerializeField] private float _contentScaleFrom = 0.8f;
    [Tooltip("닫기 연출 시간(초). 0.15는 너무 짧아 스케일 변화가 잘 안 보입니다.")]
    [SerializeField] private float _closeDuration = 0.25f;

    private CanvasGroup _closeCanvasGroup;
    private Sequence _closeSequence;
    private bool _isCloseAnimating;
    private bool _isCloseRequested;
    private EventTrigger _closeButtonEventTrigger;
    private EventTrigger.Entry _closePointerDownEntry;
    private UnityAction<BaseEventData> _closePointerDownAction;

    [Header("Stage Button")]
    [SerializeField] private Button[] _stageButtons = new Button[4];
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private float _errorFadeDuration = 0.25f;
    [SerializeField] private float _errorDisplayDuration = 2f;

    private CanvasGroup _errorCanvasGroup;
    private Tween _errorFadeTween;

    [Header("Daily Stage UI")]
    [SerializeField] private TMP_Text _dailyChallengeCountText; // CountPanel 통합 텍스트 연결용
    [SerializeField] private TMP_Text _dailyRemainingCountText;
    [SerializeField] private TMP_Text _dailyCountSlashText;
    [SerializeField] private TMP_Text _dailyMaxCountText;
    [SerializeField] private Button _dailyStartButton;

    [Header("Weekly Stage UI")]
    [SerializeField] private TMP_Text _weeklyChallengeCountText; // CountPanel 통합 텍스트 연결용
    [SerializeField] private TMP_Text _weeklyRemainingCountText;
    [SerializeField] private TMP_Text _weeklyCountSlashText;
    [SerializeField] private TMP_Text _weeklyMaxCountText;
    [SerializeField] private Button _weeklyStartButton;

    public Action OnCloseClicked;
    public Action<int> OnStageSelected;
    public Action<StageType> OnStageStartClicked;

    private bool _isButtonEventRegistered;
    private readonly UnityAction[] _stageButtonActions = new UnityAction[4];
    private UnityAction _dailyStartAction;
    private UnityAction _weeklyStartAction;

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

    private void OnDestroy()
    {
        KillErrorPanelTween();
        KillCloseTween();
    }

    // 단계 버튼과 일일/주간 START 버튼 이벤트를 등록
    private void RegisterButtonEvents()
    {
        if (_isButtonEventRegistered)
            return;

        bool hasRegisteredButton = false;

        if (_closeButton != null)
        {
            RegisterCloseButtonEvent();
            hasRegisteredButton = true;
        }

        if (_stageButtons != null)
        {
            for (int i = 0; i < _stageButtons.Length; i++)
            {
                if (_stageButtons[i] == null)
                    continue;

                int stage = i + 1;
                _stageButtonActions[i] ??= () => SelectStage(stage);
                _stageButtons[i].onClick.AddListener(_stageButtonActions[i]);
                hasRegisteredButton = true;
            }
        }

        if (_dailyStartButton != null)
        {
            _dailyStartAction ??= () => StartSelectedStage(StageType.Daily);
            _dailyStartButton.onClick.AddListener(_dailyStartAction);
            hasRegisteredButton = true;
        }

        if (_weeklyStartButton != null)
        {
            _weeklyStartAction ??= () => StartSelectedStage(StageType.Weekly);
            _weeklyStartButton.onClick.AddListener(_weeklyStartAction);
            hasRegisteredButton = true;
        }

        _isButtonEventRegistered = hasRegisteredButton;
    }

    // 등록된 단계 버튼과 일일/주간 START 버튼 이벤트를 해제
    private void UnregisterButtonEvents()
    {
        UnregisterCloseButtonEvent();

        if (_stageButtons != null)
        {
            for (int i = 0; i < _stageButtons.Length; i++)
            {
                if (_stageButtons[i] == null || _stageButtonActions[i] == null)
                    continue;

                _stageButtons[i].onClick.RemoveListener(_stageButtonActions[i]);
            }
        }

        if (_dailyStartButton != null && _dailyStartAction != null)
            _dailyStartButton.onClick.RemoveListener(_dailyStartAction);

        if (_weeklyStartButton != null && _weeklyStartAction != null)
            _weeklyStartButton.onClick.RemoveListener(_weeklyStartAction);

        _isButtonEventRegistered = false;
    }

    // 선택 화면을 표시
    public void Show()
    {
        KillCloseTween();
        _isCloseAnimating = false;
        _isCloseRequested = false;
        gameObject.SetActive(true);
        EnsureCloseTweenTargets();
        UIPanelCloseTween.PrepareShow(_closeRoot, _closeCanvasGroup, 1f);
        SetCloseInputBlocked(false);
        SetCloseButtonInteractable(true);
    }

    // 선택 화면을 즉시 숨김
    public void Hide()
    {
        KillCloseTween();
        _isCloseAnimating = false;
        _isCloseRequested = false;
        SetCloseInputBlocked(false);
        SetCloseButtonInteractable(true);
        gameObject.SetActive(false);
    }

    // 선택 화면을 닫기 애니메이션 후 숨김
    public void Hide(Action onComplete)
    {
        if (!gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        if (_isCloseAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        EnsureCloseTweenTargets();
        KillCloseTween();
        _isCloseAnimating = true;
        SetCloseInputBlocked(true);

        if (_closeRoot == null && _closeCanvasGroup == null)
        {
            Hide();
            onComplete?.Invoke();
            return;
        }

        _closeSequence = UIPanelCloseTween.Play(
            _closeRoot,
            _closeCanvasGroup,
            _contentScaleFrom,
            _closeDuration,
            () =>
            {
                _isCloseAnimating = false;
                Hide();
                onComplete?.Invoke();
            });
    }

    // 선택 화면을 초기 상태로 되돌림
    // 단계 버튼만 활성화하고 일일/주간 START 버튼은 비활성화
    public void InitView()
    {
        SetStageButtonsInteractable(true);
        SetStageStartButtonsInteractable(false, false);
        HideErrorPanel();
        ClearStageCardInfo();
    }

    // 닫기 버튼 입력을 매니저에 전달
    private void RaiseCloseClicked()
    {
        if (_isCloseAnimating || _isCloseRequested)
            return;

        _isCloseRequested = true;
        SetCloseButtonInteractable(false);
        SetCloseInputBlocked(true);
        ClearSelectedButton();
        OnCloseClicked?.Invoke();
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    // 단계 버튼 입력을 매니저에 전달
    private void SelectStage(int stage)
    {
        ClearSelectedButton();
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        OnStageSelected?.Invoke(stage);
    }

    // 선택한 단계의 일일/주간 START 버튼 입력을 매니저에 전달
    private void StartSelectedStage(StageType stageType)
    {
        ClearSelectedButton();
        HideErrorPanel();
        OnStageStartClicked?.Invoke(stageType);
    }

    // 아직 입장할 수 없는 단계 안내 패널을 표시
    public void ShowErrorPanel()
    {
        BindErrorPanel();

        if (_errorPanel == null || _errorCanvasGroup == null)
            return;

        KillErrorPanelTween();
        _errorCanvasGroup.alpha = 0f;
        _errorPanel.SetActive(true);
        _errorPanel.transform.SetAsLastSibling();

        _errorFadeTween = _errorCanvasGroup
            .DOFade(1f, _errorFadeDuration)
            .SetUpdate(true)
            .OnComplete(ScheduleErrorPanelAutoHide);
    }

    private void ScheduleErrorPanelAutoHide()
    {
        if (_errorDisplayDuration <= 0f)
        {
            HideErrorPanel();
            return;
        }

        _errorFadeTween = DOVirtual
            .DelayedCall(_errorDisplayDuration, HideErrorPanel)
            .SetUpdate(true);
    }

    // 아직 입장할 수 없는 단계 안내 패널을 숨김
    public void HideErrorPanel()
    {
        BindErrorPanel();

        if (_errorPanel == null)
            return;

        if (!_errorPanel.activeSelf)
        {
            DeactivateErrorPanel();
            return;
        }

        if (_errorCanvasGroup == null)
        {
            DeactivateErrorPanel();
            return;
        }

        KillErrorPanelTween();
        _errorFadeTween = _errorCanvasGroup
            .DOFade(0f, _errorFadeDuration)
            .SetUpdate(true)
            .OnComplete(DeactivateErrorPanel);
    }

    // 버튼 클릭 후 Space 또는 Enter로 같은 버튼이 다시 눌리지 않도록 선택 상태를 해제
    private void ClearSelectedButton()
    {
        EventSystem.current?.SetSelectedGameObject(null);
    }

    // 선택한 단계 기준으로 일일/주간 카드의 남은 횟수와 보상 경험치를 출력
    public void PrintStageCardInfo(
        int dailyRemainingCount,
        int dailyMaxCount,
        bool canStartDaily,
        int weeklyRemainingCount,
        int weeklyMaxCount,
        bool canStartWeekly)
    {
        // CountPanel 통합 텍스트가 있으면 SO 값을 한 줄로 출력함
        if (_dailyChallengeCountText != null)
            SetText(_dailyChallengeCountText, FormatChallengeCount(dailyRemainingCount, dailyMaxCount));
        else
        {
            // 분리된 텍스트 UI 폴백용임
            SetText(_dailyRemainingCountText, dailyRemainingCount.ToString());
            SetText(_dailyCountSlashText, "/");
            SetText(_dailyMaxCountText, dailyMaxCount.ToString());
        }

        if (_weeklyChallengeCountText != null)
            SetText(_weeklyChallengeCountText, FormatChallengeCount(weeklyRemainingCount, weeklyMaxCount));
        else
        {
            // 분리된 텍스트 UI 폴백용임
            SetText(_weeklyRemainingCountText, weeklyRemainingCount.ToString());
            SetText(_weeklyCountSlashText, "/");
            SetText(_weeklyMaxCountText, weeklyMaxCount.ToString());
        }

        SetStageStartButtonsInteractable(canStartDaily, canStartWeekly);
    }

    // 일일/주간 카드의 출력 정보를 기본값으로 초기화
    public void ClearStageCardInfo()
    {
        if (_dailyChallengeCountText != null)
            SetText(_dailyChallengeCountText, FormatChallengeCount(-1, -1));
        else
        {
            SetText(_dailyRemainingCountText, "-");
            SetText(_dailyCountSlashText, "/");
            SetText(_dailyMaxCountText, "-");
        }

        if (_weeklyChallengeCountText != null)
            SetText(_weeklyChallengeCountText, FormatChallengeCount(-1, -1));
        else
        {
            SetText(_weeklyRemainingCountText, "-");
            SetText(_weeklyCountSlashText, "/");
            SetText(_weeklyMaxCountText, "-");
        }
    }

    // 단계 버튼 전체의 활성화 상태를 변경
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

    // 지정한 단계 버튼 하나의 활성화 상태를 변경
    public void SetStageButtonInteractable(int stage, bool value)
    {
        if (_stageButtons == null)
            return;

        int index = stage - 1;

        if (index < 0 || index >= _stageButtons.Length)
            return;

        if (_stageButtons[index] == null)
            return;

        _stageButtons[index].interactable = true;
    }

    // 일일/주간 START 버튼의 활성화 상태를 변경
    public void SetStageStartButtonsInteractable(bool canStartDaily, bool canStartWeekly)
    {
        if (_dailyStartButton != null)
            _dailyStartButton.interactable = canStartDaily;

        if (_weeklyStartButton != null)
            _weeklyStartButton.interactable = canStartWeekly;
    }

    // 선택 화면의 모든 버튼 활성화 상태를 변경
    public void SetAllButtonsInteractable(bool value)
    {
        SetStageButtonsInteractable(value);
        SetStageStartButtonsInteractable(value, value);
    }

    // 텍스트 컴포넌트가 연결되어 있을 때만 문자열을 출력
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
        EnsureCloseTweenTargets();

        if (isActiveAndEnabled)
            RegisterButtonEvents();
    }

    private void EnsureCloseTweenTargets()
    {
        _closeRoot ??= transform;
        _closeCanvasGroup ??= UIPanelCloseTween.GetOrAddCanvasGroup(gameObject);
    }

    private void SetCloseInputBlocked(bool blocked)
    {
        if (_closeCanvasGroup == null)
            return;

        _closeCanvasGroup.interactable = !blocked;
        _closeCanvasGroup.blocksRaycasts = !blocked;
    }

    private void SetCloseButtonInteractable(bool interactable)
    {
        if (_closeButton != null)
            _closeButton.interactable = interactable;
    }

    private void RegisterCloseButtonEvent()
    {
        _closePointerDownAction ??= _ => RaiseCloseClicked();

        _closeButtonEventTrigger = _closeButton.GetComponent<EventTrigger>();
        if (_closeButtonEventTrigger == null)
            _closeButtonEventTrigger = _closeButton.gameObject.AddComponent<EventTrigger>();

        if (_closePointerDownEntry != null)
            _closeButtonEventTrigger.triggers.Remove(_closePointerDownEntry);

        _closePointerDownEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        _closePointerDownEntry.callback.AddListener(_closePointerDownAction);
        _closeButtonEventTrigger.triggers.Add(_closePointerDownEntry);
    }

    private void UnregisterCloseButtonEvent()
    {
        if (_closeButtonEventTrigger != null && _closePointerDownEntry != null)
            _closeButtonEventTrigger.triggers.Remove(_closePointerDownEntry);

        if (_closePointerDownEntry != null)
            _closePointerDownEntry.callback.RemoveListener(_closePointerDownAction);

        _closePointerDownEntry = null;
    }

    private void KillCloseTween()
    {
        UIPanelCloseTween.Kill(_closeRoot, _closeCanvasGroup, _closeSequence);
        _closeSequence = null;
    }

    private void GetButtons()
    {
        _closeButton ??= FindButton("ExitButton") ?? FindButton("CloseButton");
        if (_closeButton != null)
            EnsureCloseButtonOnTop();

        _stageButtons[0] ??= FindButton("StageTab_1");
        _stageButtons[1] ??= FindButton("StageTab_2");
        _stageButtons[2] ??= FindButton("StageTab_3");
        _stageButtons[3] ??= FindButton("StageTab_4");
        _dailyStartButton ??= FindButtonInChild("DailyStageCard", "Button (1)");
        _weeklyStartButton ??= FindButtonInChild("WeekStageCard", "Button (1)") ?? FindButtonInChild("WeekStageCard", "Button");
        BindErrorPanel();
    }

    private void GetTexts()
    {
        // 카드별 CountPanel/Text (TMP)에 남은 횟수 텍스트 연결함
        _dailyChallengeCountText ??= FindChallengeCountText("DailyStageCard");
        _weeklyChallengeCountText ??= FindChallengeCountText("WeekStageCard");

        // enum 이름 바인딩은 분리 UI용 폴백임
        _dailyRemainingCountText ??= Get<TMP_Text>((int)StageTexts.DailyRemainChallengeCount);
        _dailyCountSlashText ??= Get<TMP_Text>((int)StageTexts.DailyChallengeCountSlash);
        _dailyMaxCountText ??= Get<TMP_Text>((int)StageTexts.DailyMaxChallengeCount);
        _weeklyRemainingCountText ??= Get<TMP_Text>((int)StageTexts.WeeklyRemainChallengeCount);
        _weeklyCountSlashText ??= Get<TMP_Text>((int)StageTexts.WeeklyChallengeCountSlash);
        _weeklyMaxCountText ??= Get<TMP_Text>((int)StageTexts.WeeklyMaxChallengeCount);
    }

    private static string FormatChallengeCount(int remainingCount, int maxCount)
    {
        // ScratchingTimeSO 도전 횟수 표시 포맷임
        if (remainingCount < 0 || maxCount < 0)
            return "남은 횟수 - / -";

        return $"남은 횟수 {remainingCount} / {maxCount}";
    }

    private TMP_Text FindChallengeCountText(string cardName)
    {
        // DailyStageCard, WeekStageCard 하위 CountPanel 텍스트 찾기임
        GameObject card = UIBase.FindChild(gameObject, cardName, true);
        if (card == null)
            return null;

        GameObject countPanel = UIBase.FindChild(card, "CountPanel", true);
        if (countPanel == null)
            return null;

        TMP_Text text = UIBase.FindChild<TMP_Text>(countPanel, "Text (TMP)", false);
        return text ?? countPanel.GetComponentInChildren<TMP_Text>(true);
    }

    private enum StageButtons
    {
        CloseButton,
        StageTab_1,
        StageTab_2,
        StageTab_3,
        StageTab_4,
        DailyStartButton,
        WeeklyStartButton
    }

    private Button FindButton(string childName)
    {
        return UIBase.FindChild<Button>(gameObject, childName, true);
    }

    private void EnsureCloseButtonOnTop()
    {
        Transform eventPanel = UIBase.FindChild<Transform>(gameObject, "EventPopupPanel", true);
        if (eventPanel == null || _closeButton == null)
            return;

        if (_closeButton.transform.parent != eventPanel)
            _closeButton.transform.SetParent(eventPanel, false);

        RectTransform rect = _closeButton.transform as RectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-19f, -88.82507f);
        rect.sizeDelta = new Vector2(100f, 100f);

        _closeButton.transform.SetAsLastSibling();
    }

    private void BindErrorPanel()
    {
        if (_errorPanel == null)
            _errorPanel = UIBase.FindChild(gameObject, "ErrorPanel", true);

        if (_errorPanel == null || _errorCanvasGroup != null)
            return;

        _errorCanvasGroup = _errorPanel.GetComponent<CanvasGroup>();

        if (_errorCanvasGroup == null)
            _errorCanvasGroup = _errorPanel.AddComponent<CanvasGroup>();

        _errorCanvasGroup.interactable = false;
        _errorCanvasGroup.blocksRaycasts = false;
    }

    private void KillErrorPanelTween()
    {
        _errorFadeTween?.Kill();
        _errorFadeTween = null;
        _errorCanvasGroup?.DOKill();
    }

    private void DeactivateErrorPanel()
    {
        if (_errorPanel == null)
            return;

        KillErrorPanelTween();

        if (_errorCanvasGroup != null)
            _errorCanvasGroup.alpha = 0f;

        _errorPanel.SetActive(false);
    }

    private Button FindButtonInChild(string childName, string buttonName = null)
    {
        GameObject child = UIBase.FindChild(gameObject, childName, true);

        if (child == null)
            return null;

        if (!string.IsNullOrEmpty(buttonName))
        {
            Button namedButton = UIBase.FindChild<Button>(child, buttonName, true);

            if (namedButton != null)
                return namedButton;
        }

        return child.GetComponentInChildren<Button>(true);
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
