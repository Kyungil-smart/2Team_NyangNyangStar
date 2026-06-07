using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using Core.Managers;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private bool firebaseReady = false;
    private GameObject logInButton;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        
    }   
    private void Start()
    {
        logInButton = GameObject.Find("LogInButton");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;

                Debug.Log("Firebase Authentication initialized successfully.");

                if (auth.CurrentUser != null)
                {
                    Debug.Log($"Cached user exists: {auth.CurrentUser.UserId}");

                    await FireStoreManager.Instance.InitAsync(auth.CurrentUser.UserId);

                    Debug.Log("Cached user Firestore 초기화 완료");

                    Login();
                    
                }
                else
                {
                    Debug.Log("No cached user.");
                }
            }
            else
            {
                Debug.LogError($"Firebase dependencies error: {task.Result}");
            }
        });


    }

    public async void Login()
    {
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
            logInButton.SetActive(false);
            AuthResult result = await auth.SignInAnonymouslyAsync();
            FirebaseUser user = result.User;

            Debug.Log($"Anonymous login success. UID: {user.UserId}");

            await FireStoreManager.Instance.InitAsync(user.UserId);
            SceneManager.LoadScene("Scenes/Game Scene");

            Debug.Log("Firestore 초기화까지 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Anonymous login failed: {e}");
        }
        GameManager.Data.LoadSheets();
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
        {
            Debug.Log($"Signing out user: {auth.CurrentUser.UserId}");
        }

        auth.SignOut();

        Debug.Log("Signed out.");
    }


}