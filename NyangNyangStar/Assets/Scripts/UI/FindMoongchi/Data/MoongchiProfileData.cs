using System;
using UnityEngine;


namespace Data.ScriptableObjects.MoongchiSO
{
    // 프로필_데이터 
    // 이벤트 상점의 PROFILE 상품 productID, ProfileID 연결

    [Serializable]
    public class MoongchiProfileData
    {
        [SerializeField] private int _profileID;
        [SerializeField] private string _profileName;

        [SerializeField] private string _addressableKey;
        [SerializeField] private string _descStringID;
        [SerializeField] private bool _isDefault;

        public int ProfileID => _profileID;
        public string ProfileName => _profileName;
        public string AddressableKey => _addressableKey;
        public string DescStringID => _descStringID;
        public bool IsDefault => _isDefault;

        public MoongchiProfileData(
            int profileID,
            string profileName,
            string addressableKey,
            string descStringID,
            bool isDefault)
        {
            _profileID = profileID;
            _profileName = profileName;
            _addressableKey = addressableKey;
            _descStringID = descStringID;
            _isDefault = isDefault;
        }
    }
}