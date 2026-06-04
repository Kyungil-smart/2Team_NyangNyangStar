using System;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


// 스크래칭 타임의 클리어/실패 결과 팝업 UI를 관리
// 이 컴포넌트가 붙은 GameObject 자체를 켜고 끄는 방식으로 표시 상태를 제어

public class ResultPopupController : UIBase
{
    [Header("Result Text")]
    [SerializeField] private TMP_Text _resultTitleText;

    [SerializeField] private GameObject _clearResultObject;
    [SerializeField] private GameObject _failResultObject;

    [Header("Result Button")]
    [SerializeField] private Button _backButton;

    [SerializeField] private Button _retryButton;

    [Header("DOTween Targets")]
    [SerializeField] private Transform _contentPanel;
    [SerializeField] private Transform _buttonArea;
    [SerializeField] private GameObject _dimBackgroundObject;

    [Header("DOTween Timing")]
    [SerializeField] private float _dimTargetAlpha = 0.65f;
    [SerializeField] private float _dimFadeDuration = 0.2f;
    [SerializeField] private float _contentScaleFrom = 0.85f;
    [SerializeField] private float _contentScaleDuration = 0.25f;
    [SerializeField] private float _buttonAreaDelay = 0.1f;
    [SerializeField] private float _buttonStaggerDelay = 0.08f;
    [SerializeField] private float _buttonAnimDuration = 0.18f;
    [SerializeField] private float _buttonSlideOffsetY = -24f;
    [SerializeField] private float _closeDuration = 0.15f;
    [SerializeField] private float _clearPunchStrength = 0.08f;

    public Action OnBackClicked;
    public Action OnRetryClicked;

    private CanvasGroup _dimCanvasGroup;
    private CanvasGroup _backButtonCanvasGroup;
    private CanvasGroup _retryButtonCanvasGroup;
    private RectTransform _backButtonRect;
    private RectTransform _retryButtonRect;
    private Vector2 _backButtonOriginPos;
    private Vector2 _retryButtonOriginPos;
    private bool _buttonOriginCached;
    private Sequence _popupSequence;
    private bool _buttonsReady;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        RegisterButtonEvents();
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
        KillPopupTween();
    }

    private void OnDestroy()
    {
        KillPopupTween();
    }

    private void RegisterButtonEvents()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(RaiseBackClicked);

        if (_retryButton != null)
            _retryButton.onClick.AddListener(RaiseRetryClicked);
    }

    private void UnregisterButtonEvents()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(RaiseBackClicked);

        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(RaiseRetryClicked);
    }

    private void RaiseBackClicked()
    {
        if (!_buttonsReady)
            return;

        ClearSelectedButton();
        OnBackClicked?.Invoke();
    }

    private void RaiseRetryClicked()
    {
        if (!_buttonsReady)
            return;

        ClearSelectedButton();
        OnRetryClicked?.Invoke();
    }

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
        EnsureReferences();
        BindTweenTargets();
        KillPopupTween();

        gameObject.SetActive(true);
        _buttonsReady = false;

        if (_resultTitleText != null)
            _resultTitleText.text = isClear ? "클리어!" : "실패";

        if (_clearResultObject != null)
            _clearResultObject.SetActive(isClear);

        if (_failResultObject != null)
            _failResultObject.SetActive(!isClear);

        SetRetryButtonInteractable(canRetry);
        SetButtonsInteractable(false, false);
        PrepareOpenState();

        if (_contentPanel == null || _dimCanvasGroup == null)
        {
            SetButtonsInteractable(true, canRetry);
            _buttonsReady = true;
            return;
        }

        Ease contentEase = isClear ? Ease.OutBack : Ease.OutSine;

        _popupSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_dimCanvasGroup.DOFade(_dimTargetAlpha, _dimFadeDuration))
            .Join(_contentPanel.DOScale(1f, _contentScaleDuration)
                .From(_contentScaleFrom, true)
                .SetEase(contentEase))
            .AppendInterval(_buttonAreaDelay)
            .Append(CreateButtonInTween(_backButton, _backButtonCanvasGroup, _backButtonRect))
            .AppendInterval(_buttonStaggerDelay)
            .Append(CreateButtonInTween(_retryButton, _retryButtonCanvasGroup, _retryButtonRect))
            .OnComplete(() =>
            {
                SetButtonsInteractable(true, canRetry);
                _buttonsReady = true;
            });

        if (isClear && TryGetClearResultTransform(out Transform clearTransform))
        {
            _popupSequence.Insert(
                _dimFadeDuration + _contentScaleDuration * 0.65f,
                clearTransform
                    .DOPunchScale(Vector3.one * _clearPunchStrength, 0.35f, 5, 0.4f)
                    .SetUpdate(true));
        }
    }

   
    // 결과 팝업을 숨깁니다. 닫기 애니메이션 후 비활성화합니다.
    public void HidePopup(Action onComplete = null)
    {
        if (!gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        EnsureReferences();
        BindTweenTargets();
        KillPopupTween();
        _buttonsReady = false;
        SetButtonsInteractable(false, false);

        if (_contentPanel == null || _dimCanvasGroup == null)
        {
            DeactivatePopup(onComplete);
            return;
        }

        _popupSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_contentPanel.DOScale(_contentScaleFrom, _closeDuration).SetEase(Ease.InSine))
            .Join(_dimCanvasGroup.DOFade(0f, _closeDuration))
            .OnComplete(() => DeactivatePopup(onComplete));
    }

    
    // 다시하기 버튼 활성화 상태를 변경합니다.
    public void SetRetryButtonInteractable(bool value)
    {
        if (_retryButton == null)
            return;

        _retryButton.interactable = value;
    }

    public override void Init()
    {
        _resultTitleText ??= GetComponentInChildren<TMP_Text>(true);
        EnsureReferences();
        BindTweenTargets();
        BindButtons();
    }

    
    // 인스펙터 미연결 참조를 하위 오브젝트에서 찾아 바인딩합니다.
    private void EnsureReferences()
    {
        GameObject topArea = UIBase.FindChild(gameObject, "TopArea", true);

        if (_clearResultObject == null)
        {
            _clearResultObject = topArea != null
                ? UIBase.FindChild(topArea, "Clear", false)
                : null;
            _clearResultObject ??= UIBase.FindChild(gameObject, "Clear", true);
        }

        if (_failResultObject == null)
        {
            _failResultObject = topArea != null
                ? UIBase.FindChild(topArea, "Fail", false)
                : null;
            _failResultObject ??= UIBase.FindChild(gameObject, "Fail", true);
        }

        if (_contentPanel == null)
        {
            GameObject content = topArea ?? UIBase.FindChild(gameObject, "TopArea", true);
            if (content != null)
                _contentPanel = content.transform;
        }

        if (_buttonArea == null)
        {
            GameObject bottomArea = UIBase.FindChild(gameObject, "BottemArea", true);
            if (bottomArea != null)
                _buttonArea = bottomArea.transform;
        }

        if (_dimBackgroundObject == null)
            _dimBackgroundObject = UIBase.FindChild(gameObject, "PopupBackground", true);
    }

    private bool TryGetClearResultTransform(out Transform clearTransform)
    {
        clearTransform = null;

        if (_clearResultObject == null)
            return false;

        clearTransform = _clearResultObject.transform;
        return clearTransform != null;
    }

    private void BindButtons()
    {
        GameObject bottomArea = UIBase.FindChild(gameObject, "BottemArea", true);

        if (bottomArea != null)
        {
            Button[] bottomButtons = bottomArea.GetComponentsInChildren<Button>(true);

            if (_backButton == null && bottomButtons.Length > 0)
                _backButton = bottomButtons[0];

            if (_retryButton == null && bottomButtons.Length > 1)
                _retryButton = bottomButtons[1];
        }

        if (_backButton == null || _retryButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            if (_backButton == null && buttons.Length > 0)
                _backButton = buttons[0];

            if (_retryButton == null && buttons.Length > 1)
                _retryButton = buttons[1];
        }
    }

    private void BindTweenTargets()
    {
        EnsureReferences();

        if (_dimBackgroundObject != null)
        {
            _dimCanvasGroup = _dimBackgroundObject.GetComponent<CanvasGroup>();

            if (_dimCanvasGroup == null)
                _dimCanvasGroup = _dimBackgroundObject.AddComponent<CanvasGroup>();
        }

        BindButtonTweenTargets(_backButton, ref _backButtonCanvasGroup, ref _backButtonRect, ref _backButtonOriginPos);
        BindButtonTweenTargets(_retryButton, ref _retryButtonCanvasGroup, ref _retryButtonRect, ref _retryButtonOriginPos);
    }

    private static void BindButtonTweenTargets(
        Button button,
        ref CanvasGroup canvasGroup,
        ref RectTransform rect,
        ref Vector2 originPos)
    {
        if (button == null)
            return;

        rect = button.transform as RectTransform;

        canvasGroup = button.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

        if (rect != null)
            originPos = rect.anchoredPosition;
    }

    private void CacheButtonOriginPositions()
    {
        if (_buttonOriginCached)
            return;

        if (_backButtonRect != null)
            _backButtonOriginPos = _backButtonRect.anchoredPosition;

        if (_retryButtonRect != null)
            _retryButtonOriginPos = _retryButtonRect.anchoredPosition;

        _buttonOriginCached = true;
    }

    private void PrepareOpenState()
    {
        CacheButtonOriginPositions();

        if (_dimCanvasGroup != null)
            _dimCanvasGroup.alpha = 0f;

        if (_contentPanel != null)
            _contentPanel.localScale = Vector3.one * _contentScaleFrom;

        PrepareButtonHiddenState(_backButtonRect, _backButtonCanvasGroup, _backButtonOriginPos);
        PrepareButtonHiddenState(_retryButtonRect, _retryButtonCanvasGroup, _retryButtonOriginPos);
    }

    private void PrepareButtonHiddenState(RectTransform rect, CanvasGroup canvasGroup, Vector2 originPos)
    {
        if (rect == null || canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        rect.anchoredPosition = originPos + new Vector2(0f, _buttonSlideOffsetY);
    }

    private Tween CreateButtonInTween(Button button, CanvasGroup canvasGroup, RectTransform rect)
    {
        if (button == null || canvasGroup == null || rect == null)
            return DOVirtual.DelayedCall(0f, null);

        Vector2 originPos = rect == _backButtonRect ? _backButtonOriginPos : _retryButtonOriginPos;

        return DOTween.Sequence()
            .Append(canvasGroup.DOFade(1f, _buttonAnimDuration))
            .Join(rect.DOAnchorPos(originPos, _buttonAnimDuration).SetEase(Ease.OutSine));
    }

    private void SetButtonsInteractable(bool backEnabled, bool retryEnabled)
    {
        if (_backButton != null)
            _backButton.interactable = backEnabled;

        if (_retryButton != null)
            _retryButton.interactable = retryEnabled;
    }

    private void DeactivatePopup(Action onComplete)
    {
        KillPopupTween();
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void KillPopupTween()
    {
        if (_popupSequence != null)
        {
            _popupSequence.Kill();
            _popupSequence = null;
        }

        SafeKillTween(_dimCanvasGroup);
        SafeKillTween(_contentPanel);
        SafeKillTween(_backButtonCanvasGroup);
        SafeKillTween(_retryButtonCanvasGroup);
        SafeKillTween(_backButtonRect);
        SafeKillTween(_retryButtonRect);

        if (TryGetClearResultTransform(out Transform clearTransform))
            SafeKillTween(clearTransform);
    }

    private static void SafeKillTween(Component target)
    {
        if (target == null)
            return;

        target.DOKill();
    }
}
