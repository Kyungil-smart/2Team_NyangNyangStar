using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public enum DataType
{
    None,
    Users
}
public class FireStoreManager : MonoBehaviour
{
    private FirebaseFirestore db;
    public static FireStoreManager Instance { get; private set; }
    [SerializeField] private List<BaseFireStore> m_Data;
    private Dictionary<DataType, BaseFireStore> m_DataDictionary;


    private void Awake()
    {
        InitSingleton();

    }

    public async void Init(string userId = "testUserId")
    {
        InitDictionary();
        await InitFirebase(userId);
    }

    private void InitSingleton()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
    private async Task InitFirebase(string userId)
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase 초기화 실패: {status}");
            return;
        }

        db = FirebaseFirestore.DefaultInstance;

        bool exists = await ExistsAsync(userId);
        if (exists)
        {
            Debug.Log($"UserID {userId} exists in Firestore.");
            BindClass(userId);          // db 바인딩(동기)
            await LoadClass();          // 기존 유저: 서버 데이터 로드(비동기, await로 완료 보장)
            Debug.Log("기존 유저 데이터 로드 완료");
        }
        else
        {
            Debug.Log($"UserID {userId} does NOT exist in Firestore.");
            await CreateNew(userId);    // 신규 유저: 생성 + db 바인딩 (서버 재읽기 없음)
            BindClass(userId);
        }

        Debug.Log("Firebase 초기화 성공");
    }

    private async Task CreateNew(string userId)
    {
        foreach (BaseFireStore item in m_Data)// m_Data 는 BaseFireStore를 상속하는 SO 스크립트 오브젝트
        {
            await item.CreateNew(db, userId);
        }
    }

    private void BindClass(string userId)
    {
        foreach (BaseFireStore item in m_Data)// m_Data 는 BaseFireStore를 상속하는 SO 스크립트 오브젝트
        {
            item.InitDataBase(db, userId);

        }
    }

    private async Task LoadClass()
    {
        foreach (BaseFireStore item in m_Data)// m_Data 는 BaseFireStore를 상속하는 SO 스크립트 오브젝트
        {
            await item.UpdateFromServerAsync(true);
        }
    }
    public async Task<bool> ExistsAsync(string userId)
    {
        // Users 컬렉션에서 UserID 필드 == uid 인 문서가 있는지 조회
        Query query = db.Collection("Users").WhereEqualTo("UserID", userId);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        bool exists = snapshot.Count > 0;
        Debug.Log($"[ExistsAsync] UserID={userId} 존재여부: {exists} (matched {snapshot.Count})");
        return exists;
    }

    private void InitDictionary()
    {
        m_DataDictionary = new Dictionary<DataType, BaseFireStore>();
        m_DataDictionary = m_Data.ToDictionary(x => x.EnumType, x => x);
    }

    public FirestoreRequestContext DocumentType(DataType type)
    {
        // 해당 데이터를 처리할 컨텍스트를 새로 생성해서 반환 (동시성 문제 해결)
        return new FirestoreRequestContext(m_DataDictionary[type]);
    }

    [ContextMenu("Download")]
    public async void sdsd()
    {
        await Test();
    }
    private async Task Test()
    {
        var data = await FireStoreManager.Instance.DocumentType(DataType.Users).GetAsync<UsersSO>();
        if (data.Exists)
        {
            Debug.Log(data.ToDictionary());
        }

    }

    [ContextMenu("Upload")]
    public async void sdsd2()
    {
        await Test2();
    }
    private async Task Test2()
    {
        //var data = new Dictionary<string, object>
        //{
        //    { "Name", "Maple" }

        //};

        //await FireStoreManager.Instance.DocumentType(DataType.Users).SetAsync(data);

        await FireStoreManager.Instance.DocumentType(DataType.Users).SaveAllToServerAsync();
    }
}