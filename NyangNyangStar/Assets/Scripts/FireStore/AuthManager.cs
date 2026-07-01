using System;
using System.Collections;
using System.Threading.Tasks;
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

    [Header("냥냥스냅 사진 로드")]
    [SerializeField] private NyangNyangSnapPhotoAlbumSO _photoAlbumSO;
    [SerializeField] private NyangNyangSnapRuntimePhotoSO _runtimePhotoSO;

    private FirebaseAuth auth;
    private bool firebaseReady;
    private int _loginFlowVersion;

    public bool IsFirebaseReady => firebaseReady;
    public bool HasCurrentUser => auth != null && auth.CurrentUser != null;
    public bool IsSigningIn { get; private set; }
    public bool IsLoginFlowRunning { get; private set; }
    public string CurrentUserId { get; private set; } = string.Empty;

    public float CurrentProgress { get; private set; }
    public string CurrentProgressMessage { get; private set; } = "로그인 대기 중";

    public event Action<float, string> OnLoginProgressChanged;
    public event Action<string> OnUserIdChanged;

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
            SetCurrentUserId(auth.CurrentUser != null ? auth.CurrentUser.UserId : string.Empty);

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

        int flowVersion = BeginLoginFlow();

        try
        {
            IsSigningIn = true;

            ReportLoginProgress(0.1f, "로그인 준비 중");
            ReportLoginProgress(0.25f, "익명 로그인 중");

            AuthResult result = await auth.SignInAnonymouslyAsync();

            if (!IsValidLoginFlow(flowVersion))
                return;

            FirebaseUser user = result.User;
            SetCurrentUserId(user.UserId);

            Debug.Log($"Anonymous login success. UID: {user.UserId}");

            IsSigningIn = false;
            ReportLoginProgress(0.4f, "Firestore 초기화 중");

            await FireStoreManager.Instance.InitAsync(user.UserId);

            if (!IsValidLoginFlow(flowVersion))
                return;

            Debug.Log("Firestore 초기화 완료");

            ReportLoginProgress(0.55f, "시트 데이터 로드 중");
            StartCoroutine(WaitForDataLoad(flowVersion));
        }
        catch (Exception e)
        {
            if (!IsValidLoginFlow(flowVersion))
                return;

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

        int flowVersion = BeginLoginFlow();

        try
        {
            IsSigningIn = false;
            SetCurrentUserId(user.UserId);

            ReportLoginProgress(0.1f, "기존 로그인 정보 확인 중");
            ReportLoginProgress(0.4f, "Firestore 초기화 중");

            await FireStoreManager.Instance.InitAsync(user.UserId);

            if (!IsValidLoginFlow(flowVersion))
                return;

            Debug.Log("Cached user Firestore 초기화 완료");

            ReportLoginProgress(0.55f, "시트 데이터 로드 중");
            StartCoroutine(WaitForDataLoad(flowVersion));
        }
        catch (Exception e)
        {
            if (!IsValidLoginFlow(flowVersion))
                return;

            IsLoginFlowRunning = false;
            ReportLoginProgress(0f, "자동 로그인 실패");
            Debug.LogError($"Cached user login failed: {e}");
        }
    }

    private IEnumerator WaitForDataLoad(int flowVersion)
    {
        if (!IsValidLoginFlow(flowVersion))
            yield break;

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
            if (!IsValidLoginFlow(flowVersion))
            {
                if (GameManager.Data != null)
                    GameManager.Data.OnDataLoadProgressChanged -= HandleDataLoadProgress;

                yield break;
            }

            yield return null;
        }

        if (!IsValidLoginFlow(flowVersion))
            yield break;

        if (GameManager.Data != null)
            GameManager.Data.OnDataLoadProgressChanged -= HandleDataLoadProgress;

        StartCoroutine(LoadPhotoData(flowVersion));
    }

    private IEnumerator LoadPhotoData(int flowVersion)
    {
        if (!IsValidLoginFlow(flowVersion))
            yield break;

        ReportLoginProgress(0.95f, "사진 데이터 로드 중");

        yield return LoadPhotosCoroutine();

        if (!IsValidLoginFlow(flowVersion))
            yield break;

        ReportLoginProgress(1f, "로드 완료");

        LogInUIController login = FindObjectOfType<LogInUIController>();

        if (login != null)
            login.DownImage();
        else
            GameManager.Scene.LoadNextScene();

        IsSigningIn = false;
        IsLoginFlowRunning = false;
    }

    private IEnumerator LoadPhotosCoroutine()
    {
        Task task = LoadPhotosAsync();

        while (!task.IsCompleted)
            yield return null;

        if (task.Exception != null)
        {
            DebugTool.Warning($"사진 로드 실패", DebugType.Network, this);
        }
    }

    private async Task LoadPhotosAsync()
    {
        _runtimePhotoSO.ClearPhotos();

        await _photoAlbumSO.UpdateFromServerAsync(false);

        foreach (NyangNyangSnapSavedPhotoData photo in _photoAlbumSO.Photos)
        {
            if (string.IsNullOrEmpty(photo.storagePath)) continue;

            Sprite sprite = await FirebaseStorageHelper.LoadUserSpriteAsync(photo.storagePath);

            if (sprite == null) continue;

            _runtimePhotoSO.AddPhoto(new NyangNyangSnapRuntimePhotoData(
                    photo.photoId,
                    sprite,
                    photo.storagePath,
                    photo.starCount,
                    photo.createdAt));
        }

        DebugTool.Log(
            $"사진 로드 완료: {_runtimePhotoSO.RuntimePhotos.Count}장",
            DebugType.Network,
            this);
    }

    private void HandleDataLoadProgress(float sheetProgress, string message)
    {
        float loginProgress = Mathf.Lerp(0.55f, 0.85f, sheetProgress);
        ReportLoginProgress(loginProgress, message);
    }

    public void FreshAnonymousLogin()
    {
        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            return;
        }

        Logout();
        Login();
    }

    [ContextMenu("Logout")]
    public void Logout()
    {
        CancelLoginFlow();

        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            SetCurrentUserId(string.Empty);
            ReportLoginProgress(0f, "로그인 대기 중");
            return;
        }

        if (auth.CurrentUser != null)
            Debug.Log($"Signing out user: {auth.CurrentUser.UserId}");

        auth.SignOut();
        SetCurrentUserId(string.Empty);
        ReportLoginProgress(0f, "로그인 대기 중");

        Debug.Log("Signed out.");
    }

    public void LogoutAndClearSession()
    {
        Logout();
        GameManager.ClearSession();
    }

    private int BeginLoginFlow()
    {
        _loginFlowVersion++;
        IsLoginFlowRunning = true;
        IsSigningIn = false;
        return _loginFlowVersion;
    }

    private void CancelLoginFlow()
    {
        _loginFlowVersion++;
        StopAllCoroutines();
        IsSigningIn = false;
        IsLoginFlowRunning = false;

        if (GameManager.Data != null)
            GameManager.Data.OnDataLoadProgressChanged -= HandleDataLoadProgress;
    }

    private bool IsValidLoginFlow(int flowVersion)
    {
        return IsLoginFlowRunning && flowVersion == _loginFlowVersion;
    }

    private void SetCurrentUserId(string userId)
    {
        userId ??= string.Empty;

        if (CurrentUserId == userId)
            return;

        CurrentUserId = userId;
        PlayerResourceManager.Instance.ResetForUserChange(CurrentUserId);
        OnUserIdChanged?.Invoke(CurrentUserId);
    }

    private void ReportLoginProgress(float progress, string message)
    {
        CurrentProgress = Mathf.Clamp01(progress);
        CurrentProgressMessage = string.IsNullOrEmpty(message) ? string.Empty : message;

        OnLoginProgressChanged?.Invoke(CurrentProgress, CurrentProgressMessage);
    }
}
