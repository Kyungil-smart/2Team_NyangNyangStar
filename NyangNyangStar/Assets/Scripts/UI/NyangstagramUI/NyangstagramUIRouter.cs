using System;

public static class NyangstagramUIRouter
{
    public static event Action<string> OnRequestOpenPopup;
    public static event Action OnRequestCloseAll;
    public static event Action OnRequestHomeView;
    public static event Action OnRequestProfileView;

    public static void RequestOpenPopup(string key)
    {
        OnRequestOpenPopup?.Invoke(key);
    }

    public static void RequestCloseAll()
    {
        OnRequestCloseAll?.Invoke();
    }

    public static void RequestHomeView()
    {
        OnRequestHomeView?.Invoke();
    }

    public static void RequestProfileView()
    {
        OnRequestProfileView?.Invoke();
    }
}