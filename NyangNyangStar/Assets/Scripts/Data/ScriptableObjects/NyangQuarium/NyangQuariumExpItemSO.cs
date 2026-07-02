using Data.ScriptableObjects;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangQuariumExpItem", menuName = "SO/NyangQuarium/NyangQuariumExpItemSO", order = 2)]
public class NyangQuariumExpItemSO : SoBase, ISheetParsable
{
    // 시트에서 필요한 최소 컬럼 수
    // ID, Item_Name, Item_Level, EXP, AddressableKey
    private const int RequiredColumnCount = 5;

    [Header("경험치 아이템 데이터")]
    [Tooltip("ID, Item_Name, Item_Level, EXP, AddressableKey")]
    [SerializeField] private List<NyangQuariumExpItemData> _expItems = new();

    // ID 빠른 조회용 딕셔너리
    // 인스펙터 확인 필요 없음
    private readonly Dictionary<int, NyangQuariumExpItemData> _expItemDict = new();

    // 외부 읽기 전용 경험치 아이템 목록
    public IReadOnlyList<NyangQuariumExpItemData> ExpItems => _expItems;

    // 현재 로드된 데이터 개수
    public int DataCount => _expItems?.Count ?? 0;

    // SO 초기화 시 데이터 정리
    public override void Init() => ClearData();

    // 리스트와 딕셔너리 초기화
    // 새로 로드할 때 데이터 중복 방지
    public void ClearData()
    {
        _expItems.Clear();
        _expItemDict.Clear();
    }

    // 시트 한 줄 데이터 변환
    // 컬럼 순서 : ID, Item_Name, Item_Level, EXP, AddressableKey
    public void SetData(string[] cols)
    {
        // 컬럼 수 부족 시 데이터 생성 불가
        if (cols == null || cols.Length < RequiredColumnCount)
        {
            DebugTool.Warning(
                $"[NyangQuariumExpItemSO] 컬럼 수가 부족합니다. 필요: {RequiredColumnCount}, 실제: {cols?.Length ?? 0}",
                DebugType.Data,
                this);
            return;
        }

        // 0번 컬럼은 아이템 ID
        // ID는 데이터 구분값
        // 0 이하 값 사용 안 함
        if (!TryParseInt(cols[0], out int itemId) || itemId <= 0)
        {
            DebugTool.Warning($"[NyangQuariumExpItemSO] 잘못된 ID: {cols[0]}", DebugType.Data, this);
            return;
        }

        // 2번 컬럼은 아이템 레벨
        // 3번 컬럼은 지급 경험치
        if (!TryParseInt(cols[2], out int itemLevel) ||
            !TryParseInt(cols[3], out int expValue))
        {
            DebugTool.Warning($"[NyangQuariumExpItemSO] 숫자 파싱 실패 / ID:{itemId}", DebugType.Data, this);
            return;
        }

        // 경험치 아이템 데이터 생성
        NyangQuariumExpItemData data = new(
            itemId,
            cols[1].Trim(),
            itemLevel,
            expValue,
            cols[4].Trim());

        // 전체 데이터 리스트 추가
        _expItems.Add(data);

        // ID 조회용 딕셔너리 추가
        // 같은 ID가 들어오면 나중 데이터로 덮어쓰기
        _expItemDict[itemId] = data;
    }

    // 아이템 레벨 기준 오름차순 정렬
    // Lv1, Lv2, Lv3 순서
    public void SortData()
    {
        _expItems.Sort((a, b) => a.ItemLevel.CompareTo(b.ItemLevel));
    }

    // 아이템 ID 기준 데이터 조회
    // 데이터 있으면 true
    // 데이터 없으면 false
    public bool TryGetById(int itemId, out NyangQuariumExpItemData expItemData)
    {
        // 리스트와 딕셔너리 상태 체크
        // 필요 시 딕셔너리 재생성
        RebuildDictionaryIfNeeded();

        return _expItemDict.TryGetValue(itemId, out expItemData);
    }

    // 로드된 경험치 아이템 데이터 로그 출력
    // 데이터 확인용
    public void PrintData()
    {
        StringBuilder log = new();
        log.AppendLine("[NyangQuariumExpItemSO 로드 완료]");

        foreach (NyangQuariumExpItemData expItemData in _expItems)
        {
            log.AppendLine(
                $"아이템 ID: {expItemData.ItemId}, " +
                $"이름: {expItemData.ItemName}, " +
                $"레벨: {expItemData.ItemLevel}, " +
                $"경험치: {expItemData.ExpValue}, " +
                $"AddressableKey: {expItemData.AddressableKey}");
        }

        DebugTool.Log(log.ToString(), DebugType.Data);
    }

    // 리스트 기준 딕셔너리 재생성
    // 딕셔너리 비어있는 경우 대비
    private void RebuildDictionaryIfNeeded()
    {
        // 개수가 같으면 재생성 생략
        if (_expItemDict.Count == _expItems.Count)
            return;

        _expItemDict.Clear();

        for (int i = 0; i < _expItems.Count; i++)
        {
            NyangQuariumExpItemData data = _expItems[i];

            // null 데이터 제외
            // 잘못된 ID 제외
            if (data == null || data.ItemId <= 0)
                continue;

            // ItemId를 key로 데이터 저장
            _expItemDict[data.ItemId] = data;
        }
    }

    // 문자열 int 변환
    // 앞뒤 공백 제거 후 숫자 변환
    private static bool TryParseInt(string value, out int result)
    {
        return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}