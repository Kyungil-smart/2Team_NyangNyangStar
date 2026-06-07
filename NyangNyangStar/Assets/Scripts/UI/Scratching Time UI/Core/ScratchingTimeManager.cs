using Core.Managers;
using Data.ScriptableObjects.ScratchingTimeSO;
using DG.Tweening;
using Services.Enums;
using UI.Base;
using UnityEngine;

// 스크래칭 타임의 선택 상태, 화면 전환, 전투,결과 팝업 흐름을 관리
// partial로 Data / BattleFlow / ViewFlow / Events 역할 분리
public partial class ScratchingTimeManager : UIBase
{
    [Header("Data")]
    [SerializeField] private ScratchingSo _scratching;              // 스테이지 밸런스,진행 상태 SO
    [SerializeField] private ScratchingProgressSO _scratchingProgress; // 서버 저장용 진행도 SO
    [Tooltip("스크래칭 밸런스 시트 URL. SheetLoader.prefab scratchingURL과 동일하게 유지")]
    [SerializeField]
    private string _scratchingSheetFallbackUrl =
        "https://docs.google.com/spreadsheets/d/1hD2NCEzCWr2aml-_aqHsHZu_8HeZ0xCLpMDt_A9NfoU/edit?gid=0#gid=0";

    [Header("Controller")]
    [SerializeField] private ScratchingTimeController _selectionController;   // 단계 선택 화면
    [SerializeField] private ScratchingBattleController _battleController;     // 전투 UI (폴백)
    [SerializeField] private ScratchingBattleController _dailyStageController;  // 일일 스테이지 전투 UI
    [SerializeField] private ScratchingBattleController _weeklyStageController; // 주간 스테이지 전투 UI
    [SerializeField] private ResultPopupController _resultPopupController;     // 클리어/실패 결과 팝업
    [SerializeField] private MoongchiStatController _moongchiStatController;   // 뭉치 스탯,공격 처리

    [Header("Current Stage State")]
    [SerializeField] private int _selectedStage;                              // 현재 선택된 단계 (1~4)
    [SerializeField] private StageType _selectedStageType = StageType.None;   // 일일/주간 중 진행 중인 타입
    [SerializeField] private bool _isStarted;                                 // 스테이지 전투 진행 중 여부

    [Header("Canvas")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _rectTransform;

    private bool HasSelectedStage => _selectedStage > 0;
    private bool _isUiTransitioning;       // 화면 전환 애니메이션 중 중복 입력 방지
    private bool _isInitialized;             // Init() 1회 실행 여부
    private bool _isWaitingForDataReady;     // LocalDataAccess.OnReady 구독 중 여부
    private Coroutine _scratchingDataLoadCoroutine; // 시트 직접 로드 코루틴

    private const int ScratchingSheetHeaderRowCount = 1; // 시트 헤더 행 수 (파싱 시 스킵)

    // 선택된 스테이지 타입에 맞는 전투 컨트롤러 반환
    private ScratchingBattleController CurrentBattleController =>
        _selectedStageType == StageType.Weekly
            ? _weeklyStageController ?? _battleController
            : _dailyStageController ?? _battleController;


    // SO,시트 데이터 로드 후 Init 호출
    private void Awake()
    {
        EnsureRequiredDataLoad();
        Init();
    }


    // 스크래칭 타임 UI 이벤트를 등록
    private void OnEnable()
    {
        if (_selectionController != null)
        {
            _selectionController.OnCloseClicked += CloseEventView;
            _selectionController.OnCloseClicked += CloseScratchingTimeUI;
            _selectionController.OnStageSelected += SelectStage;
            _selectionController.OnStageStartClicked += StartStage;
        }

        RegisterBattleCloseEvent(_battleController);
        RegisterBattleCloseEvent(_dailyStageController);
        RegisterBattleCloseEvent(_weeklyStageController);
        RegisterScratchAttackEvent(_battleController);
        RegisterScratchAttackEvent(_dailyStageController);
        RegisterScratchAttackEvent(_weeklyStageController);
        RegisterInterestDepletedEvent(_battleController);
        RegisterInterestDepletedEvent(_dailyStageController);
        RegisterInterestDepletedEvent(_weeklyStageController);

        if (_resultPopupController != null)
        {
            _resultPopupController.OnBackClicked += BackToSelection;
            _resultPopupController.OnRetryClicked += RetryStage;
        }
    }


    // 등록한 UI 이벤트 해제
    private void OnDisable()
    {
        if (_selectionController != null)
        {
            _selectionController.OnCloseClicked -= CloseEventView;
            _selectionController.OnStageSelected -= SelectStage;
            _selectionController.OnStageStartClicked -= StartStage;
        }

        UnregisterBattleCloseEvent(_battleController);
        UnregisterBattleCloseEvent(_dailyStageController);
        UnregisterBattleCloseEvent(_weeklyStageController);
        UnregisterScratchAttackEvent(_battleController);
        UnregisterScratchAttackEvent(_dailyStageController);
        UnregisterScratchAttackEvent(_weeklyStageController);
        UnregisterInterestDepletedEvent(_battleController);
        UnregisterInterestDepletedEvent(_dailyStageController);
        UnregisterInterestDepletedEvent(_weeklyStageController);

        if (_resultPopupController != null)
        {
            _resultPopupController.OnBackClicked -= BackToSelection;
            _resultPopupController.OnRetryClicked -= RetryStage;
        }
    }


    // 데이터 구독 해제 및 로드 코루틴 정리
    private void OnDestroy()
    {
        UnsubscribeDataReady();
        StopScratchingDataLoad();
    }


    // 스크래칭 타임 UI와 뭉치 정보를 초기 상태로 출력
    private void Start()
    {
        ResetAllState();
        _moongchiStatController?.PrintMoongchiStat();
    }


    // 자식 오브젝트에서 컨트롤러를 찾아 Init (없으면 AddComponent)
    public override void Init()
    {
        if (_isInitialized)
            return;

        // ScratchingTimeUI Addressables 스프라이트 적용
        GetOrAddController<ScratchingTimeUISprite>("ScratchingTimeUI")?.Init();

        _selectionController ??= GetOrAddController<ScratchingTimeController>("ScratchingTimeUI");
        _dailyStageController ??= GetOrAddController<ScratchingBattleController>("ScratchingDailyStageUI");
        _weeklyStageController ??= GetOrAddController<ScratchingBattleController>("ScratchingWeekStageUI");
        _battleController ??= _dailyStageController;
        _resultPopupController ??= GetOrAddController<ResultPopupController>("ResultPopup");
        _moongchiStatController ??= GetOrAddController<MoongchiStatController>("ScratchingTimeUI");

        _selectionController?.Init();
        _dailyStageController?.Init();
        _weeklyStageController?.Init();
        _resultPopupController?.Init();
        _moongchiStatController?.Init();
        
        _canvas = GetComponent<Canvas>();
        _rectTransform = GetComponent<RectTransform>();

        _isInitialized = true;
    }

    public void OpenScratchingTimeUI()
    {
        _canvas.sortingOrder = 4;
        if (_rectTransform == null) return;
            _rectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.InOutCubic);
    }

    public void CloseScratchingTimeUI()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        if (_rectTransform == null) return;
            _rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.OutCirc);
        _canvas.sortingOrder = 1;
    }


    // 자식 Transform에서 컴포넌트를 찾고, 없으면 새로 추가
    private T GetOrAddController<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            DebugTool.Warning($"{childName} 오브젝트를 찾을 수 없습니다.", DebugType.ScratchingTime);
            return null;
        }

        T controller = child.GetComponent<T>();

        if (controller == null)
            controller = child.gameObject.AddComponent<T>();

        return controller;
    }
}
