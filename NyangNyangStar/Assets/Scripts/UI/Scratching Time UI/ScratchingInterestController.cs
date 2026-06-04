using System;
using UnityEngine;
using UnityEngine.UI;


// 스크래칭 타임 전투의 흥미도 게이지를 관리
public class ScratchingInterestController : MonoBehaviour
{
    private const float MaxInterest = 100f;
    private const float DefaultDrainDuration = 33f;
    private const float FinalPhaseDuration = 3f;

    [SerializeField] private Slider _interestSlider;

    private float _drainDuration = DefaultDrainDuration;

    private float _interest = MaxInterest;
    private float _elapsedDrainTime;
    private bool _isDraining;

    public float CurrentInterest => _interest;
    public bool IsDraining => _isDraining;
    public float RemainingTime => _isDraining
        ? Mathf.Max(0f, _drainDuration - _elapsedDrainTime)
        : _drainDuration;

    private float MainPhaseDuration => Mathf.Max(0f, _drainDuration - FinalPhaseDuration);
    private float ActualFinalPhaseDuration => _drainDuration - MainPhaseDuration;

    // ScratchingSo.TimeLimit — 스테이지 시작 시 매니저에서 호출
    public void SetDrainDuration(float duration)
    {
        _drainDuration = Mathf.Max(0.01f, duration);
    }

    // 흥미도가 0이 되었을 때 발생 -> 스테이지 실패
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

        if (_elapsedDrainTime < _drainDuration)
            return;

        _interest = 0f;
        UpdateSlider();
        _isDraining = false;
        OnDepleted?.Invoke();
    }

    // 1구간 : (TimeLimit -> 3초) 동안 100 -> 0 선형
    // 2구간 : 게이지 100% 리셋 후 3초 동안 100 -> 0 선형 (총 TimeLimit 유지)

    private float CalculateInterest(float elapsed)
    {
        if (elapsed <= 0f)
            return MaxInterest;

        if (elapsed >= _drainDuration)
            return 0f;

        float mainDuration = MainPhaseDuration;
        if (mainDuration > 0f && elapsed < mainDuration)
            return MaxInterest * (1f - elapsed / mainDuration);

        float finalElapsed = elapsed - mainDuration;
        float finalDuration = ActualFinalPhaseDuration;
        if (finalDuration <= 0f)
            return 0f;

        return MaxInterest * (1f - finalElapsed / finalDuration);
    }

    private void UpdateSlider()
    {
        if (_interestSlider == null)
            return;

        _interestSlider.value = _interest / MaxInterest;
    }
}
