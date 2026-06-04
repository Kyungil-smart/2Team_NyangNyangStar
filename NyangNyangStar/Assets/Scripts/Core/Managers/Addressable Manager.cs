﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Util;
using Object = UnityEngine.Object;

namespace Core.Managers
{
    public class AddressableManager : ISubManager
    {
        private GameObject _root;
        
        public void Init()
        {
            KeyContainer.InitKeys();
            
            _root = GameObject.Find("@Addressable");
            
            if (_root == null)
            {
                _root = new GameObject { name = "@Addressable" };
                Object.DontDestroyOnLoad(_root);
            }

            DebugTool.Log("어드레서블 매니저 초기화 완료", DebugType.Game);
        }

        public void LoadPrefab(string key, Action<GameObject> onLoaded,  Action<string> onFailed = null,  bool dontDestroy = false)
        {
            
            if (!KeyContainer.ContainsPrefabsKey(key))
            {
                onFailed?.Invoke(key);
                return;
            }
            
            AsyncOperationHandle<IList<IResourceLocation>> locationHandle = 
                Addressables.LoadResourceLocationsAsync(key, typeof(GameObject));

            locationHandle.Completed += locationResult =>
            {
                // 3. Location 검사 실패 또는 결과 없음
                if (locationResult.Status != AsyncOperationStatus.Succeeded ||
                    locationResult.Result == null ||
                    locationResult.Result.Count == 0)
                {
                    DebugTool.Warning($"{key} : Addressables Catalog에 존재하지 않는 키입니다.", DebugType.Missing);

                    Addressables.Release(locationResult);

                    onFailed?.Invoke(key);
                    return;
                }
            
                // Location 조회용 handle은 여기서 해제
                Addressables.Release(locationResult);

                AsyncOperationHandle<GameObject> prefabHandle = 
                    Addressables.InstantiateAsync(key);
                
                prefabHandle.Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        GameObject go = handle.Result;

                        if (go == null)
                        {
                            DebugTool.Warning($"{key} : 로드된 프리팹 인스턴스가 이미 제거되었습니다.", DebugType.Addressable);
                            onFailed?.Invoke(key);
                            return;
                        }

                        KeyContainer.AddPrefab(key, go);

                        if (go == null)
                        {
                            DebugTool.Warning($"{key} : 프리팹 캐시 등록 중 인스턴스가 제거되었습니다.", DebugType.Addressable);
                            onFailed?.Invoke(key);
                            return;
                        }

                        if (dontDestroy)
                            Object.DontDestroyOnLoad(go);

                        DebugTool.Log($"{go.name} 로드 성공", DebugType.Game);
                        onLoaded?.Invoke(go);

                        return;
                    }

                    DebugTool.Log($"{key} 로드 실패", DebugType.Missing);
                    onFailed?.Invoke(key);
                };
            };
        }

        public void LoadAudioClip(string key, 
            Action<AudioClip, AsyncOperationHandle<AudioClip>> onLoaded,
            Action<string> onFailed = null)
        {
            if (!KeyContainer.IsAudioKey(key))
            {
                onFailed?.Invoke(key);
                return;
            }

            AsyncOperationHandle<AudioClip> clipHandle = 
                Addressables.LoadAssetAsync<AudioClip>(key);

            clipHandle.Completed += handle =>
            {
                // 3. Location 검사 실패 또는 결과 없음
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    AudioClip clip = handle.Result;

                    if (clip == null)
                    {
                        DebugTool.Warning($"{key} : AudioClip이 null 입니다", DebugType.Addressable);
                        Addressables.Release(handle);
                        onFailed?.Invoke(key);
                        return;
                    }

                    DebugTool.Log($"{key} : AudioClip 로드 성공", DebugType.Addressable);
                    onLoaded?.Invoke(clip, handle);
                    return;
                }
                DebugTool.Warning($"{key} : AudioClip 로드 실패", DebugType.Addressable);
                Addressables.Release(handle);
                onFailed?.Invoke(key);
            };
        }
        
        public void LoadSprite(string key,
            Action<Sprite, AsyncOperationHandle<Sprite>> onLoaded,
            Action<string> onFailed = null)
        {
            if (!KeyContainer.IsSpriteLoadableKey(key))
            {
                onFailed?.Invoke(key);
                return;
            }

            AsyncOperationHandle<Sprite> spriteHandle = 
                Addressables.LoadAssetAsync<Sprite>(key);

            spriteHandle.Completed += handle =>
            {
                // 3. Location 검사 실패 또는 결과 없음
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Sprite sprite = handle.Result;

                    if (sprite == null)
                    {
                        DebugTool.Warning($"{key} : 로드 결과가 null 입니다", DebugType.Addressable);
                        Addressables.Release(handle);
                        onFailed?.Invoke(key);
                        return;
                    }

                    DebugTool.Log($"{key} : 로드 성공", DebugType.Addressable);
                    onLoaded?.Invoke(sprite, handle);
                    return;
                }
                DebugTool.Warning($"{key} : Addressable.LoadAssetAsync<Sprite> 실패 / Status: {handle.Status}", DebugType.Addressable);
                Addressables.Release(handle);
                onFailed?.Invoke(key);
            };
        }
        
        public bool TryReleasePrefab(string key, GameObject prefab)
        {
            if(!KeyContainer.ContainsPrefabsKey(key))
                return false;
            
            if (!KeyContainer.IsPrefabActive(key, prefab))
                return false;
            
            bool result = Addressables.ReleaseInstance(prefab);

            if (!result)
                return false;
            
            KeyContainer.RemovePrefab(key, prefab);
            return true;
        }

        public void Release<T>(AsyncOperationHandle<T> _handle) where T : class
        {
            Addressables.Release(_handle);
            DebugTool.Log($"{typeof(T).Name} 해제 완료", DebugType.Addressable);
        }

        public void Clear()
        {
            if (_root == null) return;
            
            Object.Destroy(_root);
            _root = null;
            
            KeyContainer.ClearKeys();
            DebugTool.Log("어드레서블 매니저 제거 완료", DebugType.Game);
        }
    }
}
