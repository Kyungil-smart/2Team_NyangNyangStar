using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
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
    private int _sessionVersion;
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
        int sessionVersion = ++_sessionVersion;
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

            // 기존 구조처럼 UsersSO.CreateNew()를 타면 UsersSO 하위에 연결된 MergeBoard/Scratching SO까지
            // 한 번에 생성하면서 모바일에서 로그인 진행이 멈출 수 있다.
            // 로그인 단계에서는 Users/{uid} 루트 문서만 만든다.
            await CreateNew(userId);
            if (!IsCurrentSession(sessionVersion))
                return;
        }

        Debug.Log("Firebase 초기화 성공");
    }

    private async Task CreateNew(string userId)
    {
        Debug.Log($"[FireStoreManager] 신규 유저 루트 문서 생성 시작: {userId}");

        bool completed = await CreateUserRootDocumentWithTimeoutAsync(userId, 3f);

        if (completed)
            Debug.Log($"[FireStoreManager] 신규 유저 루트 문서 생성 완료: {userId}");
        else
            Debug.LogWarning($"[FireStoreManager] 신규 유저 루트 문서 생성 대기 시간 초과. 초기화는 계속 진행합니다. / userId: {userId}");
    }

    private async Task<bool> CreateUserRootDocumentWithTimeoutAsync(string userId, float timeoutSeconds)
    {
        Task writeTask = CreateUserRootDocumentAsync(userId);
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
                Debug.LogWarning($"[FireStoreManager] 신규 유저 루트 문서 생성 취소: {userId}");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogWarning($"[FireStoreManager] 신규 유저 루트 문서 생성 실패: {userId} / {task.Exception?.GetBaseException().Message}");
                return;
            }

            Debug.Log($"[FireStoreManager] 신규 유저 루트 문서 백그라운드 생성 완료: {userId}");
        });

        return false;
    }

    private async Task CreateUserRootDocumentAsync(string userId)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "UserID", userId },
            { "UpdatedAt", FieldValue.ServerTimestamp }
        };

        BaseFireStore usersStore = GetData<BaseFireStore>(DataType.Users);
        if (usersStore != null)
        {
            try
            {
                Dictionary<string, object> userDefaultData = usersStore.ToFirestoreDictionary();
                if (userDefaultData != null)
                {
                    foreach (KeyValuePair<string, object> pair in userDefaultData)
                        data[pair.Key] = pair.Value;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FireStoreManager] UsersSO 기본값 변환 실패, 최소 유저 문서만 생성합니다: {e.Message}");
            }
        }

        data["UserID"] = userId;
        data["UpdatedAt"] = FieldValue.ServerTimestamp;

        DocumentReference userDoc = db.Collection("Users").Document(userId);
        await userDoc.SetAsync(data, SetOptions.MergeAll);
    }

    private void BindClass(string userId)
    {
        foreach (BaseFireStore item in m_Data)
            item.InitDataBase(db, userId);
    }

    private async Task LoadClass()
    {
        foreach (BaseFireStore item in m_Data)
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
            return null;

        if (m_DataDictionary.TryGetValue(type, out BaseFireStore data))
            return data as T;

        return null;
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
