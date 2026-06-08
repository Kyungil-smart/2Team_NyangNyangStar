using System.Collections;
using Core.Managers;
using TMPro;
using UI.Transition;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Login
{
    public class LogInUIController : MonoBehaviour
    {
        [Header("Logo")]
        [SerializeField] private Image _logoImage;

        [Header("Buttons")]
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _logoutButton;

        [Header("Loading")]
        [SerializeField] private GameObject _loadingRoot;
        [SerializeField] private RectTransform _loadingFillRect;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField] private TMP_Text _statusText;

        private Coroutine _refreshCoroutine;
        private bool _subscribed;

        private void Awake()
        {
            if (_logoImage != null)
                _logoImage.SetNativeSize();

            BindButtons();
            SetLoginButtonVisible(false);
            SetLoadingVisible(false);
            SetLoadingProgress(0f);
        }

        private void OnEnable()
        {
            BindButtons();

            if (_refreshCoroutine != null)
                StopCoroutine(_refreshCoroutine);

            _refreshCoroutine = StartCoroutine(RefreshLoginUiWhenAuthReady());
        }

        private void Start()
        {
            if (ScreenTransitionManager.Instance != null)
                ScreenTransitionManager.Instance.Reveal();
        }

        private void OnDisable()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }

            UnsubscribeAuthProgress();
        }

        private void BindButtons()
        {
            if (_loginButton == null)
                _loginButton = FindButton("LogInButton");

            if (_logoutButton == null)
                _logoutButton = FindButton("LogOutButton");

            if (_loginButton != null)
            {
                _loginButton.onClick.RemoveAllListeners();
                _loginButton.onClick.AddListener(OnClickLoginButton);
            }

            if (_logoutButton != null)
            {
                _logoutButton.onClick.RemoveAllListeners();
                _logoutButton.onClick.AddListener(() =>
                {
                    if (AuthManager.Instance != null)
                        AuthManager.Instance.LogoutAndClearSession();
                    else
                        GameManager.ClearSession();
                });
            }
        }

        private IEnumerator RefreshLoginUiWhenAuthReady()
        {
            SetLoginButtonVisible(false);
            SetLoadingVisible(false);
            SetLoadingProgress(0f);

            while (AuthManager.Instance == null || !AuthManager.Instance.IsFirebaseReady)
                yield return null;

            SubscribeAuthProgress();

            HandleLoginProgress(
                AuthManager.Instance.CurrentProgress,
                AuthManager.Instance.CurrentProgressMessage);

            RefreshLoginButtonVisible();
        }

        private void OnClickLoginButton()
        {
            SetLoginButtonVisible(false);
            SetLoadingVisible(true);

            SetLoadingProgress(0.05f);

            if (_statusText != null)
                _statusText.text = "로그인 요청 중";

            if (AuthManager.Instance != null)
                AuthManager.Instance.Login();
        }

        private void RefreshLoginButtonVisible()
        {
            if (AuthManager.Instance == null)
            {
                SetLoginButtonVisible(false);
                return;
            }

            bool shouldShowLoginButton =
                !AuthManager.Instance.HasCurrentUser &&
                !AuthManager.Instance.IsLoginFlowRunning;

            SetLoginButtonVisible(shouldShowLoginButton);
            SetLoadingVisible(AuthManager.Instance.IsLoginFlowRunning);
        }

        private void SubscribeAuthProgress()
        {
            if (_subscribed || AuthManager.Instance == null)
                return;

            AuthManager.Instance.OnLoginProgressChanged += HandleLoginProgress;
            _subscribed = true;
        }

        private void UnsubscribeAuthProgress()
        {
            if (!_subscribed || AuthManager.Instance == null)
                return;

            AuthManager.Instance.OnLoginProgressChanged -= HandleLoginProgress;
            _subscribed = false;
        }

        private void HandleLoginProgress(float progress, string message)
        {
            bool isLoading = AuthManager.Instance != null &&
                             AuthManager.Instance.IsLoginFlowRunning;

            SetLoadingVisible(isLoading);
            SetLoadingProgress(progress);

            if (_statusText != null)
                _statusText.text = message;

            RefreshLoginButtonVisible();
        }

        private void SetLoadingProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            if (_loadingFillRect != null)
            {
                Vector3 scale = _loadingFillRect.localScale;
                scale.x = progress;
                _loadingFillRect.localScale = scale;
            }

            if (_percentText != null)
                _percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        private void SetLoginButtonVisible(bool isVisible)
        {
            if (_loginButton != null)
                _loginButton.gameObject.SetActive(isVisible);
        }

        private void SetLoadingVisible(bool isVisible)
        {
            if (_loadingRoot != null)
                _loadingRoot.SetActive(isVisible);
        }

        private Button FindButton(string buttonName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        public void DownImage()
        {
            if (ScreenTransitionManager.Instance == null)
            {
                GameManager.Scene.LoadNextScene();
                return;
            }

            ScreenTransitionManager.Instance.Cover(() =>
            {
                GameManager.Scene.LoadNextScene();
            });
        }
    }
}