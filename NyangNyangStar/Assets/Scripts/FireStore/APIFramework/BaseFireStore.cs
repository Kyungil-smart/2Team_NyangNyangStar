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
        this.db = database;
        this.m_UserId = userId;

        foreach (var sub in FirestoreMapper.GetSubCollections(this))
            await sub.CreateNew(database, userId);

        await SetDataAsync(ToFirestoreDictionary());
    }

    public virtual void InitDataBase(FirebaseFirestore database)
    {
        this.db = database;
    }

    public virtual void InitDataBase(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.m_UserId = userId;

        foreach (var sub in FirestoreMapper.GetSubCollections(this))
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
            foreach (var sub in FirestoreMapper.GetSubCollections(this))
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
        foreach (var sub in FirestoreMapper.GetSubCollections(this))
            await sub.SaveAllToServerAsync();         
    }
}
