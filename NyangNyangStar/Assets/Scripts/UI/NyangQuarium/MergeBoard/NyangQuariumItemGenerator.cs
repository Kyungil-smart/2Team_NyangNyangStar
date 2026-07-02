using Data.Loader;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UI.MergeBoard;
using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    // NyangQuariumItemGenerator -> Item Generator 버튼, 클릭 시 랜덤 또는 InputItemID 지정 아이템 생성
    public sealed class NyangQuariumItemGenerator : MonoBehaviour
    {
        private const string InputItemIdFieldName = "InputItemID";
        private const int GeneratorItemIdOffset = 200000;

        private NyangQuariumItemBoard _board;
        private NyangQuariumRewardQueue _rewardQueue;
        private Button _button;
        private TMP_InputField _itemIdInputField;
        private int _fallbackItemId = 1;
        private readonly string[] _fallbackItemNames =
        {
            "Random Item A",
            "Random Item B",
            "Random Item C",
            "Random Item D",
            "Random Item E"
        };

        public void Init(NyangQuariumItemBoard board, NyangQuariumRewardQueue rewardQueue)
        {
            _board = board;
            _rewardQueue = rewardQueue;
            _rewardQueue?.SetOnItemAddedToBoard(UnlockCollectionItem);
            BindButton();
            BindInputField();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(GenerateItem);
        }

        // 자기 자신 또는 자식에서 Button 찾아서 GenerateItem 연결
        private void BindButton()
        {
            Button button = GetComponent<Button>();
            if (button == null)
                button = GetComponentInChildren<Button>(true);

            if (_button == button)
                return;

            if (_button != null)
                _button.onClick.RemoveListener(GenerateItem);

            _button = button;

            if (_button != null)
                _button.onClick.AddListener(GenerateItem);
            else
                DebugTool.Warning("[NyangQuariumItemGenerator] Button 컴포넌트를 찾지 못했습니다.", DebugType.UI, this);
        }

        private void BindInputField()
        {
            if (_itemIdInputField != null)
                return;

            Transform root = transform;
            while (root.parent != null)
                root = root.parent;

            Transform inputTransform = FindChildTransform(root, InputItemIdFieldName);
            if (inputTransform == null)
            {
                DebugTool.Warning(
                    "[NyangQuariumItemGenerator] InputItemID를 찾지 못했습니다.",
                    DebugType.UI,
                    this);
                return;
            }

            _itemIdInputField = inputTransform.GetComponent<TMP_InputField>();
            if (_itemIdInputField == null)
                DebugTool.Warning(
                    "[NyangQuariumItemGenerator] InputItemID에 TMP_InputField가 없습니다.",
                    DebugType.UI,
                    this);
        }

        public void GenerateItem()
        {
            if (_board == null)
                return;

            BindInputField();

            NyangQuariumBoardItem item = null;
            bool usedInputItemId = false;

            if (TryReadInputItemId(out int itemId, out bool hasInput))
            {
                if (!TryCreateItemById(itemId, out item))
                {
                    DebugTool.Warning(
                        $"[NyangQuariumItemGenerator] {itemId} ID에 해당하는 아이템 데이터를 찾지 못했습니다.",
                        DebugType.UI,
                        this);
                    return;
                }

                usedInputItemId = true;
            }
            else if (hasInput)
                return;
            else
                item = CreateRandomItem();

            if (item == null || !item.HasItem)
                return;

            bool added = false;

            if (_rewardQueue != null)
            {
                _rewardQueue.EnqueueItem(item);
                added = true;
            }
            else if (_board.TryAddItem(item))
            {
                added = true;
                UnlockCollectionItem(item);
            }

            if (usedInputItemId && added)
                ClearInputItemIdField();
        }

        private async void UnlockCollectionItem(NyangQuariumBoardItem item)
        {
            if (!IsKnownFishItem(item))
                return;

            if (!await WaitForFirestoreReadyAsync())
            {
                DebugTool.Warning("[NyangQuariumItemGenerator] Firestore가 준비되지 않아 도감 해금 저장을 생략합니다.", DebugType.Data, this);
                return;
            }

            FireStoreManager fireStoreManager = FireStoreManager.Instance;

            if (fireStoreManager == null ||
                !fireStoreManager.IsInitialized ||
                !fireStoreManager.TryGetStore(out NyangQuariumFirestoreSO nyangQuariumSO) ||
                nyangQuariumSO == null)
            {
                DebugTool.Warning("[NyangQuariumItemGenerator] Firestore가 준비되지 않아 도감 해금 저장을 생략합니다.", DebugType.Data, this);
                return;
            }

            try
            {
                await nyangQuariumSO.UnlockFishAsync(item.Id);
            }
            catch (System.Exception e)
            {
                DebugTool.Warning($"[NyangQuariumItemGenerator] 도감 해금 저장 실패: {e.Message}", DebugType.Data, this);
            }
        }

        private static async Task<bool> WaitForFirestoreReadyAsync(int timeoutMs = 5000)
        {
            int elapsedMs = 0;
            const int intervalMs = 100;

            while (elapsedMs < timeoutMs)
            {
                if (FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized)
                    return true;

                await Task.Delay(intervalMs);
                elapsedMs += intervalMs;
            }

            return FireStoreManager.Instance != null && FireStoreManager.Instance.IsInitialized;
        }

        private bool IsKnownFishItem(NyangQuariumBoardItem item)
        {
            if (item == null || !item.HasItem || item.Id <= 0)
                return false;

            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader == null ||
                !sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO fishSO) ||
                fishSO == null ||
                fishSO.FishData == null)
            {
                return false;
            }

            for (int i = 0; i < fishSO.FishData.Count; i++)
            {
                NyangQuariumFishData fishData = fishSO.FishData[i];

                if (fishData != null && fishData.FishId == item.Id)
                    return true;
            }

            return false;
        }

        private void ClearInputItemIdField()
        {
            if (_itemIdInputField == null)
                return;

            _itemIdInputField.text = string.Empty;
            _itemIdInputField.ReleaseSelection();
        }

        private bool TryReadInputItemId(out int itemId, out bool hasInput)
        {
            itemId = 0;
            hasInput = false;

            if (_itemIdInputField == null)
                return false;

            string input = _itemIdInputField.text?.Trim();
            if (string.IsNullOrEmpty(input))
                return false;

            hasInput = true;

            if (!int.TryParse(input, out itemId) || itemId <= 0)
            {
                DebugTool.Warning(
                    $"[NyangQuariumItemGenerator] 아이템 ID 입력값이 올바르지 않습니다. 입력값: {input}",
                    DebugType.UI,
                    this);
                return false;
            }

            return true;
        }

        private bool TryCreateItemById(int itemId, out NyangQuariumBoardItem item)
        {
            item = null;

            if (itemId <= 0)
                return false;

            TryResolveFishSO(out NyangQuariumFishSO fishSO);
            TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO);

            if (fishSO?.FishData != null)
            {
                for (int i = 0; i < fishSO.FishData.Count; i++)
                {
                    NyangQuariumFishData fishData = fishSO.FishData[i];
                    if (fishData == null || fishData.FishId != itemId)
                        continue;

                    item = CreateFishItem(fishData);
                    return item != null && item.HasItem;
                }
            }

            if (generatorSO != null)
            {
                if (itemId >= GeneratorItemIdOffset &&
                    generatorSO.TryGetById(itemId - GeneratorItemIdOffset, out NyangQuariumGeneratorData offsetGeneratorData))
                {
                    item = CreateGeneratorItem(offsetGeneratorData);
                    return item != null && item.HasItem;
                }

                if (generatorSO.TryGetById(itemId, out NyangQuariumGeneratorData generatorData))
                {
                    item = CreateGeneratorItem(generatorData);
                    return item != null && item.HasItem;
                }
            }

            return false;
        }

        private NyangQuariumBoardItem CreateRandomItem()
        {
            TryResolveFishSO(out NyangQuariumFishSO fishSO);
            TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO);

            List<NyangQuariumBoardItem> candidates = new();

            if (fishSO?.FishData != null && fishSO.FishData.Count > 0)
            {
                for (int i = 0; i < fishSO.FishData.Count; i++)
                {
                    NyangQuariumBoardItem candidate = CreateFishItem(fishSO.FishData[i]);
                    if (candidate != null && candidate.HasItem)
                        candidates.Add(candidate);
                }
            }

            if (generatorSO?.Generators != null && generatorSO.Generators.Count > 0)
            {
                for (int i = 0; i < generatorSO.Generators.Count; i++)
                {
                    NyangQuariumBoardItem candidate = CreateGeneratorItem(generatorSO.Generators[i]);
                    if (candidate != null && candidate.HasItem)
                        candidates.Add(candidate);
                }
            }

            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];

            int fallbackIndex = Random.Range(0, _fallbackItemNames.Length);
            int fallbackId = _fallbackItemId++;
            ItemData fallbackItem = new(
                fallbackId,
                _fallbackItemNames[fallbackIndex],
                fallbackIndex + 1,
                ItemType.Common);

            DebugTool.Warning(
                "[NyangQuariumItemGenerator] NyangQuariumFishSO가 준비되지 않아 임시 랜덤 아이템을 생성했습니다.",
                DebugType.UI,
                this);
            return new NyangQuariumBoardItem(fallbackItem);
        }

        private static NyangQuariumBoardItem CreateFishItem(NyangQuariumFishData fishData)
        {
            if (fishData == null || fishData.FishId <= 0)
                return null;

            ItemData itemData = new(
                fishData.FishId,
                fishData.FishName,
                Mathf.Max(1, fishData.Level),
                ItemType.Common,
                fishData.FishKey);

            if (NyangQuariumFishSpriteCache.TryGetSprite(fishData.FishKey, out Sprite sprite))
                itemData.SetSprite(sprite);

            return new NyangQuariumBoardItem(itemData);
        }

        private static NyangQuariumBoardItem CreateGeneratorItem(NyangQuariumGeneratorData generatorData)
        {
            if (generatorData == null || generatorData.GeneratorId <= 0)
                return null;

            string addressableKey = GetGeneratorSpriteKey(generatorData);
            RegisterGeneratorSpriteKey(addressableKey);

            ItemData itemData = new(
                GeneratorItemIdOffset + generatorData.GeneratorId,
                generatorData.GeneratorName,
                Mathf.Max(1, generatorData.Level),
                ItemType.Common,
                addressableKey);

            return new NyangQuariumBoardItem(itemData);
        }

        public static bool TryGetGeneratorSpriteKey(ItemData itemData, out string addressableKey)
        {
            addressableKey = itemData != null ? itemData.AddressableKey : string.Empty;

            if (itemData == null || itemData.ItemID < GeneratorItemIdOffset)
            {
                if (!string.IsNullOrWhiteSpace(addressableKey))
                {
                    RegisterGeneratorSpriteKey(addressableKey);
                    return true;
                }

                return false;
            }

            int generatorId = itemData.ItemID - GeneratorItemIdOffset;
            if (TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO) &&
                generatorSO != null &&
                generatorSO.TryGetById(generatorId, out NyangQuariumGeneratorData generatorData))
            {
                addressableKey = GetGeneratorSpriteKey(generatorData);
                RegisterGeneratorSpriteKey(addressableKey);
                return !string.IsNullOrWhiteSpace(addressableKey);
            }

            if (!string.IsNullOrWhiteSpace(addressableKey))
            {
                RegisterGeneratorSpriteKey(addressableKey);
                return true;
            }

            return false;
        }

        private static string GetGeneratorSpriteKey(NyangQuariumGeneratorData generatorData)
        {
            if (generatorData == null)
                return string.Empty;

            switch (generatorData.GeneratorName?.Trim())
            {
                case "작은 담수어 어항":
                    return "Gen_FreshTank_Small";
                case "중간 담수어 어항":
                    return "Gen_FreshTank_Medium";
                case "큰 담수어 어항":
                    return "Gen_FreshTank_Large";
                case "작은 기수어 어항":
                    return "Gen_BrackishTank_Small";
                case "중간 기수어 어항":
                    return "Gen_BrackishTank_Medium";
                case "큰 기수어 어항":
                    return "Gen_BrackishTank_Large";
                case "작은 해수어 어항":
                    return "Gen_SaltTank_Small";
                case "중간 해수어 어항":
                    return "Gen_SaltTank_Medium";
                case "큰 해수어 어항":
                    return "Gen_SaltTank_Large";
                case "작은 자연요소 상자":
                    return "Gen_EnvBox_Small";
                case "중간 자연요소 상자":
                    return "Gen_EnvBox_Medium";
                case "큰 자연요소 상자":
                    return "Gen_EnvBox_Large";
                default:
                    return string.Empty;
            }
        }

        private static void RegisterGeneratorSpriteKey(string addressableKey)
        {
            if (!string.IsNullOrWhiteSpace(addressableKey))
                Util.KeyContainer.Sprites.Add(addressableKey);
        }

        private static bool TryResolveFishSO(out NyangQuariumFishSO fishSO)
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumFishSO(out fishSO))
                return fishSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            fishSO = quariumLoader != null ? quariumLoader.FishSO : null;
            return fishSO != null;
        }

        private static bool TryResolveGeneratorSO(out NyangQuariumGeneratorSO generatorSO)
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null && sheetLoader.TryGetNyangQuariumGeneratorSO(out generatorSO))
                return generatorSO != null;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            generatorSO = quariumLoader != null ? quariumLoader.GeneratorSO : null;
            return generatorSO != null;
        }

        private static Transform FindChildTransform(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name == objectName)
                    return child;
            }

            return null;
        }
    }
}
