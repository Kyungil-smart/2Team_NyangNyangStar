using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

public enum DataType
{
    None = 0,
    Users = 1,
    MergeBoard = 2,
    ScratchingTime = 3,
    MoongchiProgess = 4
}

public class FireStoreManager : MonoBehaviour
{
    private const int FirestoreTimeoutMs = 5000;

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
        catch (Exception e)
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

        BindClass(userId);

        if (exists)
        {
            Debug.Log($"UserID {userId} exists in Firestore.");
            await LoadClass();
            Debug.Log("기존 유저 데이터 로드 완료");
        }
        else
        {
            Debug.Log($"UserID {userId} does NOT exist in Firestore.");
            await CreateNew(userId);
        }

        Debug.Log("Firebase 초기화 성공");
    }

    private async Task CreateNew(string userId)
    {
        Debug.Log($"[FireStoreManager] 신규 유저 루트 문서 생성 시작: {userId}");

        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "UserID", userId },
            { "CreatedAt", Timestamp.GetCurrentTimestamp() },
            { "LastLoginAt", Timestamp.GetCurrentTimestamp() }
        };

        DocumentReference userRef = db.Collection("Users").Document(userId);
        Task writeTask = userRef.SetAsync(userData, SetOptions.MergeAll);

        Task finishedTask = await Task.WhenAny(writeTask, Task.Delay(FirestoreTimeoutMs));

        if (finishedTask != writeTask)
        {
            Debug.LogWarning($"[FireStoreManager] 신규 유저 루트 문서 생성 대기 시간 초과. 초기화는 계속 진행합니다. userId: {userId}");
            WatchTask(writeTask, "[FireStoreManager] 신규 유저 루트 문서 생성");
            return;
        }

        await writeTask;

        Debug.Log("[FireStoreManager] 신규 유저 루트 문서 생성 완료");
    }

    private void BindClass(string userId)
    {
        foreach (BaseFireStore item in m_Data.Where(item => item != null))
            item.InitDataBase(db, userId);
    }

    private async Task LoadClass()
    {
        foreach (BaseFireStore item in m_Data.Where(item => item != null))
        {
            try
            {
                Task loadTask = item.UpdateFromServerAsync(true);
                Task finishedTask = await Task.WhenAny(loadTask, Task.Delay(FirestoreTimeoutMs));

                if (finishedTask != loadTask)
                {
                    Debug.LogWarning($"[FireStoreManager] 데이터 로드 시간 초과: {item.name} / Type: {item.EnumType}");
                    WatchTask(loadTask, $"[FireStoreManager] 데이터 로드: {item.name}");
                    continue;
                }

                await loadTask;
                Debug.Log($"[FireStoreManager] 데이터 로드 완료: {item.name} / Type: {item.EnumType}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FireStoreManager] 데이터 로드 실패: {item.name} / Type: {item.EnumType} / {e.Message}");
            }
        }
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        DocumentReference userRef = db.Collection("Users").Document(userId);
        DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();

        bool exists = snapshot.Exists;
        Debug.Log($"[ExistsAsync] UserID={userId} 문서 존재여부: {exists}");
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

    public T GetData<T>(DataType type) where T : BaseFireStore
    {
        if (m_DataDictionary == null)
            InitDictionary();

        if (m_DataDictionary != null && m_DataDictionary.TryGetValue(type, out BaseFireStore data))
            return data as T;

        Debug.LogWarning($"[FireStoreManager] DataType을 찾을 수 없습니다: {type}");
        return null;
    }

    public FirestoreRequestContext DocumentType(DataType type)
    {
        return new FirestoreRequestContext(m_DataDictionary[type]);
    }

    public void ClearSession()
    {
        IsInitialized = false;
        m_DataDictionary = null;
        db = null;

        Debug.Log("[FireStoreManager] 세션 초기화 완료");
    }

    private async void WatchTask(Task task, string label)
    {
        try
        {
            await task;
            Debug.Log($"{label} 지연 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"{label} 지연 실패: {e}");
        }
    }
}
