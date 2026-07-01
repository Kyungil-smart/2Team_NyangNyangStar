using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하나의 Toggle과 관상어 필터 종류를 연결합니다.
/// </summary>
[RequireComponent(typeof(Toggle))]
public sealed class NyangQuariumFishFilterToggle : MonoBehaviour
{
    [Header("필터 설정")]
    [SerializeField] private NyangQuariumFishFilterType _filterType;

    private Toggle _toggle;

    public NyangQuariumFishFilterType FilterType => _filterType;
    public bool IsOn => _toggle != null && _toggle.isOn;

    public event Action<NyangQuariumFishFilterToggle, bool> ValueChanged;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (_toggle == null)
            _toggle = GetComponent<Toggle>();

        _toggle.onValueChanged.RemoveListener(HandleValueChanged);
        _toggle.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(HandleValueChanged);
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        if (_toggle == null)
            _toggle = GetComponent<Toggle>();

        _toggle.SetIsOnWithoutNotify(isOn);
    }

    private void HandleValueChanged(bool isOn)
    {
        ValueChanged?.Invoke(this, isOn);

        DebugTool.Log($"[NyangQuariumFishFilterToggle] Filter:{_filterType}, IsOn:{isOn}",
            DebugType.UI,
            this);
    }
}

/// <summary>
/// 관상어 배치 패널에서 사용하는 필터 종류입니다.
/// </summary>
public enum NyangQuariumFishFilterType
{
    All,
    Freshwater,
    Saltwater,
    BrackishWater
}
