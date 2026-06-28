using System;
using UI.NyangQuarium;
using UnityEngine;

/// <summary>
/// 수조 기본 화면의 무입력 시간을 감지하여 자동 물멍 모드를 관리합니다.
///
/// 기본 상태에서 5초간 입력이 없으면 모든 UI를 숨깁니다.
/// 물멍 상태에서는 첫 입력을 UI 복구에만 사용하고 물고기 선택을 차단합니다.
/// </summary>
public sealed class NyangQuariumWaterGazeController : MonoBehaviour
{
    [Header("물멍 진입 시간")]
    [Min(0.1f)]
    [SerializeField]
    private float _idleDuration = 5f;

    [Header("상태 참조")]
    [SerializeField]
    private NyangQuariumUIVisibilityToggle _uiVisibilityToggle;

    [SerializeField]
    private NyangQuariumFishDeleteUI _fishDeleteUI;

    [SerializeField]
    private NyangquariumPlacedFishRenderer _placedFishRenderer;

    [Header("물멍 진입을 막는 패널")]
    [Tooltip("인벤토리 등 열려 있는 동안 물멍 타이머를 멈출 UI 패널")]
    [SerializeField]
    private GameObject[] _blockingPanels;

    [Header("물멍에서 숨길 전체 UI")]
    [Tooltip("UI 토글 버튼을 포함하여 물멍 진입 시 숨길 UI")]
    [SerializeField]
    private GameObject[] _waterGazeUIObjects;

    private float _idleTimer;
    private bool _isWaterGazeMode;
    private bool[] _previousActiveStates;

    /// <summary>
    /// 현재 자동 물멍 모드 여부입니다.
    /// </summary>
    public bool IsWaterGazeMode => _isWaterGazeMode;

    /// <summary>
    /// 물멍 모드 상태가 변경될 때 호출됩니다.
    /// </summary>
    public event Action<bool> WaterGazeModeChanged;

    private void OnEnable()
    {
        ResetIdleTimer();
    }

    private void OnDisable()
    {
        ExitWaterGazeMode(false);
        ResetIdleTimer();
    }

    private void Update()
    {
        if (_isWaterGazeMode)
        {
            if (HasPointerDown())
                ExitWaterGazeMode(true);

            return;
        }

        if (HasAnyUserInput())
        {
            ResetIdleTimer();
            return;
        }

        if (!CanCountIdleTime())
        {
            ResetIdleTimer();
            return;
        }

        _idleTimer += Time.unscaledDeltaTime;

        if (_idleTimer >= _idleDuration)
            EnterWaterGazeMode();
    }

    /// <summary>
    /// 외부 시스템에서 입력 발생 시 타이머를 초기화할 때 사용합니다.
    /// </summary>
    public void ResetIdleTimer()
    {
        _idleTimer = 0f;
    }

    private bool CanCountIdleTime()
    {
        if (_uiVisibilityToggle != null &&
            _uiVisibilityToggle.IsManualHidden)
        {
            return false;
        }

        if (_fishDeleteUI != null &&
            (_fishDeleteUI.IsDeleteMode ||
             _fishDeleteUI.IsDeleteConfirmPopupOpened))
        {
            return false;
        }

        if (IsAnyBlockingPanelOpened())
            return false;

        return true;
    }

    private bool IsAnyBlockingPanelOpened()
    {
        if (_blockingPanels == null)
            return false;

        for (int i = 0; i < _blockingPanels.Length; i++)
        {
            GameObject panel = _blockingPanels[i];

            if (panel != null && panel.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void EnterWaterGazeMode()
    {
        if (_isWaterGazeMode || !CanCountIdleTime())
            return;

        _isWaterGazeMode = true;
        SaveAndHideUI();

        // 물멍 중 물고기를 터치해도 삭제 모드가 열리지 않도록 선택을 차단합니다.
        if (_placedFishRenderer != null)
            _placedFishRenderer.SetSelectionEnabled(false);

        WaterGazeModeChanged?.Invoke(true);

        Debug.Log(
            "[NyangQuariumWaterGazeController] 자동 물멍 모드 진입",
            this);
    }

    private void ExitWaterGazeMode(bool restoreSelection)
    {
        if (!_isWaterGazeMode)
            return;

        _isWaterGazeMode = false;
        RestoreUI();

        if (restoreSelection && _placedFishRenderer != null)
            _placedFishRenderer.SetSelectionEnabled(true);

        ResetIdleTimer();
        WaterGazeModeChanged?.Invoke(false);

        Debug.Log(
            "[NyangQuariumWaterGazeController] 자동 물멍 모드 종료",
            this);
    }

    private void SaveAndHideUI()
    {
        if (_waterGazeUIObjects == null)
            return;

        _previousActiveStates = new bool[_waterGazeUIObjects.Length];

        for (int i = 0; i < _waterGazeUIObjects.Length; i++)
        {
            GameObject target = _waterGazeUIObjects[i];

            if (target == null)
                continue;

            _previousActiveStates[i] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void RestoreUI()
    {
        if (_waterGazeUIObjects == null ||
            _previousActiveStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            _waterGazeUIObjects.Length,
            _previousActiveStates.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject target = _waterGazeUIObjects[i];

            if (target != null)
                target.SetActive(_previousActiveStates[i]);
        }

        _previousActiveStates = null;
    }

    private static bool HasPointerDown()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }
        }

        return Input.GetMouseButtonDown(0);
    }

    private static bool HasAnyUserInput()
    {
        if (Input.touchCount > 0)
            return true;

        return Input.GetMouseButtonDown(0) ||
               Input.GetMouseButton(0) ||
               Input.GetMouseButtonUp(0);
    }
}
