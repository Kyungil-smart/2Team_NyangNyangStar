using Services.AddressableKey;
using Services.Scriptable_Object;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Managers
{
    public class AddressableManager : ISubManager
    {
        private GameObject _root;

        public bool TryLoadPrefab(string key)
        {
            if (!KeyContainer.GetAddressableKey(key))
                return false;
            
            GameObject prefab = null;
            Addressables.InstantiateAsync(key).Completed += handle =>
            {
                // 로드 및 생성 성공
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    // EventSystem 가져오기
                    prefab = handle.Result;

                    Object.DontDestroyOnLoad(prefab);
                }
                else
                {
                    DebugTool.Log($"{key} 로드 실패", DebugType.Missing);
                }
            };

            return true;
        }
        
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

        public void Clear()
        {
            if (_root == null) return;
            
            Object.Destroy(_root);
            _root = null;
            
            DebugTool.Log("어드레서블 매니저 제거 완료", DebugType.Game);
        }
    }
}