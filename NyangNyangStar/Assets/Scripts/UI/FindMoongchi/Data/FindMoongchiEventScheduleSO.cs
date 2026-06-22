using UnityEngine;

namespace Data.ScriptableObjects.MoongchiSO
{
    // 뭉치를 찾아라 이벤트 전체 기간 설정. 테스트 시 인스펙터에서 날짜만 바꾸면 UI/판정에 반영됩니다.
    [CreateAssetMenu(fileName = "FindMoongchiEventScheduleSO", menuName = "SO/FindMoongchi/FindMoongchiEventScheduleSO", order = 4)]
    public class FindMoongchiEventScheduleSO : ScriptableObject
    {
        [Header("이벤트 시작일 (KST 00:00:00)")]
        [SerializeField] private int _startYear = 2026;
        [SerializeField] private int _startMonth = 6;
        [SerializeField] private int _startDay = 19;

        [Header("이벤트 종료일 (KST 23:59:59)")]
        [SerializeField] private int _endYear = 2026;
        [SerializeField] private int _endMonth = 7;
        [SerializeField] private int _endDay = 2;

        public int StartYear => _startYear;
        public int StartMonth => _startMonth;
        public int StartDay => _startDay;
        public int EndYear => _endYear;
        public int EndMonth => _endMonth;
        public int EndDay => _endDay;
    }
}
