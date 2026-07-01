using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수조 기본 UI의 표시/숨김 상태를 관리합니다.
///
/// UI 숨김 상태에서도 토글 버튼은 계속 표시되며,
/// 배치된 물고기 선택과 삭제 기능은 그대로 사용할 수 있습니다.
/// </summary>
public sealed class NyangQuariumUIVisibilityToggle : MonoBehaviour
{
    [Header("UI 토글 버튼")]
    [Tooltip("기본 UI 표시 상태를 전환하는 버튼")]
    [SerializeField]
    private Button _toggleButton;

    [Header("숨길 기본 UI")]
    [Tooltip("토글 버튼을 제외하고 숨길 UI 오브젝트")]
    [SerializeField]
    private GameObject[] _targetUIObjects;

    private bool _isUIVisible = true;
    private bool[] _previousActiveStates;

    /// <summary>
    /// 현재 기본 UI 표시 여부입니다.
    /// </summary>
    public bool IsUIVisible => _isUIVisible;

    /// <summary>
    /// UI 토글 버튼으로 기본 UI를 수동 숨김한 상태입니다.
    /// 자동 물멍 타이머는 이 상태에서 정지해야 합니다.
    /// </summary>
    public bool IsManualHidden => !_isUIVisible;

    /// <summary>
    /// 기본 UI 표시 상태가 바뀌었을 때 호출됩니다.
    /// true는 표시, false는 숨김입니다.
    /// </summary>
    public event Action<bool> VisibilityChanged;

    private void Awake()
    {
        BindButton();
        ShowUIWithoutSound();
    }

    private void OnDisable()
    {
        // 종료/비활성화 중에는 구독 중인 UI가 먼저 파괴될 수 있으므로
        // VisibilityChanged 이벤트를 발생시키지 않고 상태만 초기화합니다.
        ResetUIStateWithoutNotification();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    private void BindButton()
    {
        if (_toggleButton == null)
        {
            DebugTool.Warning("[NyangQuariumUIVisibilityToggle] UI 토글 버튼이 연결되지 않았습니다.",
                DebugType.UI,
                this);

            return;
        }

        _toggleButton.onClick.RemoveListener(ToggleUI);
        _toggleButton.onClick.AddListener(ToggleUI);
    }

    private void UnbindButton()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.RemoveListener(ToggleUI);
    }

    /// <summary>
    /// 현재 상태의 반대로 기본 UI를 전환합니다.
    /// </summary>
    public void ToggleUI()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        SetUIVisible(!_isUIVisible);

        DebugTool.Log($"[NyangQuariumUIVisibilityToggle] 기본 UI 표시 상태: {_isUIVisible}",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 기본 UI 표시 상태를 직접 설정합니다.
    /// </summary>
    public void SetUIVisible(bool isVisible)
    {
        if (_isUIVisible == isVisible)
            return;

        if (isVisible)
            RestoreUI();
        else
            HideUI();

        _isUIVisible = isVisible;
        VisibilityChanged?.Invoke(_isUIVisible);
    }

    private void HideUI()
    {
        if (_targetUIObjects == null)
            return;

        _previousActiveStates = new bool[_targetUIObjects.Length];

        for (int i = 0; i < _targetUIObjects.Length; i++)
        {
            GameObject target = _targetUIObjects[i];

            if (target == null)
                continue;

            _previousActiveStates[i] = target.activeSelf;
            target.SetActive(false);
        }
    }

    private void RestoreUI()
    {
        if (_targetUIObjects == null)
            return;

        for (int i = 0; i < _targetUIObjects.Length; i++)
        {
            GameObject target = _targetUIObjects[i];

            if (target == null)
                continue;

            bool shouldBeActive =
                _previousActiveStates == null ||
                i >= _previousActiveStates.Length ||
                _previousActiveStates[i];

            target.SetActive(shouldBeActive);
        }

        _previousActiveStates = null;
    }

    private void ShowUIWithoutSound()
    {
        RestoreUI();

        _isUIVisible = true;
        VisibilityChanged?.Invoke(true);
    }

    /// <summary>
    /// 오브젝트 비활성화 또는 게임 종료 시 UI 상태만 초기화합니다.
    /// 파괴 순서에 따른 NullReferenceException을 막기 위해 이벤트는 호출하지 않습니다.
    /// </summary>
    private void ResetUIStateWithoutNotification()
    {
        RestoreUI();
        _isUIVisible = true;
    }
}