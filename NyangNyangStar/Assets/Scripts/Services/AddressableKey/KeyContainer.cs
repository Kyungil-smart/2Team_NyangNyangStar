using System.Collections.Generic;

namespace Services.AddressableKey
{
    public static class KeyContainer
    {
        public static Dictionary<string, string> KeyDict = new();

        public static string EventSystem = "EventSystem";

        public static bool GetAddressableKey(string key)
        {
            if(KeyDict[key] == key)
                return true;

            DebugTool.Log("존재 하지 않는 키 입니다.", DebugType.Addressable);
            return false;
        }

        public static void InitKeyDict()
        {
            KeyDict.Clear();
            KeyDict.Add(EventSystem, EventSystem);
            
            DebugTool.Log("KeyContainer InitKeyDict", DebugType.Addressable);
        }
    }
}