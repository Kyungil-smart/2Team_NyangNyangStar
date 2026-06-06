using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

// ──────────────────────────────────────────────────────────────
// 맵 기능 예시 SO (배열 기능 추가 이후 기준)
//
//  1) 고정 맵        : struct 필드 (어트리뷰트 불필요)
//  2) 중첩 맵        : struct 안 struct
//  3) 동적 맵        : [FirestoreMap] + List<T>  ← 마커 필수!
//  4) 맵 안의 맵     : 동적 맵 값 안에 또 동적 맵
//  5) 혼합           : 동적 맵 값 안에 고정 struct
//
//  ⚠️ [FirestoreMap] 없는 List<T>는 이제 'array'로 저장된다 (맵 아님).
//     배열 예시는 ArrayTestSimpleSO / ArrayTestFullSO 참고.
//
// 경로: Users/{userId}/Test/{docId}
// ──────────────────────────────────────────────────────────────
namespace FireStoreTest
{
    [FirestorePath("Users/{userId}/Test/{docId}")]
    [CreateAssetMenu(fileName = "MapTestSO", menuName = "ScriptableObjects/MapTestSO")]
    public class MapTestSO : BaseFireStore
    {
        // 1) 고정 맵 — Combat: { Atk, Def, CritRate }
        //[SerializeField] private MapCombat combat;

        // 2) 중첩 맵 — Stats: { Hp, Combat: { ... } }
        //[SerializeField] private MapStats stats;

        // 3) 동적 맵 — Items: { "sword_01": { Count, Level }, ... }
        //[FirestoreMap]
        //[SerializeField] private List<MapItemEntry> items = new List<MapItemEntry>();

        // 4) 맵 안의 맵 — Chapters: { "chapter_1": { UnlockLevel, Stages: { "1-1": { Score, Stars } } } }
        //[FirestoreMap]
        //[SerializeField] private List<MapChapter> chapters = new List<MapChapter>();

        // 5) 동적 맵 값 안에 고정 struct — Units: { "cat_001": { Level, Stats: {...} } }
        [FirestoreMap]
        [SerializeField] private List<MapUnitEntry> units = new List<MapUnitEntry>();


        // 신규 생성 시 기본값 (struct는 필드 초기화 불가 → 여기서 세팅)
        public override async Task CreateNew(FirebaseFirestore database, string userId)
        {
            //combat = new MapCombat { atk = 50, def = 20, critRate = 0.15f };

            //stats = new MapStats { hp = 120, combat = new MapCombat { atk = 30, def = 15, critRate = 0.25f } };

            //items = new List<MapItemEntry>
            //{
            //    new MapItemEntry { itemId = "sword_01", count = 3, level = 2 },
            //    new MapItemEntry { itemId = "potion",   count = 10, level = 0 },
            //};

            //chapters = new List<MapChapter>
            //{
            //    new MapChapter
            //    {
            //        chapterId = "chapter_1", unlockLevel = 1,
            //        stages = new List<MapStageRecord>
            //        {
            //            new MapStageRecord { stageId = "1-1", score = 9800, stars = 3 },
            //            new MapStageRecord { stageId = "1-2", score = 7200, stars = 2 },
            //        }
            //    },
            //    new MapChapter
            //    {
            //        chapterId = "chapter_2", unlockLevel = 10,
            //        stages = new List<MapStageRecord> { new MapStageRecord { stageId = "2-1", score = 5000, stars = 1 } }
            //    },
            //};

            units = new List<MapUnitEntry>
            {
                new MapUnitEntry { unitId = "cat_001", level = 3, stats = new MapStats { hp = 120, combat = new MapCombat { atk = 30, def = 15, critRate = 0.1f } } },
                new MapUnitEntry { unitId = "cat_002", level = 1, stats = new MapStats { hp = 90,  combat = new MapCombat { atk = 45, def = 8,  critRate = 0.3f } } },
            };

            await base.CreateNew(database, userId);
        }

        // ── 런타임 동적 맵 조작 예시 (chapters) ──────────────────────
        // 플레이 중(FireStoreManager.Init 완료 후) 인스펙터 톱니바퀴 메뉴로 실행.
        // 리스트를 고치고 저장하면 맵에 반영된다.

        // 새 챕터 추가 → 저장 시 Chapters 맵에 새 키 생김
        //[ContextMenu("Test/1. Add Chapter")]
        //private void AddChapter()
        //{
        //    chapters.Add(new MapChapter
        //    {
        //        chapterId = $"chapter_{chapters.Count + 1}",
        //        unlockLevel = 20,
        //        stages = new List<MapStageRecord>()
        //    });
        //    Debug.Log($"[MapTestSO] chapters.Count = {chapters.Count}");
        //}

        //// chapter_1 안에 새 스테이지 기록 추가 → 중첩 맵(Stages)에 새 키 생김
        //[ContextMenu("Test/2. Add Stage to chapter_1")]
        //private void AddStage()
        //{
        //    int ci = chapters.FindIndex(c => c.chapterId == "chapter_1");
        //    if (ci < 0) { Debug.LogWarning("[MapTestSO] chapter_1 없음"); return; }

        //    var stages = chapters[ci].stages;   // List는 참조 타입 — struct가 복사돼도 같은 리스트를 가리킴
        //    stages.Add(new MapStageRecord { stageId = $"1-{stages.Count + 1}", score = 0, stars = 0 });
        //    Debug.Log($"[MapTestSO] chapter_1 stages = {stages.Count}");
        //}

        //// 기존 스테이지 값 수정 — struct 원소는 복사 → 수정 → 되돌려 넣기
        //[ContextMenu("Test/3. Update Stage 1-1 (+100점, +1별)")]
        //private void UpdateStageScore()
        //{
        //    int ci = chapters.FindIndex(c => c.chapterId == "chapter_1");
        //    if (ci < 0) { Debug.LogWarning("[MapTestSO] chapter_1 없음"); return; }

        //    var stages = chapters[ci].stages;
        //    int si = stages.FindIndex(s => s.stageId == "1-1");
        //    if (si < 0) { Debug.LogWarning("[MapTestSO] 스테이지 1-1 없음"); return; }

        //    var st = stages[si];                // struct 복사
        //    st.score += 100;
        //    st.stars = Mathf.Min(st.stars + 1, 3);
        //    stages[si] = st;                    // 되돌려 넣기 (이거 없으면 원본 안 바뀜)
        //    Debug.Log($"[MapTestSO] 1-1 → score {st.score}, stars {st.stars}");
        //}

        //// 현재 리스트 상태를 Firestore 맵으로 저장
        //[ContextMenu("Test/4. Save (UpdateDataAsync)")]
        //private async void SaveToServer()
        //{
        //    await UpdateDataAsync();
        //    Debug.Log("[MapTestSO] 저장 완료 — Firestore 콘솔에서 Chapters 맵 확인");
        //}
    }


    // ── 고정 맵용 struct ───────────────────────────────────────────
    [System.Serializable]
    public struct MapStats
    {
        public int hp;
        public MapCombat combat;     // 중첩 struct → 맵 안의 맵
    }

    [System.Serializable]
    public struct MapCombat
    {
        public int atk;
        public int def;
        public float critRate;
    }


    // ── 동적 맵 원소들 (원소당 [FirestoreMapKey] 정확히 1개) ─────────
    //[System.Serializable]
    //public struct MapItemEntry
    //{
    //    [FirestoreMapKey] public string itemId;   // 이 값이 맵 키
    //    public int count;
    //    public int level;
    //}

    // 챕터-스테이지 예시: 챕터(chapterId가 맵 키) 안에 스테이지 기록 맵이 중첩됨
    //[System.Serializable]
    //public struct MapChapter
    //{
    //    [FirestoreMapKey] public string chapterId;
    //    public int unlockLevel;
    //    [FirestoreMap] public List<MapStageRecord> stages;   // 값 안에 또 동적 맵 (마커 필요)
    //}

    //[System.Serializable]
    //public struct MapStageRecord
    //{
    //    [FirestoreMapKey] public string stageId;
    //    public int score;
    //    public int stars;
    //}

    [System.Serializable]
    public struct MapUnitEntry
    {
        [FirestoreMapKey] public string unitId;
        public int level;
        public MapStats stats;                       // 동적 맵 값 안에 고정 struct
    }
}
