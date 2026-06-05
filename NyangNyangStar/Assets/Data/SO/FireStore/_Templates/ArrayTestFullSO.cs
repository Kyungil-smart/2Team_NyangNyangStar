using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// 배열 기능 검증 (전체 버전)
//  S3: struct 배열            → array of maps
//  S5: 배열 원소 안 동적 맵    → array( map( map ) )
//  S4: 배열 안 배열(실수)     → 경고 + 원소 스킵, 우회(struct 감싸기)는 정상 동작
//  빈/널 컬렉션               → [] 저장
// 경로: Users/{userId}/Test/{docId}   (docId는 인스펙터에서 입력)
// ──────────────────────────────────────────────────────────────
namespace FireStoreTest
{
    [FirestorePath("Users/{userId}/Test/{docId}")]
    [CreateAssetMenu(fileName = "ArrayTestFullSO", menuName = "ScriptableObjects/Test/ArrayTestFullSO")]
    public class ArrayTestFullSO : BaseFireStore
    {
        // S3) struct 배열 → Logs: [ { Action, Damage }, ... ]  (순서 보존)
        [SerializeField] private List<ArrLog> logs = new List<ArrLog>();

        // S5) 배열 원소 struct 안에 동적 맵
        //     Stages: [ { StageNo: 1, Drops: { "wood": { Rate: 0.5 } } }, ... ]
        [SerializeField] private List<ArrStage> stages = new List<ArrStage>();

        // S4-우회) 배열 안 배열은 struct로 감싸면 합법
        //     Waves: [ { EnemyIds: [1, 2] }, { EnemyIds: [3] } ]
        [SerializeField] private List<ArrWave> waves = new List<ArrWave>();

        // S4-실수) 배열 안 배열 — Unity 인스펙터에는 안 보이지만 매퍼는 인식함.
        //     저장 시 경고 1회 + 원소 스킵 → BadNested: [] 가 되어야 정상.
        public List<List<int>> badNested;


        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            logs = new List<ArrLog>
            {
                new ArrLog { action = "attack", damage = 30 },
                new ArrLog { action = "skill",  damage = 85 },
            };

            stages = new List<ArrStage>
            {
                new ArrStage
                {
                    stageNo = 1,
                    drops = new List<ArrDrop>
                    {
                        new ArrDrop { itemId = "wood", rate = 0.5f },
                        new ArrDrop { itemId = "iron", rate = 0.1f },
                    }
                },
                new ArrStage
                {
                    stageNo = 2,
                    drops = new List<ArrDrop> { new ArrDrop { itemId = "seed", rate = 0.8f } }
                },
            };

            waves = new List<ArrWave>
            {
                new ArrWave { enemyIds = new List<int> { 1, 2 } },
                new ArrWave { enemyIds = new List<int> { 3 } },
            };

            // 실수 케이스: 저장 시 경고가 떠야 정상
            badNested = new List<List<int>>
            {
                new List<int> { 9, 9, 9 },
            };

            await base.CreateNew(database, userId);
        }

        [ContextMenu("Add Log (runtime)")]
        private void AddLog()
        {
            logs.Add(new ArrLog { action = "merge", damage = Random.Range(0, 100) });
            Debug.Log($"[ArrayTestFullSO] logs.Count = {logs.Count}");
            this.UpdateDataAsync();
        }
    }


    // ── 배열 원소 타입들 ───────────────────────────────────────────
    [System.Serializable]
    public struct ArrLog
    {
        public string action;
        public int damage;
    }

    [System.Serializable]
    public struct ArrDrop
    {
        [FirestoreMapKey] public string itemId;   // 동적 맵 키
        public float rate;
    }

    [System.Serializable]
    public struct ArrStage
    {
        public int stageNo;
        [FirestoreMap] public List<ArrDrop> drops;   // 배열 원소 안 동적 맵
    }

    [System.Serializable]
    public struct ArrWave
    {
        public List<int> enemyIds;   // struct로 감싼 내부 배열 → array(map(array)) 합법
    }
}
