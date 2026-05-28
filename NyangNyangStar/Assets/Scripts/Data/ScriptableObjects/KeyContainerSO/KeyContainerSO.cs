using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Data.ScriptableObjects.KeyContainerSO
{
    [CreateAssetMenu(fileName = "KeyContainer", menuName = "SO/Addressable/KeyContainer", order = 0)]
    public class KeyContainerSo : ScriptableObject
    {
        [Header("어드레서블 키")]
        [SerializeField] private List<KeyData> keyDatas = new();
        public List<KeyData> KeyDatas => keyDatas;

        public void ClearData()
        {
            keyDatas.Clear();
        }

        public void AddData(KeyData data)
        {
            if (data == null)
                return;

            keyDatas.Add(data);
            AddKey(data);
        }

        private void AddKey(KeyData data)
        {
            if (data == null)
            {
                DebugTool.Warning("KeyData 가 없습니다.", DebugType.Data);
                return;
            }

            KeyContainer.Sprites.Add(data.Key);
        }
    }
}