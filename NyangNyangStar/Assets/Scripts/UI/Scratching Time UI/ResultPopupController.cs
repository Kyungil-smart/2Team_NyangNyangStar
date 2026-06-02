using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 스크래칭 타임의 클리어/실패 결과 팝업 UI를 관리합니다.
/// 이 컴포넌트가 붙은 GameObject 자체를 켜고 끄는 방식으로 표시 상태를 제어합니다.
/// </summary>
public class ResultPopupController : UIBase
{
    [Header("Result Text")] [SerializeField]
    private TMP_Text _resultTitleText;

    [SerializeField] private GameObject _clearResultObject;
    [SerializeField] private GameObject _failResultObject;

    [Header("Result Button")] [SerializeField]
    private Button _backButton;

    [SerializeField] private Button _retryButton;

    public Action OnBackClicked;
    public Action OnRetryClicked;

    /// <summary>
    /// 결과 팝업 버튼 이벤트를 등록합니다.
    /// </summary>
    private void OnEnable()
    {
        RegisterButtonEvents();
    }

    /// <summary>
    /// 결과 팝업 버튼 이벤트를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        UnregisterButtonEvents();
    }

    /// <summary>
    /// 돌아가기와 다시하기 버튼 이벤트를 등록합니다.
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(RaiseBackClicked);

        if (_retryButton != null)
            _retryButton.onClick.AddListener(RaiseRetryClicked);
    }

    /// <summary>
    /// 돌아가기와 다시하기 버튼 이벤트를 해제합니다.
    /// </summary>
    private void UnregisterButtonEvents()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(RaiseBackClicked);

        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(RaiseRetryClicked);
    }

    /// <summary>
    /// 결과 팝업의 돌아가기 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    private void RaiseBackClicked()
    {
        ClearSelectedButton();
        OnBackClicked?.Invoke();
    }

    /// <summary>
    /// 결과 팝업의 다시하기 버튼 입력을 매니저에 전달합니다.
    /// </summary>
    private void RaiseRetryClicked()
    {
        ClearSelectedButton();
        OnRetryClicked?.Invoke();
    }

    /// <summary>
    /// 버튼 클릭 후 Space 또는 Enter로 같은 버튼이 다시 눌리지 않도록 선택 상태를 해제합니다.
    /// </summary>
    private void ClearSelectedButton()
    {
        EventSystem.current?.SetSelectedGameObject(null);
    }

    /// <summary>
    /// 공용 결과 팝업을 표시합니다.
    /// </summary>
    /// <param name="isClear">클리어 결과이면 true, 실패 결과이면 false</param>
    /// <param name="canRetry">다시하기 버튼 활성화 여부</param>
    public void ShowResultPopup(bool isClear, bool canRetry)
    {
        gameObject.SetActive(true);

        if (_resultTitleText != null)
            _resultTitleText.text = isClear ? "클리어!" : "실패";

        if (_clearResultObject != null)
            _clearResultObject.SetActive(isClear);

        if (_failResultObject != null)
            _failResultObject.SetActive(!isClear);

        SetRetryButtonInteractable(canRetry);
    }

    /// <summary>
    /// 결과 팝업을 숨깁니다.
    /// </summary>
    public void HidePopup()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 다시하기 버튼 활성화 상태를 변경합니다.
    /// </summary>
    /// <param name="value">활성화 여부</param>
    public void SetRetryButtonInteractable(bool value)
    {
        if (_retryButton == null)
            return;

        _retryButton.interactable = value;
    }

    public override void Init()
    {
        _resultTitleText ??= GetComponentInChildren<TMP_Text>(true);
        _clearResultObject ??= UIBase.FindChild(gameObject, "Clear", true);
        _failResultObject ??= UIBase.FindChild(gameObject, "Fail", true);

        Button[] buttons = GetComponentsInChildren<Button>(true);

        if (_backButton == null && buttons.Length > 0)
            _backButton = buttons[0];

        if (_retryButton == null && buttons.Length > 1)
            _retryButton = buttons[1];
    }
}