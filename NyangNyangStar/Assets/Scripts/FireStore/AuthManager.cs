using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private bool firebaseReady = false;

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;

                Debug.Log("Firebase Authentication initialized successfully.");

                if (auth.CurrentUser != null)
                {
                    Debug.Log($"Cached user exists: {auth.CurrentUser.UserId}");
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

    public void Login()
    {
        if (!firebaseReady || auth == null)
        {
            Debug.LogWarning("Firebase is not ready yet.");
            return;
        }

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync error: " + task.Exception);
                return;
            }

            AuthResult result = task.Result;
            FirebaseUser user = result.User;

            Debug.Log($"Anonymous login success. UID: {user.UserId}");

            FireStoreManager.Instance.Init(user.UserId);

        });
    }

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


}