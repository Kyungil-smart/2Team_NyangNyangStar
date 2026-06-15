using System;
using System.Collections;
using Data.Parsing;
using Data.ScriptableObjects;
using Data.ScriptableObjects.HideAndSeekSO;
using UnityEngine;

namespace UI.FindMoongchi
{
    public class FindMoongchiDataManager : MonoBehaviour
    {
        [Header("상점/보상 테이블")]
        [SerializeField] private SheetData _shopSheet;
        [SerializeField] private HideAndSeekShopSO _shopSO;

        [Header("숨바꼭질 미션 테이블")]
        [SerializeField] private SheetData _missionSheet;
        [SerializeField] private HideAndSeekMissionSO _missionSO;

        [Header("프로필 테이블")]
        [SerializeField] private SheetData _profileSheet;
        [SerializeField] private HideAndSeekProfileSO _profileSO;

        [Header("테스트 옵션")]
        [SerializeField] private bool _loadOnStart = true;

        public HideAndSeekShopSO ShopSO => _shopSO;
        public HideAndSeekMissionSO MissionSO => _missionSO;
        public HideAndSeekProfileSO ProfileSO => _profileSO;

        public bool IsLoaded { get; private set; }

        public event Action OnLoadCompleted;

        private void Start()
        {
            if (_loadOnStart)
                LoadAll();
        }

        [ContextMenu("FindMoongchi 시트 로드")]
        public void LoadAll()
        {
            StopAllCoroutines();
            StartCoroutine(LoadAllCoroutine());
        }

        private IEnumerator LoadAllCoroutine()
        {
            IsLoaded = false;

            DebugTool.Log("[FindMoongchiDataManager] 시트 로드 시작", DebugType.Data, this);

            yield return LoadSheet(_shopSheet, _shopSO, 2, "상점/보상");
            yield return LoadSheet(_missionSheet, _missionSO, 2, "숨바꼭질 미션");
            yield return LoadSheet(_profileSheet, _profileSO, 2, "프로필");

            _shopSO?.PrintData();
            _missionSO?.PrintData();
            _profileSO?.PrintData();

            IsLoaded = true;
            OnLoadCompleted?.Invoke();

            DebugTool.Log("[FindMoongchiDataManager] 모든 시트 로드 완료", DebugType.Data, this);
        }

        private IEnumerator LoadSheet<T>(
            SheetData sheet,
            T targetSO,
            int headerRowCount,
            string sheetName)
            where T : SoBase, ISheetParsable
        {
            if (targetSO == null)
            {
                DebugTool.Error($"[FindMoongchiDataManager] {sheetName} SO가 할당되지 않았습니다.", DebugType.Data, this);
                yield break;
            }

            targetSO.Init();

            if (string.IsNullOrEmpty(sheet.URL))
            {
                DebugTool.Warning($"[FindMoongchiDataManager] {sheetName} URL이 비어있습니다.", DebugType.Data, this);
                yield break;
            }

            yield return sheet.Load((split, lines) =>
            {
                if (lines == null)
                {
                    DebugTool.Error($"[FindMoongchiDataManager] {sheetName} 시트 로드 실패", DebugType.Data, this);
                    return;
                }

                for (int i = headerRowCount; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] cols = line.Split(split);

                    if (cols.Length == 0)
                        continue;

                    targetSO.SetData(cols);
                }

                DebugTool.Log($"[FindMoongchiDataManager] {sheetName} 시트 로드 완료", DebugType.Data, this);
            });
        }
    }
}