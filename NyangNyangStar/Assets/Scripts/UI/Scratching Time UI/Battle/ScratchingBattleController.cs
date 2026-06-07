using Core.Managers;
using System;
using DG.Tweening;
using Services.Enums;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


// 스크래칭 타임 전투 화면 UI를 관리
// 흥미도와 스크래쳐 내구도는 Filled 타입 Image의 fillAmount로 표시

public class ScratchingBattleController : UIBase
{
    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("Close Animation")]
    [Tooltip("비우면 전투 UI 루트 전체가 닫힙니다.")]
    [SerializeField] private Transform _closeRoot;
    [Tooltip("닫힐 때 도달하는 스케일. 0.5~0.7로 바꿔 보면 차이가 확실합니다.")]
    [SerializeField] private float _contentScaleFrom = 0.82f;
    [SerializeField] private float _closeDuration = 0.22f;

    private CanvasGroup _closeCanvasGroup;
    private Sequence _closeSequence;
    private bool _isCloseAnimating;

    [Header("스테이지/단계 표시 텍스트")]
    [SerializeField] private TMP_Text _battleStageText;

    [Header("전투 게이지 이미지")]
    [Tooltip("스크래쳐 내구도 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField] private Image _durabilityAmountImage;
    [Tooltip("흥미도 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField] private Image _interestAmountImage;
    [SerializeField] private Slider _durabilitySlider;
    [SerializeField] private Slider _interestSlider;
    [SerializeField] private ScratchEffectPool _scratchEffectPool;
    [SerializeField] private ScratchingInterestController _interestController;

    public Action OnCloseClicked;
    public ScratchEffectPool ScratchEffectPool => _scratchEffectPool;
    public ScratchingInterestController InterestController => _interestController;

    public void StopInterestDrain()
    {
        _interestController?.StopInterestDrain();
    }

    public void ConfigureInterestDrain(float timeLimit)
    {
        _interestController?.SetDrainDuration(timeLimit);
    }


    // 전투 화면 버튼 이벤트를 등록
    private void OnEnable()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(RaiseCloseClicked);
    }


    // 전투 화면 버튼 이벤트를 해제
    private void OnDisable()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(RaiseCloseClicked);
    }

    private void OnDestroy()
    {
        KillCloseTween();
    }


    // 닫기 버튼 입력을 매니저에 전달
    private void RaiseCloseClicked()
    {
        if (_isCloseAnimating)
            return;

        EventSystem.current?.SetSelectedGameObject(null);
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        OnCloseClicked?.Invoke();
    }


    // 전투 화면을 표시
    public void Show()
    {
        KillCloseTween();
        _isCloseAnimating = false;
        gameObject.SetActive(true);
        EnsureCloseTweenTargets();
        UIPanelCloseTween.PrepareShow(_closeRoot, _closeCanvasGroup, 1f);
        SetCloseInputBlocked(false);
    }


    // 전투 화면을 즉시 숨기기
    public void Hide()
    {
        KillCloseTween();
        _isCloseAnimating = false;
        SetCloseInputBlocked(false);
        gameObject.SetActive(false);
    }
    // 전투 화면을 닫기 애니메이션 후 숨김. onComplete: 닫기 완료 후 콜백
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
    /// <summary>
    /// 전투 화면의 스테이지 제목을 출력합니다.
    /// </summary>
    /// <param name="stage">현재 스테이지 단계</param>
    /// <param name="stageType">현재 스테이지 타입</param>
    public void PrintBattleStageTitle(int stage, StageType stageType)
    {
        if (_battleStageText == null)
            return;

        _battleStageText.text = $"{GetStageTypeText(stageType)} 스테이지 ({stage}단계)";
    }
    /// <summary>
    /// 흥미도 게이지 이미지를 현재/최대 비율로 표시합니다.
    /// 흥미도 감소 로직은 포함하지 않습니다.
    /// </summary>
    /// <param name="current">현재 흥미도 값</param>
    /// <param name="max">최대 흥미도 값</param>
    public void SetInterestAmount(float current, float max)
    {
        float ratio = GetSafeRatio(current, max);

        if (_interestSlider != null)
            _interestSlider.value = ratio;

        if (_interestAmountImage != null)
            _interestAmountImage.fillAmount = ratio;
    }
    /// <summary>
    /// 내구도 게이지 이미지를 현재/최대 비율로 표시합니다.
    /// </summary>
    /// <param name="current">현재 내구도</param>
    /// <param name="max">최대 내구도</param>
    public void SetDurabilityAmount(int current, int max)
    {
        float ratio = GetSafeRatio(current, max);

        if (_durabilitySlider != null)
            _durabilitySlider.value = ratio;

        if (_durabilityAmountImage != null)
            _durabilityAmountImage.fillAmount = ratio;
    }
    /// <summary>
    /// 현재/최대 값으로 UI 게이지 비율을 계산합니다.
    /// </summary>
    /// <param name="current">현재 값</param>
    /// <param name="max">최대 값</param>
    /// <returns>0에서 1 사이로 제한된 비율 값</returns>
    private float GetSafeRatio(float current, float max)
    {
        if (max <= 0)
            return 0f;

        return Mathf.Clamp01(current / max);
    }
    /// <summary>
    /// 스테이지 타입을 화면 출력용 문자열로 변환합니다.
    /// </summary>
    /// <param name="stageType">변환할 스테이지 타입</param>
    /// <returns>화면 출력용 스테이지 타입 문자열</returns>
    private string GetStageTypeText(StageType stageType)
    {
        return stageType switch
        {
            StageType.Daily => "일일",
            StageType.Weekly => "주간",
            _ => "미선택"
        };
    }

    public override void Init()
    {
        _closeButton ??= UIBase.FindChild<Button>(gameObject, "ExitButton", true);
        _closeButton ??= GetComponentInChildren<Button>(true);
        _battleStageText ??= GetComponentInChildren<TMP_Text>(true);
        _durabilitySlider ??= FindSlider("DurabilitySlider");
        _interestSlider ??= FindSlider("InterestSlider");
        _durabilityAmountImage ??= FindFillImage("DurabilitySlider");
        _interestAmountImage ??= FindFillImage("InterestSlider");
        _scratchEffectPool ??= GetComponentInChildren<ScratchEffectPool>(true);
        _interestController ??= GetComponentInChildren<ScratchingInterestController>(true);
        EnsureCloseTweenTargets();
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

    private void KillCloseTween()
    {
        UIPanelCloseTween.Kill(_closeRoot, _closeCanvasGroup, _closeSequence);
        _closeSequence = null;
    }

    private Slider FindSlider(string sliderName)
    {
        Transform slider = UIBase.FindChild<Transform>(gameObject, sliderName, true);
        return slider != null ? slider.GetComponent<Slider>() : null;
    }

    private Image FindFillImage(string sliderName)
    {
        Transform slider = UIBase.FindChild<Transform>(gameObject, sliderName, true);
        Transform fill = slider != null ? UIBase.FindChild<Transform>(slider.gameObject, "Fill", true) : null;

        return fill != null ? fill.GetComponent<Image>() : null;
    }
}
