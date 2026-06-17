using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

public enum DataType
{
    None,
    Users,
    // 최상위(루트) SO 전용 식별자. 서브컬렉션 SO는 enum에 넣지 않고 인스펙터 직접 참조로 접근한다.
}

public class FireStoreManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private int _sessionVersion;
    public static FireStoreManager Instance { get; private set; }

    // 최상위(루트) SO만 등록한다. 서브컬렉션 SO는 각 부모 SO의 subCollections에만 등록한다.
    // (구 필드명 m_Data → rootSOs. FormerlySerializedAs로 기존 프리팹/씬 참조를 그대로 승계한다.)
    [FormerlySerializedAs("m_Data")]
    [SerializeField] private List<BaseFireStore> rootSOs;
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
        int sessionVersion = ++_sessionVersion;
        IsInitialized = false;

        Debug.Log($"[FireStoreManager] InitAsync 시작 / userId: {userId}");

        if (rootSOs == null || rootSOs.Count == 0)
        {
            Debug.LogError("[FireStoreManager] rootSOs가 비어 있습니다.");
            return;
        }

        for (int i = 0; i < rootSOs.Count; i++)
        {
            if (rootSOs[i] == null)
            {
                Debug.LogError($"[FireStoreManager] rootSOs[{i}]가 null입니다.");
                return;
            }

            Debug.Log($"[FireStoreManager] 등록 루트 SO: {rootSOs[i].name} / Type: {rootSOs[i].EnumType}");
        }

        try
        {
            InitDictionary();
            await InitFirebase(userId, sessionVersion);

            if (!IsCurrentSession(sessionVersion))
            {
                Debug.Log("[FireStoreManager] 오래된 InitAsync 결과 무시");
                return;
            }

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

    private async Task InitFirebase(string userId, int sessionVersion)
    {
        DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (!IsCurrentSession(sessionVersion))
            return;

        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase 초기화 실패: {status}");
            return;
        }

        db = FirebaseFirestore.DefaultInstance;

        // SO 참조를 먼저 현재 로그인 유저 기준으로 바인딩한다.
        // 신규 유저 생성 과정에서 하위 SO가 직접 사용되더라도 db/userId가 비어 있지 않게 한다.
        BindClass(userId);

        bool exists = await ExistsAsync(userId);
        if (!IsCurrentSession(sessionVersion))
            return;

        if (exists)
        {
            Debug.Log($"UserID {userId} exists in Firestore.");
            await LoadClass();
            if (!IsCurrentSession(sessionVersion))
                return;

            Debug.Log("기존 유저 데이터 로드 완료");
        }
        else
        {
            Debug.Log($"UserID {userId} does NOT exist in Firestore.");

            // 신규 유저: 루트 SO와 그 하위 서브컬렉션을 재귀로 생성한다.
            // (타임아웃 가드가 있어 느린 네트워크에서도 로그인 흐름은 막히지 않는다.)
            await CreateNew(userId);
            if (!IsCurrentSession(sessionVersion))
                return;
        }

        Debug.Log("Firebase 초기화 성공");
    }

    private async Task CreateNew(string userId)
    {
        Debug.Log($"[FireStoreManager] 신규 유저 데이터 생성 시작: {userId}");

        bool completed = await CreateAllWithTimeoutAsync(userId, 3f);

        if (completed)
            Debug.Log($"[FireStoreManager] 신규 유저 데이터 생성 완료: {userId}");
        else
            Debug.LogWarning($"[FireStoreManager] 신규 유저 데이터 생성 대기 시간 초과. 초기화는 계속 진행합니다. / userId: {userId}");
    }

    private async Task<bool> CreateAllWithTimeoutAsync(string userId, float timeoutSeconds)
    {
        Task writeTask = CreateAllRootsAsync(userId);
        Task timeoutTask = Task.Delay(Mathf.RoundToInt(timeoutSeconds * 1000f));

        Task completedTask = await Task.WhenAny(writeTask, timeoutTask);

        if (completedTask == writeTask)
        {
            await writeTask;
            return true;
        }

        writeTask.ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogWarning($"[FireStoreManager] 신규 유저 데이터 생성 취소: {userId}");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogWarning($"[FireStoreManager] 신규 유저 데이터 생성 실패: {userId} / {task.Exception?.GetBaseException().Message}");
                return;
            }

            Debug.Log($"[FireStoreManager] 신규 유저 데이터 백그라운드 생성 완료: {userId}");
        });

        return false;
    }

    // 등록된 루트 SO들의 CreateNew를 호출한다. 각 SO가 자신과 하위 서브컬렉션 문서를 재귀로 생성한다.
    private async Task CreateAllRootsAsync(string userId)
    {
        foreach (BaseFireStore root in rootSOs)
            await root.CreateNew(db, userId);
    }

    private void BindClass(string userId)
    {
        foreach (BaseFireStore item in rootSOs)
            item.InitDataBase(db, userId);
    }

    private async Task LoadClass()
    {
        foreach (BaseFireStore item in rootSOs)
        {
            try
            {
                await item.UpdateFromServerAsync(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FireStoreManager] {item.name} 서버 데이터 로드 실패: {e.Message}");
            }
        }
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        DocumentSnapshot snapshot = await db.Collection("Users").Document(userId).GetSnapshotAsync();

        bool exists = snapshot.Exists;
        Debug.Log($"[ExistsAsync] UserID={userId} 문서 존재여부: {exists}");
        return exists;
    }

    private void InitDictionary()
    {
        m_DataDictionary = new Dictionary<DataType, BaseFireStore>();

        foreach (BaseFireStore data in rootSOs.Where(data => data != null))
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

    private bool IsCurrentSession(int sessionVersion)
    {
        return _sessionVersion == sessionVersion;
    }

    public FirestoreRequestContext DocumentType(DataType type)
    {
        if (m_DataDictionary == null)
            throw new System.InvalidOperationException("FireStoreManager가 아직 초기화되지 않았습니다.");

        return new FirestoreRequestContext(m_DataDictionary[type]);
    }

    public void ClearSession()
    {
        _sessionVersion++;
        IsInitialized = false;
        m_DataDictionary = null;
        db = null;

        Debug.Log("[FireStoreManager] 세션 초기화 완료");
    }
}
