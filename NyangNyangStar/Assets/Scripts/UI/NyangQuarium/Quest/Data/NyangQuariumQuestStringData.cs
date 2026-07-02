using System;
using UnityEngine;

namespace Data.ScriptableObjects.NyangQuariumSO
{
    // 냥쿠_퀘스트 스트링 테이블의 한 줄(row)을 담는 데이터 클래스입니다.
    // 실제 시트 컬럼:
    // id, kor
    [Serializable]
    public class NyangQuariumQuestStringData
    {
        // 스트링 키입니다. 예: Q_NAME5, Q_DESC5
        [SerializeField] private string _id;

        // 한국어 출력 문구입니다. 예: 어항 구입
        [SerializeField] private string _kor;

        public string ID => _id;
        public string Kor => _kor;

        // 시트에서 파싱한 값을 한 번에 넣기 위한 생성자입니다.
        public NyangQuariumQuestStringData(string id, string kor)
        {
            _id = id;
            _kor = kor;
        }
    }
}