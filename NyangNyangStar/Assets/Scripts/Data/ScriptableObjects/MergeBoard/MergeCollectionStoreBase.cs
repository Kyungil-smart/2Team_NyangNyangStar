using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;

namespace Data.ScriptableObjects.MergeBoard
{
    // 한 Firestore 서브컬렉션(문서 N개)을 관리하는 공용 베이스.
    // 직렬화는 FirestoreMapper에 위임하고, 컬렉션 read-all / batch-write 메커니즘만 제공한다.
    // 기존 DB 구조(문서 N개, "D2" 문서ID, 필드명)는 그대로 유지된다.
    public abstract class MergeCollectionStoreBase<TDoc> : BaseFireStore
    {
        protected const int BatchLimit = 450;

        // 이 SO가 담당하는 서브컬렉션 이름 (예: "MergeBoard").
        protected abstract string CollectionName { get; }

        public bool IsReady => db != null && !string.IsNullOrEmpty(m_UserId);

        protected CollectionReference Collection =>
            db.Collection("Users").Document(m_UserId).Collection(CollectionName);

        protected static string DocId(int number) => number.ToString("D2");

        protected static Dictionary<string, object> ToDict(TDoc doc) =>
            (Dictionary<string, object>)FirestoreMapper.SerializeValue(doc, typeof(TDoc), null);

        protected static TDoc FromDict(IDictionary<string, object> raw) =>
            (TDoc)FirestoreMapper.DeserializeValue(raw, typeof(TDoc), null);

        // 이 SO는 단일 문서가 아니라 컬렉션(문서 N개)을 관리하므로,
        // FSO 표준 단일문서 동기화는 쓰지 않는다. 로드/저장은 도메인 facade로 게임플레이가 직접 수행.
        // (서브컬렉션 재귀 로드/저장이 단일문서 경로(GetDocumentRef)를 타지 않도록 no-op 처리)
        public override Task<DocumentSnapshot> UpdateFromServerAsync(bool updateAll) =>
            Task.FromResult<DocumentSnapshot>(null);

        public override Task SaveAllToServerAsync() => Task.CompletedTask;

        // 컬렉션의 모든 문서를 읽어 TDoc 리스트로 돌려준다.
        protected async Task<List<TDoc>> ReadAllAsync()
        {
            var result = new List<TDoc>();

            QuerySnapshot snapshot = await Collection.GetSnapshotAsync();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (!document.Exists)
                    continue;

                result.Add(FromDict(document.ToDictionary()));
            }

            return result;
        }

        // (문서ID, 문서) 목록을 batch로 기록한다. (BatchLimit 단위로 커밋)
        protected async Task WriteManyAsync(IEnumerable<KeyValuePair<string, TDoc>> docs)
        {
            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (KeyValuePair<string, TDoc> pair in docs)
            {
                batch.Set(Collection.Document(pair.Key), ToDict(pair.Value));

                if (++operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }

        protected Task WriteOneAsync(string docId, TDoc doc) =>
            Collection.Document(docId).SetAsync(ToDict(doc));

        // 컬렉션의 모든 문서를 삭제한다.
        protected async Task ClearAsync()
        {
            QuerySnapshot snapshot = await Collection.GetSnapshotAsync();

            WriteBatch batch = db.StartBatch();
            int operationCount = 0;

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                batch.Delete(document.Reference);

                if (++operationCount >= BatchLimit)
                {
                    await batch.CommitAsync();
                    batch = db.StartBatch();
                    operationCount = 0;
                }
            }

            if (operationCount > 0)
                await batch.CommitAsync();
        }
    }
}
