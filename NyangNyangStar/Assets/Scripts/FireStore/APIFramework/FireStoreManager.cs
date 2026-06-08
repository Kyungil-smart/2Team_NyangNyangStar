using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public enum DataType
{
    None,
    Users,
    MergeBoard,
    ScratchingTime,
    MoongchiProgess,
}

public class FireStoreManager : MonoBehaviour
{
    private FirebaseFirestore db;
    public static FireStoreManager Instance { get; private set; }

    [SerializeField] private List<BaseFireStore> m_Data;
    private Dictionary<DataType, BaseFireStore> m_DataDictionary;

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        InitSingleton();
        DontDestroyOnLoad(gameObject);
    }

    private void InitSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    public async Task InitAsync(string userId = "testUserId")
    {
        IsInitialized = false;

        Debug.Log($"[FireStoreManager] InitAsync 시작 / userId: {userId}");

        if (m_Data == null || m_Data.Count == 0)
        {
            Debug.LogError("[FireStoreManager] m_Data가 비어 있습니다.");
            return;
        }

        for (int i = 0; i < m_Data.Count; i++)
        {
            if (m_Data[i] == null)
            {
                Debug.LogError($"[FireStoreManager] m_Data[{i}]가 null입니다.");
                return;
            }

            Debug.Log($"[FireStoreManager] 등록 데이터: {m_Data[i].name} / Type: {m_Data[i].EnumType}");
        }

        try
        {
            InitDictionary();
            await InitFirebase(userId);

            IsInitialized = true;
            Debug.Log("[FireStoreManager] InitAsync 완료");
        }
        catch (System.Exception e)
        {
            IsInitialized = false;
            Debug.LogError($"[FireStoreManager] InitAsync 실패: {e}");
        }
    }

    public async void Init(string userId = "testUserId")
    {
        await InitAsync(userId);
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
            BindClass(userId);
            await LoadClass();
            Debug.Log("기존 유저 데이터 로드 완료");
        }
        else
        {
            Debug.Log($"UserID {userId} does NOT exist in Firestore.");
            await CreateNew(userId);
            BindClass(userId);
        }

        Debug.Log("Firebase 초기화 성공");
    }

    private async Task CreateNew(string userId)
    {
        foreach (BaseFireStore item in m_Data)
        {
            Debug.Log($"[FireStoreManager] CreateNew 시작: {item.name} / {item.EnumType}");
            await item.CreateNew(db, userId);
            Debug.Log($"[FireStoreManager] CreateNew 완료: {item.name} / {item.EnumType}");
        }
    }
    private void BindClass(string userId)
    {
        foreach (BaseFireStore item in m_Data)
            item.InitDataBase(db, userId);
    }

    private async Task LoadClass()
    {
        foreach (BaseFireStore item in m_Data)
            await item.UpdateFromServerAsync(true);
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        Query query = db.Collection("Users").WhereEqualTo("UserID", userId);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        bool exists = snapshot.Count > 0;
        Debug.Log($"[ExistsAsync] UserID={userId} 존재여부: {exists} (matched {snapshot.Count})");
        return exists;
    }

    private void InitDictionary()
    {
        m_DataDictionary = new Dictionary<DataType, BaseFireStore>();

        foreach (BaseFireStore data in m_Data.Where(data => data != null))
        {
            if (data.EnumType == DataType.None)
            {
                Debug.LogError($"[FireStoreManager] {data.name}의 EnumType이 None입니다.");
                continue;
            }

            if (m_DataDictionary.ContainsKey(data.EnumType))
            {
                Debug.LogError($"[FireStoreManager] DataType 중복 등록: {data.EnumType}");
                continue;
            }

            m_DataDictionary.Add(data.EnumType, data);
        }
    }

    public FirestoreRequestContext DocumentType(DataType type)
    {
        return new FirestoreRequestContext(m_DataDictionary[type]);
    }

    public T GetData<T>(DataType type) where T : BaseFireStore
    {
        if (m_DataDictionary == null)
            InitDictionary();

        if (m_DataDictionary != null && m_DataDictionary.TryGetValue(type, out BaseFireStore data))
            return data as T;

        Debug.LogWarning($"[FireStoreManager] {type} 타입 데이터를 찾을 수 없습니다.");
        return null;
    }

    public bool TryGetData<T>(DataType type, out T result) where T : BaseFireStore
    {
        result = GetData<T>(type);
        return result != null;
    }

    public void ClearSession()
    {
        IsInitialized = false;
        m_DataDictionary = null;
        db = null;

        Debug.Log("[FireStoreManager] 세션 초기화 완료");
    }
}
