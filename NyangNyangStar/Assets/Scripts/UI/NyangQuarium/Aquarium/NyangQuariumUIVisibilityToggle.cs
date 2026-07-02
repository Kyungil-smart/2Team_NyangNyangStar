using System;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 수조 기본 UI의 표시/숨김 상태를 관리합니다.
/// On/Off 버튼을 각각 미리 준비하고 활성화 상태만 교체하여 즉시 반응합니다.
/// </summary>
public sealed class NyangQuariumUIVisibilityToggle : MonoBehaviour
{
    [Header("UI 토글 버튼")]
    [Tooltip("기본 UI가 보일 때 표시되는 On 버튼")]
    [SerializeField] private Button _toggleOnButton;

    [Tooltip("기본 UI가 숨겨졌을 때 표시되는 Off 버튼")]
    [SerializeField] private Button _toggleOffButton;

    [Header("숨길 기본 UI")]
    [Tooltip("토글 버튼을 제외하고 숨길 UI 오브젝트")]
    [SerializeField] private GameObject[] _targetUIObjects;

    private bool _isUIVisible = true;
    private bool[] _previousActiveStates;

    public bool IsUIVisible => _isUIVisible;
    public bool IsManualHidden => !_isUIVisible;

    public event Action<bool> VisibilityChanged;

    private void Awake()
    {
        BindButtons();
        ShowUIWithoutSound();
    }

    private void OnDisable()
    {
        ResetUIStateWithoutNotification();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (_toggleOnButton == null || _toggleOffButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumUIVisibilityToggle] UI On/Off 버튼 연결을 확인해주세요.",
                DebugType.UI,
                this);
        }

        if (_toggleOnButton != null)
        {
            _toggleOnButton.onClick.RemoveListener(HideUIByButton);
            _toggleOnButton.onClick.AddListener(HideUIByButton);
        }

        if (_toggleOffButton != null)
        {
            _toggleOffButton.onClick.RemoveListener(ShowUIByButton);
            _toggleOffButton.onClick.AddListener(ShowUIByButton);
        }
    }

    private void UnbindButtons()
    {
        if (_toggleOnButton != null)
            _toggleOnButton.onClick.RemoveListener(HideUIByButton);

        if (_toggleOffButton != null)
            _toggleOffButton.onClick.RemoveListener(ShowUIByButton);
    }

    private void HideUIByButton()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        SetUIVisible(false);
    }

    private void ShowUIByButton()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        SetUIVisible(true);
    }

    /// <summary>
    /// 기존 외부 호출 호환용 토글 함수입니다.
    /// </summary>
    public void ToggleUI()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        SetUIVisible(!_isUIVisible);
    }

    public void SetUIVisible(bool isVisible)
    {
        if (_isUIVisible == isVisible)
            return;

        // 버튼 이미지는 이미 로드되어 있으므로 활성화 상태를 먼저 즉시 교체합니다.
        SetToggleButtonsActive(isVisible);

        if (isVisible)
            RestoreUI();
        else
            HideTargetUI();

        _isUIVisible = isVisible;
        VisibilityChanged?.Invoke(_isUIVisible);

        DebugTool.Log(
            $"[NyangQuariumUIVisibilityToggle] 기본 UI 표시 상태: {_isUIVisible}",
            DebugType.UI,
            this);
    }

    private void HideTargetUI()
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

    private void SetToggleButtonsActive(bool isUIVisible)
    {
        if (_toggleOnButton != null)
            _toggleOnButton.gameObject.SetActive(isUIVisible);

        if (_toggleOffButton != null)
            _toggleOffButton.gameObject.SetActive(!isUIVisible);
    }

    private void ShowUIWithoutSound()
    {
        RestoreUI();
        _isUIVisible = true;
        SetToggleButtonsActive(true);
        VisibilityChanged?.Invoke(true);
    }

    private void ResetUIStateWithoutNotification()
    {
        RestoreUI();
        _isUIVisible = true;
        SetToggleButtonsActive(true);
    }
}
