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
        public int DataCount => keyDatas.Count;

        public void ClearData()
        {
            keyDatas.Clear();
        }

        public void AddData(KeyData data)
        {
            if (data == null)
                return;

            keyDatas.Add(data);
        }

        public void RegisterAll()
        {
            foreach (KeyData data in keyDatas)
            {
                if (data == null)
                {
                    DebugTool.Warning("KeyData 가 없습니다", DebugType.Data);
                    continue;
                }

                KeyContainer.Register(data);
            }
        }
    }
}