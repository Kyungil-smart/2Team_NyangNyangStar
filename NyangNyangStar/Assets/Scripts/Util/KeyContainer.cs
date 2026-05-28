using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Util
{
    public static class KeyContainer
    {
        public static Dictionary<string, List<GameObject>> PrefabKeyDict = new();
        public static HashSet<string> AudioClips = new();
        public static HashSet<string> Sprites = new();

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
            
            public const string ScratcherSeasonEventPanel = "ScratcherSeasonEventPanel";
        }

        public static bool IsPrefabKey(string key)
        {
            if (!PrefabKeyDict.ContainsKey(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Prefab Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static bool IsAudioKey(string key)
        {
            if (!AudioClips.Contains(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Audio Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static bool IsSpriteKey(string key)
        {
            if (!Sprites.Contains(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Image Key 입니다.", DebugType.Addressable);
                return false;
            }
            return true;
        }

        public static void AddPrefab(string key, GameObject prefab)
        {
            PrefabKeyDict[key].Add(prefab);
            DebugTool.Log($"{key} :  Prefab : {prefab.name}", DebugType.Addressable);
        }

        public static void RemovePrefab(string key, GameObject prefab)
            => PrefabKeyDict[key].Remove(prefab);

        public static bool IsPrefabActive(string key, GameObject go)
        {
            if (!PrefabKeyDict.ContainsKey(key))
                return false;

            if (!PrefabKeyDict[key].Contains(go))
            {
                DebugTool.Log("해당 프리팹이 존재하지 않습니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static void InitKeys()
        {
            PrefabKeyDictInit();
            AudioClipsInit();
            SpritesInit();
            
            PrintKeys();
        }

        public static void ClearKeys()
        {
            PrintKeys();
            
            PrefabKeyDict.Clear();
            AudioClips.Clear();
            Sprites.Clear();
        }

        private static void PrefabKeyDictInit()
        {
            PrefabKeyDict.Clear();

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
            
            PrefabKeyDict.Add(Prefabs.ScratcherSeasonEventPanel, new List<GameObject>());
        }

        private static void AudioClipsInit()
        {
            AudioClips.Clear();
        }

        private static void SpritesInit()
        {
            Sprites.Clear();
        }

        private static void PrintKeys()
        {
            StringBuilder log = new();
            log.AppendLine("[어드레서블 키 초기화]");

            log.AppendLine("[Prefabs]");
            foreach (string key in PrefabKeyDict.Keys)
                log.AppendLine(key);

            log.AppendLine("[Audio]");
            foreach (string key in AudioClips)
                log.AppendLine(key);
            
            log.AppendLine("[Sprite]");
            foreach (string key in Sprites)
                log.AppendLine(key);

            DebugTool.Log(log.ToString(), DebugType.Addressable);
        }
    }
}