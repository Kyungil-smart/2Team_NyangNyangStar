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
                onFailed?.Invoke(key);
                DebugTool.Warning($"{key} : Addressable Key를 찾을 수 없습니다.", DebugType.Game);
                return false;
            }
            
            Addressables.InstantiateAsync(key, _root.transform).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = handle.Result;
                    
                    if(dontDestroy)
                        Object.DontDestroyOnLoad(go);
                    
                    DebugTool.Log($"{go.name} 로드 성공", DebugType.Game);
                    onLoaded?.Invoke(go);
                    
                    return;
                }
                
                DebugTool.Log($"{key} 로드 실패", DebugType.Missing);
                onFailed?.Invoke(key);
            };

            return true;
        }

        // TODO : 어드레서블 해제 메서드
        public bool TryReleasePrefab(string key, Action<GameObject> onReleased, Action<string> onFailed = null)
        {
            if (!KeyContainer.GetAddressableKey(key))
                {
                    onFailed?.Invoke(key);
                    DebugTool.Warning($"{key} : Addressable Key를 찾을 수 없습니다.", DebugType.Game);
                    return false;
                }

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