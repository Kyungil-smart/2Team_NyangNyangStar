using Services.AddressableKey;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class AddressableManager : ISubManager
    {
        private GameObject _root;
        public GameObject Root => _root;
        
        public void Init()
        {
            KeyContainer.InitKeyDict();
            
            _root = GameObject.Find("@Addressable");
            
            if (_root == null)
            {
                _root = new GameObject { name = "@Addressable" };
                Object.DontDestroyOnLoad(_root);
            }

            DebugTool.Log("어드레서블 매니저 초기화 완료", DebugType.Game);
        }

        public bool TryLoadPrefab(string key, Action<GameObject> onLoaded,  Action<string> onFailed = null,  bool dontDestroy = false)
        {
            if (!KeyContainer.GetAddressableKey(key))
            {
                onFailed?.Invoke(null);
                return false;
            }
            
            Addressables.InstantiateAsync(key, _root.transform).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = handle.Result;
                    onLoaded?.Invoke(go);
                    
                    if(dontDestroy)
                        Object.DontDestroyOnLoad(go);
                    return;
                }
                
                DebugTool.Log($"{key} 로드 실패", DebugType.Missing);
                onFailed?.Invoke(key);
            };

            return true;
        }

        public void Clear()
        {
            if (_root == null) return;
            
            Object.Destroy(_root);
            _root = null;
            
            DebugTool.Log("어드레서블 매니저 제거 완료", DebugType.Game);
        }
    }
}