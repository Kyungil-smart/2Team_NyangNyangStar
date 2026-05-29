using Services.Enums;
using System;
using UnityEngine;

namespace Data.ScriptableObjects.KeyContainerSO
{
    [Serializable]
    public class KeyData
    {
        [SerializeField] private string key;
        public string Key { get => key; set => key = value; }
        [SerializeField] private string fileName;
        public string FileName { get => fileName; set => fileName = value; }
        [SerializeField] private string usage;
        public string Usage { get => usage; set => usage = value; }
        [SerializeField] private AddressableGroupType groupType;
        public AddressableGroupType GroupType { get => groupType; set => groupType = value; }
        [SerializeField] private LabelType labelType;
        public LabelType LabelType { get => labelType; set => labelType = value; }
        [SerializeField] private BuildType buildType;
        public BuildType BuildType { get => buildType; set => buildType = value; }
    }
}