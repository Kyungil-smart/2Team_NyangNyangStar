using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// 배열 기능 검증 (간단 버전) — 스칼라 배열 왕복만 확인
//  - string[] / List<int> / float[] 저장 → Firestore array → 로드 복원
//  - 빈 배열·null 리스트는 [] 로 저장되는지 확인
// 경로: Users/{userId}/Test/{docId}   (docId는 인스펙터에서 입력)
// ──────────────────────────────────────────────────────────────
namespace FireStoreTest
{
    [FirestorePath("Users/{userId}/Test/{docId}")]
    [CreateAssetMenu(fileName = "ArrayTestSimpleSO", menuName = "ScriptableObjects/Test/ArrayTestSimpleSO")]
    public class ArrayTestSimpleSO : BaseFireStore
    {
        [SerializeField] public string[] tags = new string[3];
        [SerializeField] private List<int> rewardIds = new List<int>();
        [SerializeField] public float[] multipliers = { 1f, 1.5f, 2f };

        // null 상태로 두고 저장 → Firestore에 빈 배열 [] 로 나오는지 확인용
        [SerializeField] private List<string> emptyCheck;


        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            tags = new[] { "starter", "test", "array" };
            rewardIds = new List<int> { 101, 102, 205 };
            multipliers = new[] { 1f, 1.5f, 2f };
            // emptyCheck 는 일부러 안 채움 → [] 기대

            await base.CreateNew(database, userId);
        }

        // 런타임 변경 → 저장 시 반영되는지 확인용
        [ContextMenu("Add Random Reward")]
        private void AddRandomReward()
        {
            rewardIds.Add(1000);
            Debug.Log($"[ArrayTestSimpleSO] rewardIds.Count = {rewardIds.Count}");
            this.UpdateDataAsync();
        }

    }


}
