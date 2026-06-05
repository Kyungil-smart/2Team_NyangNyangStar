using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TestEventPanelPopupButton : UIPopup
{
    private const string ScratchingTimeScreenKey = "ScratchingTimeScreen";
    private static GameObject _scratchingTimeScreen;

    public void OpenScratcherSeasonEventPanel()
    {
        if (_scratchingTimeScreen != null)
        {
            _scratchingTimeScreen.SetActive(true);

            ScratchingTimeManager loadedManager = _scratchingTimeScreen.GetComponent<ScratchingTimeManager>();
            loadedManager?.OpenEventView();
            return;
        }

        Addressables.InstantiateAsync(ScratchingTimeScreenKey).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                DebugTool.Log($"{ScratchingTimeScreenKey} load failed.", DebugType.Addressable);
                return;
            }

            _scratchingTimeScreen = handle.Result;
            SetPopupParent(_scratchingTimeScreen);

            ScratchingTimeManager manager = _scratchingTimeScreen.GetComponent<ScratchingTimeManager>();

            if (manager == null)
                manager = _scratchingTimeScreen.AddComponent<ScratchingTimeManager>();

            manager.Init();
            manager.OpenEventView();
            DebugTool.Log($"{ScratchingTimeScreenKey} opened.", DebugType.Addressable);
        };
    }

    private static void SetPopupParent(GameObject popupObject)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        popupObject.transform.SetParent(canvas.transform, false);
    }

    public override void Init()
    {
    }
}
