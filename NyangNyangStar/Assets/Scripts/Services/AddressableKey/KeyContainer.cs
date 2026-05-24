using System.Collections.Generic;
using UnityEngine;

namespace Services.AddressableKey
{
    public static class KeyContainer
    {
        private static Dictionary<string, List<GameObject>> KeyDict = new();

        public static readonly string EventSystem = "EventSystem";
        public static readonly string TestUI = "TestScene";
        public static readonly string PopupUI = "UITestPopup";

        public static bool IsContainsKey(string key)
        {
            if (!KeyDict.ContainsKey(key))
            {
                DebugTool.Log("존재 하지 않는 키 입니다.", DebugType.Addressable);
                return false;
            }
            return true;
        }

        public static void AddPrefab(string key, GameObject prefab)
        {
            KeyDict[key].Add(prefab);
            DebugTool.Log($"{key} :  Prefab : {prefab.name}", DebugType.Addressable);
        }

        public static void RemovePrefab(string key, GameObject prefab)
            => KeyDict[key].Remove(prefab);

        public static bool IsPrefabActive(string key, GameObject go)
        {
            if (!KeyDict.ContainsKey(key))
                return false;

            if (!KeyDict[key].Contains(go))
            {
                DebugTool.Log("해당 프리팹이 존재하지 않습니다.", DebugType.Addressable);
                return false;
            }

            return true;
        }
        
        

        public static void InitKeyDict()
        {
            KeyDict.Clear();
            
            KeyDict.Add(EventSystem, new List<GameObject>());
            KeyDict.Add(TestUI, new List<GameObject>());
            KeyDict.Add(PopupUI, new List<GameObject>());

            string log = "[어드레서블 키 딕셔너리 초기화]";

            foreach (string key in KeyDict.Keys)
                log += $"\n{key}";

            DebugTool.Log($"{log}", DebugType.Addressable);
        }
    }
}