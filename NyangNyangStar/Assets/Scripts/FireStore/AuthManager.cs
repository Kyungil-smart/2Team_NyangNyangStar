using System;
using System.Collections;
using Core.Managers;
using Data.LibrarySystem;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UI.Login;
using UnityEngine;
using Util;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseAuth auth;
    private bool firebaseReady;

    public bool IsFirebaseReady => firebaseReady;
    public bool HasCurrentUser => auth != null && auth.CurrentUser != null;
    public bool IsSigningIn { get; private set; }
    public bool IsLoginFlowRunning { get; private set; }

    public float CurrentProgress { get; private set; }
    public string CurrentProgressMessage { get; private set; } = "로그인 대기 중";

    public event Action<float, string> OnLoginProgressChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameManager.Audio.PlayBgm("BGM_Main");
        InitializeFirebaseAuth();
    }

    private void InitializeFirebaseAuth()
    {
        ReportLoginProgress(0f, "Firebase 확인 중");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                firebaseReady = false;
                IsLoginFlowRunning = false;
                ReportLoginProgress(0f, $"Firebase 초기화 실패: {task.Result}");
                Debug.LogError($"Firebase dependencies error: {task.Result}");
                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            firebaseReady = true;

            Debug.Log("Firebase Authentication initialized successfully.");

            if (auth.CurrentUser != null)
            {
                Debug.Log($"Cached user exists: {auth.CurrentUser.UserId}");
                await LoginWithCachedUserAsync(auth.CurrentUser);
            }
            else
            {
                IsLoginFlowRunning = false;
                ReportLoginProgress(0f, "로그인 대기 중");
                Debug.Log("No cached user.");
            }
        });
    }

    public async void Login()
    {
        if (IsLoginFlowRunning)
            return;

        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            return;
        }

        if (FireStoreManager.Instance == null)
        {
            Debug.LogError("FireStoreManager.Instance가 없습니다. 씬에 FireStoreManager 오브젝트를 추가하세요.");
            return;
        }

        try
        {
            IsLoginFlowRunning = true;
            IsSigningIn = true;

            ReportLoginProgress(0.1f, "로그인 준비 중");
            ReportLoginProgress(0.25f, "익명 로그인 중");

            AuthResult result = await auth.SignInAnonymouslyAsync();
            FirebaseUser user = result.User;

            Debug.Log($"Anonymous login success. UID: {user.UserId}");

            IsSigningIn = false;
            ReportLoginProgress(0.4f, "Firestore 초기화 중");

            await FireStoreManager.Instance.InitAsync(user.UserId);

            Debug.Log("Firestore 초기화 완료");

            ReportLoginProgress(0.55f, "시트 데이터 로드 중");
            StartCoroutine(WaitForDataLoad());
        }
        catch (Exception e)
        {
            IsSigningIn = false;
            IsLoginFlowRunning = false;
            ReportLoginProgress(0f, "로그인 실패");
            Debug.LogError($"Anonymous login failed: {e}");
        }
    }

    private async System.Threading.Tasks.Task LoginWithCachedUserAsync(FirebaseUser user)
    {
        if (FireStoreManager.Instance == null)
        {
            Debug.LogError("FireStoreManager.Instance가 없습니다. 씬에 FireStoreManager 오브젝트를 추가하세요.");
            return;
        }

        try
        {
            IsLoginFlowRunning = true;
            IsSigningIn = false;

            ReportLoginProgress(0.1f, "기존 로그인 정보 확인 중");
            ReportLoginProgress(0.4f, "Firestore 초기화 중");

            await FireStoreManager.Instance.InitAsync(user.UserId);

            Debug.Log("Cached user Firestore 초기화 완료");

            ReportLoginProgress(0.55f, "시트 데이터 로드 중");
            StartCoroutine(WaitForDataLoad());
        }
        catch (Exception e)
        {
            IsLoginFlowRunning = false;
            ReportLoginProgress(0f, "자동 로그인 실패");
            Debug.LogError($"Cached user login failed: {e}");
        }
    }

    private IEnumerator WaitForDataLoad()
    {
        if (GameManager.Data != null)
        {
            GameManager.Data.OnDataLoadProgressChanged -= HandleDataLoadProgress;
            GameManager.Data.OnDataLoadProgressChanged += HandleDataLoadProgress;
        }

        GameManager.Data.LoadSheets();

        while (LocalDataAccess.Instance == null ||
               LocalDataAccess.Instance.Game == null ||
               !LocalDataAccess.Instance.Game.IsReady)
        {
            yield return null;
        }

        if (GameManager.Data != null)
            GameManager.Data.OnDataLoadProgressChanged -= HandleDataLoadProgress;

        ReportLoginProgress(1f, "로드 완료");

        LogInUIController login = FindObjectOfType<LogInUIController>();

        if (login != null)
            login.DownImage();
        else
            GameManager.Scene.LoadNextScene();

        IsSigningIn = false;
        IsLoginFlowRunning = false;
    }

    private void HandleDataLoadProgress(float sheetProgress, string message)
    {
        float loginProgress = Mathf.Lerp(0.55f, 0.95f, sheetProgress);
        ReportLoginProgress(loginProgress, message);
    }

    public void FreshAnonymousLogin()
    {
        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            return;
        }

        if (auth.CurrentUser != null)
        {
            Debug.Log($"Cached user found. Sign out first: {auth.CurrentUser.UserId}");
            auth.SignOut();
        }

        Login();
    }

    [ContextMenu("Logout")]
    public void Logout()
    {
        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            return;
        }

        if (auth.CurrentUser != null)
            Debug.Log($"Signing out user: {auth.CurrentUser.UserId}");

        auth.SignOut();

        IsSigningIn = false;
        IsLoginFlowRunning = false;
        ReportLoginProgress(0f, "로그인 대기 중");

        Debug.Log("Signed out.");
    }

    private void ReportLoginProgress(float progress, string message)
    {
        CurrentProgress = Mathf.Clamp01(progress);
        CurrentProgressMessage = string.IsNullOrEmpty(message) ? string.Empty : message;

        OnLoginProgressChanged?.Invoke(CurrentProgress, CurrentProgressMessage);
    }
}
