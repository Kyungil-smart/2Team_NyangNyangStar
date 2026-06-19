using System.Collections.Generic;
using System.Threading.Tasks; // Task를 사용하기 위해 추가
using Firebase.Firestore;
using UnityEngine;

public abstract class BaseFireStore : ScriptableObject
{
    [SerializeField] private string m_DocumentId;
    public string DocumentId => m_DocumentId;

    // 매니저에서 딕셔너리 매핑용으로 쓸 식별자
    [SerializeField] private DataType m_EnumType;
    public DataType EnumType => m_EnumType;

    // 서브컬렉션 SO 목록 (인스펙터에서 추가). 모든 SO가 자동으로 갖는다.
    // ※ 필드명은 기존 파생 클래스 선언과 동일한 'subCollections'를 유지해야
    //    기존 .asset의 직렬화 데이터가 이름 매칭으로 그대로 이어진다.
    [SerializeField] private List<BaseFireStore> subCollections = new List<BaseFireStore>();

    // 베이스 리스트 + 파생 클래스가 직접 선언한 서브컬렉션 필드(리플렉션)를 합쳐 중복 없이 순회.
    // (FirestoreMapper의 필드 열거는 BaseFireStore 자신의 필드를 보지 않으므로 베이스 리스트는 직접 순회해야 한다.)
    private IEnumerable<BaseFireStore> AllSubCollections()
    {
        var seen = new HashSet<BaseFireStore>();
        foreach (var s in subCollections)
            if (s != null && seen.Add(s)) yield return s;
        foreach (var s in FirestoreMapper.GetSubCollections(this))
            if (s != null && seen.Add(s)) yield return s;
    }


    protected FirebaseFirestore db;
    protected string m_UserId;




    protected virtual DocumentReference GetDocumentRef()
    {
        var attr = (FirestorePathAttribute)System.Attribute
            .GetCustomAttribute(GetType(), typeof(FirestorePathAttribute));
        string template = attr != null ? attr.Template : null;
        return FirestoreMapper.ResolveDocument(db, template, m_UserId, DocumentId);
    }


    public virtual Dictionary<string, object> ToFirestoreDictionary()
        => FirestoreMapper.ToDictionary(this);


    public virtual void ApplyFromSnapshot(DocumentSnapshot snapshot)
        => FirestoreMapper.ApplyFromSnapshot(this, snapshot);


    public virtual Task CreateNew(FirebaseFirestore database)
        => CreateNew(database, m_UserId);

    public virtual async Task CreateNew(FirebaseFirestore database, string userId)
    {
        db = database;
        m_UserId = userId;

        await SetDataAsync(ToFirestoreDictionary());

        foreach (BaseFireStore sub in AllSubCollections())
        {
            await sub.CreateNew(database, userId);
        }
    }

    public virtual void InitDataBase(FirebaseFirestore database)
    {
        this.db = database;
    }

    public virtual void InitDataBase(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.m_UserId = userId;

        foreach (var sub in AllSubCollections())
            sub.InitDataBase(database, userId);
    }

    public virtual Task SetDataAsync(object data)
        => GetDocumentRef().SetAsync(data);

    public virtual Task UpdateDataAsync()
        => GetDocumentRef().UpdateAsync(ToFirestoreDictionary());

    public virtual async Task<DocumentSnapshot> UpdateFromServerAsync(bool updateAll)
    {
        DocumentSnapshot snap = await GetDocumentRef().GetSnapshotAsync();
        ApplyFromSnapshot(snap);

        if (updateAll)
            foreach (var sub in AllSubCollections())
                await sub.UpdateFromServerAsync(true);

        return snap;
    }

    public virtual Task<DocumentSnapshot> UpdateFromServerAsync<T>()
        => UpdateFromServerAsync(false);

    public virtual Task DeleteDataAsync()
        => GetDocumentRef().DeleteAsync();


    public virtual async Task SaveAllToServerAsync()
    {
        await UpdateDataAsync();
        foreach (var sub in AllSubCollections())
            await sub.SaveAllToServerAsync();
    }

#if UNITY_EDITOR
    // 에디터에서 인스펙터 편집 시 동적 맵의 중복 키를 미리 경고한다.
    protected virtual void OnValidate()
    {
        FirestoreMapper.ValidateMapKeys(this);
    }
#endif
}
