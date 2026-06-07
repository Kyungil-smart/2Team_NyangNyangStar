using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// 배열 기능 예시 (간단 버전) — 순수 배열만 다룬다
//
//  케이스 1) 스칼라 배열        : string[] / float[]
//  케이스 2) List + 런타임 추가 : List<int>
//  케이스 3) 빈/null 리스트     : [] 로 저장되는지 확인
//
//  ※ 한 케이스만 보려면: 다른 케이스의 [필드] + [CreateNew 안 블록]을 주석 처리
//  ※ struct 배열·맵 혼합 등 나머지 케이스는 ArrayTestFullSO 참고
// 경로: Users/{userId}/Test/{docId}   (docId는 인스펙터에서 입력)
// ──────────────────────────────────────────────────────────────
namespace FireStoreTest
{
    [FirestorePath("Users/{userId}/Test/{docId}")]
    [CreateAssetMenu(fileName = "ArrayTestSimpleSO", menuName = "ScriptableObjects/Test/ArrayTestSimpleSO")]
    public class ArrayTestSimpleSO : BaseFireStore
    {
        // ── 케이스 1) 스칼라 배열 ──────────────────────────────
        //    결과: Tags: ["starter","test","array"] / Multipliers: [1, 1.5, 2]
        [SerializeField] public string[] tags = new string[3];
        [SerializeField] public float[] multipliers = { 1f, 1.5f, 2f };

        // ── 케이스 2) List<int> + 런타임 추가 ──────────────────
        //    결과: RewardIds: [101, 102, 205]
        [SerializeField] private List<int> rewardIds = new List<int>();

        // ── 케이스 3) null 리스트 ─────────────────────────────
        //    결과: EmptyCheck: []   (null이어도 빈 배열로 저장됨)
        [SerializeField] private List<string> emptyCheck;


        // 신규 생성 시 기본값
        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            // 케이스 1
            tags = new[] { "starter", "test", "array" };
            multipliers = new[] { 1f, 1.5f, 2f };

            // 케이스 2
            rewardIds = new List<int> { 101, 102, 205 };

            // 케이스 3 — emptyCheck 는 일부러 안 채움 → [] 기대

            await base.CreateNew(database, userId);
        }


        // ── 케이스 2 런타임: 원소 추가 후 저장 ─────────────────
        //    실행 후 결과: RewardIds: [101, 102, 205, 1000]
        [ContextMenu("Add Reward (runtime)")]
        private async void AddReward()
        {
            rewardIds.Add(1000);
            await UpdateDataAsync();
            Debug.Log($"[ArrayTestSimpleSO] rewardIds.Count = {rewardIds.Count} — 저장 완료");
        }
    }
}
