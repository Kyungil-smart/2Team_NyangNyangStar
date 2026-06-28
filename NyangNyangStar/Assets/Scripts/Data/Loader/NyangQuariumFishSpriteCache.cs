using Core.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Data.Loader
{
    
    // NyangQuariumFishSO는 시트에서 FishKey까지만 받아오고, Sprite는 안 들고 있음.
    // 그 상태로 머지보드에서 아이템 생성하면 UISpriteController가 그때 Addressable 로드함
    // -> Image는 먼저 켜지고 sprite는 null ->
    // 흰색 네모 잠깐 보였다가 이미지 나오는 그 딜레이.
    
    public static class NyangQuariumFishSpriteCache
    {
        // FishKey(Addressable 키) -> 로드된 Sprite
        private static readonly Dictionary<string, Sprite> SpritesByKey = new();

        // Release할 때 쓰는 핸들
        // Sprite만 들고 있으면 Addressable 메모리 안 풀림
        private static readonly Dictionary<string, AsyncOperationHandle<Sprite>> HandlesByKey = new();

        // 프리로드 끝났는지
        // SheetLoader OnSheetCompleted 전에 true가 되어야 함
        public static bool IsLoaded { get; private set; }

        // SheetLoader.ClearDatas() 할 때 같이 비워줌
        // 핸들 Release 안 하면 스프라이트가 메모리에 계속 남음
        public static void Clear()
        {
            foreach (AsyncOperationHandle<Sprite> handle in HandlesByKey.Values)
            {
                if (!handle.IsValid())
                    continue;

                GameManager.Addressable.Release(handle);
            }

            SpritesByKey.Clear();
            HandlesByKey.Clear();
            IsLoaded = false;
        }

        // SheetLoader에서 물고기 시트 로드 직후 StartCoroutine으로 호출
        public static IEnumerator LoadSpritesCoroutine(NyangQuariumFishSO fishSO, Action onComplete = null)
        {
            
            Clear();
            IsLoaded = false;

            if (fishSO == null || fishSO.FishData == null || fishSO.FishData.Count == 0)
            {
                IsLoaded = true;
                onComplete?.Invoke();
                yield break;
            }

            // 시트에 같은 FishKey가 여러 줄 있을 수 있어서 중복 로드 방지
            HashSet<string> requestedKeys = new();

            // 비동기 LoadSprite 다 끝날 때까지 코루틴 대기용 카운터
            int pendingCount = 0;

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData fishData = fishSO.FishData[i];

                if (fishData == null || string.IsNullOrWhiteSpace(fishData.FishKey))
                    continue;

                if (!requestedKeys.Add(fishData.FishKey))
                    continue;

                pendingCount++;
                string fishKey = fishData.FishKey;

                GameManager.Addressable.LoadSprite(
                    fishKey,
                    (sprite, handle) =>
                    {
                        if (sprite != null)
                        {
                            SpritesByKey[fishKey] = sprite;
                            HandlesByKey[fishKey] = handle;
                        }

                        pendingCount--;
                    },
                    failedKey =>
                    {
                        DebugTool.Warning(
                            $"[NyangQuariumFishSpriteCache] {failedKey} Sprite 로드 실패",
                            DebugType.Addressable);
                        pendingCount--;
                    });
            }

            // 콜백 다 올 때까지 프레임 넘김 (ItemDatabaseSo랑 동일 패턴)
            while (pendingCount > 0)
                yield return null;

            IsLoaded = true;
            DebugTool.Log(
                $"[NyangQuariumFishSpriteCache] 물고기 Sprite 로드 완료 ({SpritesByKey.Count}개)",
                DebugType.Addressable);
            onComplete?.Invoke();
        }

        // NyangQuariumMergeBoardRuntime.CreateRandomItem에서 호출
        // 여기서 Sprite 받아서 ItemData.SetSprite() 넣으면 슬롯에 바로 그려짐
        public static bool TryGetSprite(string fishKey, out Sprite sprite)
        {
            sprite = null;

            if (string.IsNullOrWhiteSpace(fishKey))
                return false;

            return SpritesByKey.TryGetValue(fishKey, out sprite) && sprite != null;
        }
    }
}
