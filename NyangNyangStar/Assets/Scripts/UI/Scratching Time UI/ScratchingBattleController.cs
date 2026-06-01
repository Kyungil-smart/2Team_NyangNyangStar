using System;
using Services.Enums;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스크래칭 타임 전투 화면 UI를 관리합니다.
/// 흥미도와 스크래쳐 내구도는 Filled 타입 Image의 fillAmount로 표시합니다.
/// </summary>
public class ScratchingBattleController : UIBase
{
    [Header("닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("스테이지/단계 표시 텍스트")]
    [SerializeField] private TMP_Text _battleStageText;

    [Header("전투 게이지 이미지")]
    [Tooltip("스크래쳐 내구도 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField] private Image _durabilityAmountImage;
    [Tooltip("흥미도 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField] private Image _interestAmountImage;

    public Action OnCloseClicked;

    /// <summary>
    /// 전투 화면 버튼 이벤트를 등록합니다.
    /// </summary>
    private void OnEnable()
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(RaiseCloseClicked);
    }

    /// <summary>
    /// 전투 화면 버튼 이벤트를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(RaiseCloseClicked);
    }

    /// <summary>
    /// 닫기 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    private void RaiseCloseClicked()
    {
        EventSystem.current?.SetSelectedGameObject(null);
        OnCloseClicked?.Invoke();
    }

    /// <summary>
    /// 전투 화면을 표시합니다.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 전투 화면을 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
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
        if (_interestAmountImage == null)
            return;

        _interestAmountImage.fillAmount = GetSafeRatio(current, max);
    }

    /// <summary>
    /// 내구도 게이지 이미지를 현재/최대 비율로 표시합니다.
    /// </summary>
    /// <param name="current">현재 내구도</param>
    /// <param name="max">최대 내구도</param>
    public void SetDurabilityAmount(int current, int max)
    {
        if (_durabilityAmountImage == null)
            return;

        _durabilityAmountImage.fillAmount = GetSafeRatio(current, max);
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
    }
}
