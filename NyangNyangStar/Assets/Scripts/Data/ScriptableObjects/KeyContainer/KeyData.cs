using Services.Enums;
using System;
using UnityEngine;

namespace Data.ScriptableObjects.KeyContainer
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
        [SerializeField] private BuildType buildType;
        public BuildType BuildType { get => buildType; set => buildType = value; }
        [SerializeField] private ImageType imageType;
        public ImageType ImageType { get => imageType; set => imageType = value; }
    }
}