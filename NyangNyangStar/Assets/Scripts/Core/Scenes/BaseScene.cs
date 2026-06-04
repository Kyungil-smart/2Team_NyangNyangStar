using Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using Util;

public class BaseScene : MonoBehaviour
{
    void Awake() => Init();

    protected virtual void Init()
    {
        Object obj = FindFirstObjectByType(typeof(EventSystem));

        if (obj == null)
        {
            GameManager.Addressable.LoadPrefab(KeyContainer.Prefabs.EventSystem,
                onloaded => { },
                onFailed => { }
            );
        }
    }
}