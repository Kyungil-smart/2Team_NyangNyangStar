using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.HideAndSeekSO
{
    /// <summary>
    /// 뭉치를 찾아라 프로필 보상 마스터 데이터입니다.
    /// 현재는 이벤트 한정 프로필 110001을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "HideAndSeekProfileSO", menuName = "SO/FindMoongchi/HideAndSeekProfileSO", order = 2)]
    public class HideAndSeekProfileSO : SoBase, ISheetParsable
    {
        [Header("뭉치를 찾아라 프로필 데이터")]
        [SerializeField] private List<HideAndSeekProfileData> _profiles = new();

        // ProfileID 기반 조회 캐시입니다.
        private readonly Dictionary<int, HideAndSeekProfileData> _profileByID = new();

        public IReadOnlyList<HideAndSeekProfileData> Profiles => _profiles;

        private void OnEnable()
        {
            RebuildDictionary();
        }

        public override void Init()
        {
            ClearData();
        }

        public void ClearData()
        {
            _profiles.Clear();
            _profileByID.Clear();
        }

        /// <summary>
        /// 시트 한 줄을 프로필 데이터로 변환합니다.
        /// IsDefault는 0/1 또는 true/false 모두 처리합니다.
        /// </summary>
        public void SetData(string[] cols)
        {
            if (cols == null || cols.Length < 5)
                return;

            if (!int.TryParse(GetColumn(cols, 0), out int profileID) || profileID <= 0)
                return;

            string profileName = GetColumn(cols, 1);
            string addressableKey = GetColumn(cols, 2);
            string descStringID = GetColumn(cols, 3);
            bool isDefault = ParseBool(GetColumn(cols, 4));

            if (string.IsNullOrEmpty(profileName) || string.IsNullOrEmpty(addressableKey))
                return;

            HideAndSeekProfileData data = new HideAndSeekProfileData(
                profileID,
                profileName,
                addressableKey,
                descStringID,
                isDefault);

            AddOrUpdate(data);
        }

        public bool TryGetProfile(int profileID, out HideAndSeekProfileData data)
        {
            RebuildDictionaryIfNeeded();
            return _profileByID.TryGetValue(profileID, out data);
        }

        public void PrintData()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[HideAndSeekProfileSO] 로드된 프로필 데이터: {_profiles.Count}");

            foreach (HideAndSeekProfileData data in _profiles)
            {
                builder.AppendLine(
                    $"ProfileID:{data.ProfileID}, Name:{data.ProfileName}, " +
                    $"Key:{data.AddressableKey}, Desc:{data.DescStringID}, Default:{data.IsDefault}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        private void AddOrUpdate(HideAndSeekProfileData data)
        {
            RebuildDictionaryIfNeeded();

            if (_profileByID.ContainsKey(data.ProfileID))
            {
                for (int i = 0; i < _profiles.Count; i++)
                {
                    if (_profiles[i].ProfileID != data.ProfileID)
                        continue;

                    _profiles[i] = data;
                    _profileByID[data.ProfileID] = data;
                    return;
                }
            }

            _profiles.Add(data);
            _profileByID[data.ProfileID] = data;
        }

        private void RebuildDictionaryIfNeeded()
        {
            if (_profileByID.Count == _profiles.Count)
                return;

            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            _profileByID.Clear();

            foreach (HideAndSeekProfileData data in _profiles)
            {
                if (data == null || data.ProfileID <= 0)
                    continue;

                _profileByID[data.ProfileID] = data;
            }
        }

        private static string GetColumn(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }

        private static bool ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (bool.TryParse(value, out bool result))
                return result;

            return value == "1";
        }
    }
}