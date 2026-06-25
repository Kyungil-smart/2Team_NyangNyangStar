using System.Collections.Generic;
using System.Text;
using Data.ScriptableObjects;
using UnityEngine;

namespace Data.ScriptableObjects.NyangQuariumSO
{
    // 냥쿠_퀘스트 스트링 테이블 전체를 담는 ScriptableObject입니다.
    // 퀘스트 테이블의 questName, questDesc 값이 이 SO의 id를 참조합니다.
    [CreateAssetMenu(fileName = "NyangQuariumQuestStringSO", menuName = "SO/NyangQuarium/NyangQuariumQuestStringSO", order = 3)]
    public class NyangQuariumQuestStringSO : SoBase, ISheetParsable
    {
        // 시트에서 읽어온 스트링 목록입니다.
        [Header("냥쿠아리움 퀘스트 스트링 테이블")]
        [SerializeField] private List<NyangQuariumQuestStringData> _strings = new();

        // 스트링 키로 실제 문구를 빠르게 찾기 위한 캐시 딕셔너리입니다.
        private readonly Dictionary<string, string> _stringById = new();

        // 외부에서는 읽기 전용으로만 접근합니다.
        public IReadOnlyList<NyangQuariumQuestStringData> Strings => _strings;

        // SO 활성화 시 조회 캐시를 다시 구성합니다.
        private void OnEnable()
        {
            RebuildDictionary();
        }

        // 시트 로드 전 기존 데이터를 초기화합니다.
        public override void Init()
        {
            ClearData();
        }

        // 스트링 목록과 캐시를 모두 비웁니다.
        public void ClearData()
        {
            _strings.Clear();
            _stringById.Clear();
        }

        // 시트 한 줄을 받아서 NyangQuariumQuestStringData로 변환합니다.
        public void SetData(string[] cols)
        {
            // 스트링 테이블은 최소 2개 컬럼이 필요합니다.
            if (cols == null || cols.Length < 2)
                return;

            // 0번 컬럼: id
            string id = GetColumn(cols, 0);

            // 1번 컬럼: kor
            string kor = GetColumn(cols, 1);

            // 키 또는 문구가 비어 있으면 유효하지 않은 행으로 보고 무시합니다.
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(kor))
                return;

            // 파싱한 값으로 스트링 데이터 객체를 생성합니다.
            NyangQuariumQuestStringData data = new NyangQuariumQuestStringData(id, kor);

            // 같은 키가 있으면 교체하고, 없으면 새로 추가합니다.
            AddOrUpdate(data);
        }

        // 스트링 키로 실제 문구를 조회합니다.
        public bool TryGetString(string id, out string value)
        {
            RebuildDictionaryIfNeeded();

            if (string.IsNullOrWhiteSpace(id))
            {
                value = string.Empty;
                return false;
            }

            return _stringById.TryGetValue(id, out value);
        }

        // 스트링 키로 실제 문구를 조회합니다.
        // 없으면 키 자체를 반환해서 UI에서 누락 여부를 확인하기 쉽게 합니다.
        public string GetStringOrKey(string id)
        {
            return TryGetString(id, out string value) ? value : id;
        }

        // 시트 로드 후 디버그 로그로 데이터가 잘 들어왔는지 확인할 때 사용합니다.
        public void PrintData()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"[NyangQuariumQuestStringSO] 로드된 스트링 데이터: {_strings.Count}");

            for (int i = 0; i < _strings.Count; i++)
            {
                NyangQuariumQuestStringData data = _strings[i];

                if (data == null)
                    continue;

                builder.AppendLine($"ID:{data.ID}, KOR:{data.Kor}");
            }

            DebugTool.Log(builder.ToString(), DebugType.Data, this);
        }

        // 같은 ID가 이미 있으면 기존 데이터를 교체하고,
        // 없으면 리스트와 딕셔너리에 새로 추가합니다.
        private void AddOrUpdate(NyangQuariumQuestStringData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ID))
                return;

            RebuildDictionaryIfNeeded();

            if (_stringById.ContainsKey(data.ID))
            {
                for (int i = 0; i < _strings.Count; i++)
                {
                    if (_strings[i] == null || _strings[i].ID != data.ID)
                        continue;

                    _strings[i] = data;
                    _stringById[data.ID] = data.Kor;
                    return;
                }
            }

            _strings.Add(data);
            _stringById[data.ID] = data.Kor;
        }

        // 리스트 개수와 딕셔너리 개수가 다르면 캐시가 깨진 것으로 보고 다시 만듭니다.
        private void RebuildDictionaryIfNeeded()
        {
            if (_stringById.Count == _strings.Count)
                return;

            RebuildDictionary();
        }

        // 스트링 리스트를 기준으로 ID 조회용 딕셔너리를 다시 만듭니다.
        private void RebuildDictionary()
        {
            _stringById.Clear();

            for (int i = 0; i < _strings.Count; i++)
            {
                NyangQuariumQuestStringData data = _strings[i];

                if (data == null || string.IsNullOrWhiteSpace(data.ID))
                    continue;

                _stringById[data.ID] = data.Kor;
            }
        }

        // 배열에서 특정 컬럼을 안전하게 가져옵니다.
        private static string GetColumn(string[] cols, int index)
        {
            if (cols == null || index < 0 || index >= cols.Length)
                return string.Empty;

            return cols[index]?.Trim() ?? string.Empty;
        }
    }
}