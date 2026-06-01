using System;
using UnityEngine;
using UnityEngine.UI;


// 스크래칭 타임 전투의 흥미도 게이지를 관리


public class ScratchingInterestController : MonoBehaviour
{
    private const float MaxInterest = 100f;

    // 100 -> 1 구간: 30초
    private const float MainPhaseDrainDuration = 30f;

    // 1 -> 0 구간 : 3초 (막판 긴장감용)
    private const float FinalPhaseDrainDuration = 3f;
    private const float TotalDrainDuration = MainPhaseDrainDuration + FinalPhaseDrainDuration;

    [SerializeField] private Slider _interestSlider;

    private float _interest = MaxInterest;
    private float _elapsedDrainTime;
    private bool _isDraining;

    public float CurrentInterest => _interest;
    public bool IsDraining => _isDraining;
    public float RemainingTime => _isDraining
        ? Mathf.Max(0f, TotalDrainDuration - _elapsedDrainTime)
        : TotalDrainDuration;

    // 흥미도가 0이 되었을 때 발생 (스테이지 실패 처리용)
    public event Action OnDepleted;

    private void OnEnable()
    {
        ResetInterest();
    }

    // 스테이지 입장 시 흥미도를 100으로 초기화하고 감소를 멈춤
    public void ResetInterest()
    {
        _interest = MaxInterest;
        _elapsedDrainTime = 0f;
        _isDraining = false;
        UpdateSlider();
    }

    // 첫 터치 시 호출 TOUCH! 안내가 사라진 직후 흥미도 감소를 시작
    public void StartInterestDrain()
    {
        if (_isDraining || _interest <= 0f)
            return;

        _isDraining = true;
        _elapsedDrainTime = 0f;
    }

    public void StopInterestDrain()
    {
        _isDraining = false;
    }

    private void Update()
    {
        if (!_isDraining)
            return;

        _elapsedDrainTime += Time.deltaTime;
        _interest = CalculateInterest(_elapsedDrainTime);
        UpdateSlider();

        if (_elapsedDrainTime < TotalDrainDuration)
            return;

        _interest = 0f;
        UpdateSlider();
        _isDraining = false;
        OnDepleted?.Invoke();
    }

    
    // 경과 시간 기준으로 흥미도를 계산
    // 1구간(30초) : 100 -> 1, 2구간(3초) : 1 -> 0

    private static float CalculateInterest(float elapsed)
    {
        if (elapsed <= 0f)
            return MaxInterest;

        if (elapsed <= MainPhaseDrainDuration)
            return MaxInterest - (MaxInterest - 1f) * (elapsed / MainPhaseDrainDuration);

        float finalElapsed = elapsed - MainPhaseDrainDuration;
        if (finalElapsed >= FinalPhaseDrainDuration)
            return 0f;

        return 1f - finalElapsed / FinalPhaseDrainDuration;
    }

    private void UpdateSlider()
    {
        if (_interestSlider == null)
            return;

        _interestSlider.value = _interest / MaxInterest;
    }
}
