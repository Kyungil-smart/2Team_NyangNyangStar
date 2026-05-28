using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Data.ScriptableObjects.KeyContainer
{
    [CreateAssetMenu(fileName = "KeyContainer", menuName = "SO/Addressable/KeyContainer", order = 0)]
    public class KeyContainerSo : ScriptableObject
    {
        [Header("어드레서블 키")]
        [SerializeField] private List<KeyData> keyDatas = new();
        public List<KeyData> KeyDatas => keyDatas;

        public void ClearData()
            => keyDatas.Clear();

        public void AddData(KeyData data)
        {
            if (data == null)
                return;

            keyDatas.Add(data);
        }
    }
}