using Data.ScriptableObjects.KeyContainerSO;
using Services.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Util
{
    public static class KeyContainer
    {
        public static Dictionary<string, List<GameObject>> PrefabKeyDict = new();
        private static Dictionary<string, KeyData> _keyDataDict  = new();
        
        private static readonly Dictionary<AddressableGroupType, List<string>> _keysByGroup = new();
        private static readonly Dictionary<(AddressableGroupType group, LabelType label), List<string>> _keysByGroupAndLabel = new();

        public static readonly HashSet<string> Sprites = new();
        public static readonly HashSet<string> Audios = new();
        
        public static class Prefabs
        {
            public const string EventSystem = "EventSystem";
            public const string SheetLoader = "SheetLoader";

            // 메인 화면 UI 프리팹
            public const string MainUI = "MainUI";
            public const string ShopPopupUI = "ShopPopupUI";
            public const string EventPopupUI = "EventPopupUI";
            public const string DailyCheckInPopupUI = "DailyCheckInPopupUI";
            public const string MailPopupUI = "MailPopupUI";
            public const string SettingsPopupUI = "SettingsPopupUI";
            public const string CollectionPopupUI = "CollectionPopupUI";
            public const string StoryBookPopupUI = "StoryBookPopupUI";
            public const string NotebookPopupUI = "NotebookPopupUI";
            public const string RoulettePopupUI = "RoulettePopupUI";
            public const string AffinityPopupUI = "AffinityPopupUI";
            
            // 머지보드 UI 프리펩
            public const string MergeBoard = "MergeBoard";

            //냥스타그램 UI 프리펩 NyangStargram
            public const string NyangStargramHomeProfile = "UINyangstagramHome & Profile";
            public const string NyangStargramAddPostPopUpUI = "게시물 추가 화면 Canvas";
            public const string NyangStargramNoticePopUpUI = "Nyagram알림 켄버스";
            public const string NyangStargramNPCProfilePopUpUI = "Npc프로필 화면";
            public const string NyangStargramPostPopUpUI = "게시물 Canvas";
            public const string NyangStargramDMListPopUpUI = "NyagramDM리스트 켄버스";
            public const string NyangStargramDMchatPopUpUI = "NyagramDM 대화";

            // 냥냥스냅 UI 프리팹
            public const string NyangNyangSnapStagePopUpUI = "NyangNyangSnapStagePopupUI";
            public const string NyangNyangSnapSnackPopupUI = "NyangNyangSnapSnackCanvas";
            public const string NyangNyangSnapToyPopupUI = "NyangNyangSnapToyCanvas";
            public const string NyangNyangSnapPopupUI = "NyangNyangSnapPopupUI";
            public const string NyangNyangSnapResultPopupUI = "NyangNyangSnapResultCanvas";


            public const string ScratchingTime = "ScratchingTimeScreen";
        }

        public static void Register(KeyData data)
        {
            if (data == null)
            {
                DebugTool.Warning("KeyData 가 없습니다", DebugType.Data);
                return;
            }

            if (string.IsNullOrEmpty(data.Key))
            {
                DebugTool.Warning("Key 값이 비어 있습니다", DebugType.Data);
                return;
            }
            
            _keyDataDict[data.Key] = data;
            
            RegisterByType(data);
            RegisterByGroup(data);
        }

        public static void RegisterByType(KeyData data)
        {
            switch (data.LabelType)
            {
                case LabelType.Sprite:
                    Sprites.Add(data.Key);
                    break;
                case LabelType.Audio:
                    Audios.Add(data.Key);
                    break;
                default :
                    DebugTool.Warning("잘못된 레이블 타입입니다", DebugType.Data);
                    return;
            }
        }

        private static void RegisterByGroup(KeyData data)
        {
            if (data.GroupType == AddressableGroupType.None)
            {
                DebugTool.Warning($"{data.Key} : GroupType이 None 입니다", DebugType.Data);
                return;
            }

            if (!_keysByGroup.TryGetValue(data.GroupType, out List<string> groupKeys))
            {
                groupKeys = new List<string>();
                _keysByGroup[data.GroupType] = groupKeys;
            }

            if (!groupKeys.Contains(data.Key))
                groupKeys.Add(data.Key);

            var groupLabelKey = (data.GroupType, data.LabelType);

            if (!_keysByGroupAndLabel.TryGetValue(groupLabelKey, out List<string> groupLabelKeys))
            {
                groupLabelKeys = new List<string>();
                _keysByGroupAndLabel[groupLabelKey] = groupLabelKeys;
            }

            if (!groupLabelKeys.Contains(data.Key))
                groupLabelKeys.Add(data.Key);
        }

        public static IReadOnlyList<string> GetKeysByGroup(AddressableGroupType groupType)
        {
            if (_keysByGroup.TryGetValue(groupType, out List<string> keys))
                return keys;

            return Array.Empty<string>();
        }

        public static IReadOnlyList<string> GetKeysByGroupAndLabel(
            AddressableGroupType groupType,
            LabelType labelType)
        {
            var key = (groupType, labelType);

            if (_keysByGroupAndLabel.TryGetValue(key, out List<string> keys))
                return keys;
            
            return Array.Empty<string>();
        }
        
        public static bool ContainsPrefabsKey(string key)
        {
            if (!PrefabKeyDict.ContainsKey(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Prefab Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static bool IsSpriteLoadableKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                DebugTool.Warning("Key 값이 비어 있습니다.", DebugType.Addressable);
                return false;
            }

            if (Sprites.Contains(key))
                return true;

            DebugTool.Warning($"{key} : Sprite로 로드 가능한 Key가 아닙니다.", DebugType.Addressable);
            return false;
        }
        
        public static bool IsAudioKey(string key)
        {
            if (!Audios.Contains(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }
        
        public static bool TryGetData(string key, out KeyData data)
            => _keyDataDict.TryGetValue(key, out data);
        
        public static void AddPrefab(string key, GameObject prefab)
        {
            if (string.IsNullOrEmpty(key) || !PrefabKeyDict.ContainsKey(key))
            {
                DebugTool.Warning($"{key} : 등록되지 않은 Prefab Key 입니다.", DebugType.Addressable);
                return;
            }

            if (prefab == null)
            {
                DebugTool.Warning($"{key} : Prefab 인스턴스가 없습니다.", DebugType.Addressable);
                return;
            }

            PrefabKeyDict[key].Add(prefab);
            DebugTool.Log($"{key} :  Prefab : {prefab.name}", DebugType.Addressable);
        }

        public static void RemovePrefab(string key, GameObject prefab)
            => PrefabKeyDict[key].Remove(prefab);

        public static bool IsPrefabActive(string key, GameObject go)
        {
            if (!PrefabKeyDict.ContainsKey(key))
                return false;

            if (PrefabKeyDict[key].Contains(go))
            {
                DebugTool.Log("이미 로드한 프리펩입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static void InitKeys()
        {
            ClearKeys();
            PrefabKeyDictInit();
        }

        public static void ClearKeys()
        {
            PrefabKeyDict.Clear();
            _keyDataDict.Clear();
            
            _keysByGroup.Clear();
            _keysByGroupAndLabel.Clear();
            
            Sprites.Clear();
            Audios.Clear();
            
            Audios.Add("BGM_Main");
            
            PrintKeys();
        }

        private static void PrefabKeyDictInit()
        {
            PrefabKeyDict.Add(Prefabs.EventSystem, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.SheetLoader, new List<GameObject>());

            PrefabKeyDict.Add(Prefabs.MainUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.ShopPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.EventPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.DailyCheckInPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.MailPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.SettingsPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.CollectionPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.StoryBookPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NotebookPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.RoulettePopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.AffinityPopupUI, new List<GameObject>());
            
            PrefabKeyDict.Add(Prefabs.MergeBoard, new List<GameObject>());

            //냥스타 그램
            PrefabKeyDict.Add(Prefabs.NyangStargramHomeProfile, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramAddPostPopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramNoticePopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramNPCProfilePopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramPostPopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramDMListPopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangStargramDMchatPopUpUI, new List<GameObject>());

            // 냥냥스냅
            PrefabKeyDict.Add(Prefabs.NyangNyangSnapStagePopUpUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangNyangSnapSnackPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangNyangSnapToyPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangNyangSnapPopupUI, new List<GameObject>());
            PrefabKeyDict.Add(Prefabs.NyangNyangSnapResultPopupUI, new List<GameObject>());

            PrefabKeyDict.Add(Prefabs.ScratchingTime, new List<GameObject>());
        }

        public static void PrintKeys()
        {
            StringBuilder log = new();
            log.AppendLine("[어드레서블 키 초기화]");

            log.AppendLine("[Prefabs]");
            foreach (string key in PrefabKeyDict.Keys)
                log.AppendLine(key);
            log.AppendLine();
            
            log.AppendLine("[Sprite]");
            foreach (string key in Sprites)
                log.AppendLine(key);
            log.AppendLine();
            
            log.AppendLine("[Audio]");
            foreach (string key in Audios)
                log.AppendLine(key);

            DebugTool.Log(log.ToString(), DebugType.Addressable);
        }
    }
}
