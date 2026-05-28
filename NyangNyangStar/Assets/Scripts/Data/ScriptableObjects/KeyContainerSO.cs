using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "KeyContainer", menuName = "SO/Addressable/KeyContainer", order = 0)]
    public class KeyContainerSo : ScriptableObject
    {
        [Header("어드레서블 키")]
        [SerializeField] private List<string> _key = new();
        public List<string> Key => _key;

        [Header("실제 파일 명")]
        [SerializeField] private List<string> _fileName = new();

        public List<string> FileName => _fileName;

        public void ClearData()
        {
            _key.Clear();
            _fileName.Clear();
        }

        public void AddData(string key, string fileName)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            _key.Add(key.Trim());
            _fileName.Add(fileName.Trim());

            DebugTool.Log($"[KeyContainer] {key} = {fileName}", DebugType.Data);
        }
    }
}