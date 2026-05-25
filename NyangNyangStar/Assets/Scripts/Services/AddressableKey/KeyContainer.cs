using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Services.AddressableKey
{
    public static class KeyContainer
    {
        private static Dictionary<string, List<GameObject>> prefabKeyDict = new();
        private static HashSet<string> audioClips = new();
        private static HashSet<string> sprites = new();

        public static class Prefabs
        {
            public const string EventSystem = "EventSystem";
            public const string TestUI = "TestScene";
            public const string VolumePopupUI = "VolumePopupUI";
            public const string BGMPopupUI = "BGMPopupUI";
        }

        public static class Audio
        {
            public const string TitleBGM = "BGM/Title.mp3";
            public const string BGM01 = "BGM/BGM 01.mp3";
            public const string BGM02 = "BGM/BGM 02.mp3";
            public const string BGM03 = "BGM/BGM 03.mp3";
        }

        public static class Image
        {
            
        }

        public static bool IsPrefabKey(string key)
        {
            if (!prefabKeyDict.ContainsKey(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Prefab Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static bool IsAudioKey(string key)
        {
            if (!audioClips.Contains(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Audio Key 입니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }

        public static bool IsSpriteKey(string key)
        {
            if (!sprites.Contains(key))
            {
                DebugTool.Log($"{key} : 존재 하지 않는 Image Key 입니다.", DebugType.Addressable);
                return false;
            }
            return true;
        }

        public static void AddPrefab(string key, GameObject prefab)
        {
            prefabKeyDict[key].Add(prefab);
            DebugTool.Log($"{key} :  Prefab : {prefab.name}", DebugType.Addressable);
        }

        public static void RemovePrefab(string key, GameObject prefab)
            => prefabKeyDict[key].Remove(prefab);

        public static bool IsPrefabActive(string key, GameObject go)
        {
            if (!prefabKeyDict.ContainsKey(key))
                return false;

            if (!prefabKeyDict[key].Contains(go))
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
            ImagesInit();
            
            PrintKeys();
        }

        private static void PrefabKeyDictInit()
        {
            prefabKeyDict.Clear();

            prefabKeyDict.Add(Prefabs.EventSystem, new List<GameObject>());
            prefabKeyDict.Add(Prefabs.TestUI, new List<GameObject>());
            prefabKeyDict.Add(Prefabs.VolumePopupUI, new List<GameObject>());
            prefabKeyDict.Add(Prefabs.BGMPopupUI, new List<GameObject>());
        }

        private static void AudioClipsInit()
        {
            audioClips.Clear();
            
            audioClips.Add(Audio.TitleBGM);
            audioClips.Add(Audio.BGM01);
            audioClips.Add(Audio.BGM02);
            audioClips.Add(Audio.BGM03);
        }

        private static void ImagesInit()
        {
            sprites.Clear();
        }

        private static void PrintKeys()
        {
            StringBuilder log = new();
            log.AppendLine("[어드레서블 키 초기화]");

            log.AppendLine("[Prefabs]");
            foreach (string key in prefabKeyDict.Keys)
                log.AppendLine(key);

            log.AppendLine("[Audio]");
            foreach (string key in audioClips)
                log.AppendLine(key);

            DebugTool.Log(log.ToString(), DebugType.Addressable);
        }
    }
}