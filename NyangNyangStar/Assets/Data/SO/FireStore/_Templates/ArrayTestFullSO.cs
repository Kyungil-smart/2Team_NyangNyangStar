using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// 배열 기능 예시 (전체 버전) — 순수 배열 외 나머지 케이스
//
//  케이스 1) struct 배열            : array of maps (순서 보존)
//  케이스 2) struct로 감싼 중첩 배열 : 배열 안 배열 금지의 우회법
//  케이스 3) 배열 + 동적 맵 혼합     : 배열 원소 struct 안에 [FirestoreMap]
//  케이스 4) 배열 안 배열 (실수)     : 경고 + 빈 배열 확인용
//
//  ※ 한 케이스만 보려면: 다른 케이스의 [필드] + [CreateNew 안 블록]을 주석 처리
//  ※ 순수 배열(스칼라·런타임 추가·빈 배열)은 ArrayTestSimpleSO 참고
// 경로: Users/{userId}/Test/{docId}   (docId는 인스펙터에서 입력)
// ──────────────────────────────────────────────────────────────
namespace FireStoreTest
{
    [FirestorePath("Users/{userId}/Test/{docId}")]
    [CreateAssetMenu(fileName = "ArrayTestFullSO", menuName = "ScriptableObjects/Test/ArrayTestFullSO")]
    public class ArrayTestFullSO : BaseFireStore
    {
        // ── 케이스 1) struct 배열 ─────────────────────────────
        //    결과: Logs: [ { Action: "attack", Damage: 30 }, { Action: "skill", Damage: 85 } ]
        [SerializeField] private List<ArrLog> logs = new List<ArrLog>();

        // ── 케이스 2) struct로 감싼 중첩 배열 ──────────────────
        //    결과: Waves: [ { EnemyIds: [1, 2] }, { EnemyIds: [3] } ]
        [SerializeField] private List<ArrWave> waves = new List<ArrWave>();

        // ── 케이스 3) 배열 + 동적 맵 혼합 ──────────────────────
        //    결과: Stages: [ { StageNo: 1, Drops: { "wood": { Rate: 0.5 }, "iron": { Rate: 0.1 } } }, ... ]
        [SerializeField] private List<ArrStage> stages = new List<ArrStage>();

        // ── 케이스 4) 배열 안 배열 (실수) ──────────────────────
        //    인스펙터에는 안 보이지만 매퍼는 인식함.
        //    저장 시 경고 1회 + 원소 스킵 → BadNested: [] 가 정상.
        public List<List<int>> badNested;


        // 신규 생성 시 기본값
        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            // 케이스 1
            logs = new List<ArrLog>
            {
                new ArrLog { action = "attack", damage = 30 },
                new ArrLog { action = "skill",  damage = 85 },
            };

            // 케이스 2
            waves = new List<ArrWave>
            {
                new ArrWave { enemyIds = new List<int> { 1, 2 } },
                new ArrWave { enemyIds = new List<int> { 3 } },
            };

            // 케이스 3
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

            // 케이스 4 — 저장 시 콘솔에 "배열 안 배열" 경고가 떠야 정상
            badNested = new List<List<int>> { new List<int> { 9, 9, 9 } };

            await base.CreateNew(database, userId);
        }


        // ── 케이스 1 런타임: 원소 추가 후 저장 (순서 보존 확인) ─
        //    실행 후 결과: Logs 맨 뒤에 { Action: "merge", ... } 추가됨
        [ContextMenu("Add Log (runtime)")]
        private async void AddLog()
        {
            logs.Add(new ArrLog { action = "merge", damage = Random.Range(0, 100) });
            await UpdateDataAsync();
            Debug.Log($"[ArrayTestFullSO] logs.Count = {logs.Count} — 저장 완료");
        }
    }


    // ── 배열 원소 타입들 ───────────────────────────────────────
    // 케이스 1: 일반 struct → 원소가 map이 되어 array of maps
    [System.Serializable]
    public struct ArrLog
    {
        public string action;
        public int damage;
    }

    // 케이스 2: 내부 배열을 struct로 한 겹 감싸면 합법 (배열 안 배열 우회)
    [System.Serializable]
    public struct ArrWave
    {
        public List<int> enemyIds;
    }

    // 케이스 3: 배열 원소 struct 안에 동적 맵 ([FirestoreMap] + [FirestoreMapKey])
    [System.Serializable]
    public struct ArrStage
    {
        public int stageNo;
        [FirestoreMap] public List<ArrDrop> drops;
    }

    [System.Serializable]
    public struct ArrDrop
    {
        [FirestoreMapKey] public string itemId;   // 동적 맵 키
        public float rate;
    }
}
