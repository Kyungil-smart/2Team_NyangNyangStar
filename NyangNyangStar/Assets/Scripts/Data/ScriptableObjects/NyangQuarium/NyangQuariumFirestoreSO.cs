using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UI.NyangQuarium;
using UnityEngine;

[FirestorePath("Users/{userId}/NyangQuarium/{docId}")]
[CreateAssetMenu(fileName = "NyangQuariumFirestoreSO", menuName = "SO/NyangQuarium/NyangQuarium Firestore SO")]
public class NyangQuariumFirestoreSO : BaseFireStore
{
    [Header("도감")]
    [SerializeField] private List<int> _unlockedFishIds = new();

    [Header("수조 배치")]
    [SerializeField] private List<int> _freshwaterPlacedFishIds = new();
    [SerializeField] private List<int> _saltwaterPlacedFishIds = new();

    [Header("수조 자연 요소 배치 - 상세 데이터")]
    [SerializeField] private List<NyangQuariumPlacedNatureData> _freshwaterPlacedNatureData = new();
    [SerializeField] private List<NyangQuariumPlacedNatureData> _saltwaterPlacedNatureData = new();

    [Header("Aquarium Level")]
    [SerializeField] private int _aquariumLevel = 1;
    [SerializeField] private int _aquariumExp;


    [Header("스토리")]
    [SerializeField] private List<int> _readStoryIds = new();

    private readonly HashSet<int> _unlockedFishIdSet = new();

    public IReadOnlyList<int> UnlockedFishIds => _unlockedFishIds;
    public IReadOnlyList<int> FreshwaterPlacedFishIds => _freshwaterPlacedFishIds;
    public IReadOnlyList<int> SaltwaterPlacedFishIds => _saltwaterPlacedFishIds;
    public IReadOnlyList<NyangQuariumPlacedNatureData> FreshwaterPlacedNatureData => _freshwaterPlacedNatureData;
    public IReadOnlyList<NyangQuariumPlacedNatureData> SaltwaterPlacedNatureData => _saltwaterPlacedNatureData;
    public int AquariumLevel => _aquariumLevel;
    public int AquariumExp => _aquariumExp;

    private void OnEnable()
    {
        NormalizeAquariumLevel();
        RebuildCache();
        NormalizePlacedFishLists();
        NormalizePlacedNatureList();
    }

    public static async Task<NyangQuariumFirestoreSO> WaitForReadyAsync(int timeoutMs = 5000)
    {
        int elapsedMs = 0;
        const int intervalMs = 100;

        while (elapsedMs < timeoutMs)
        {
            FireStoreManager manager = FireStoreManager.Instance;

            if (manager != null &&
                manager.IsInitialized &&
                manager.TryGetStore(out NyangQuariumFirestoreSO store) &&
                store != null)
            {
                store.TryEnsureDatabaseReady();
                return store;
            }

            await Task.Delay(intervalMs);
            elapsedMs += intervalMs;
        }

        FireStoreManager finalManager = FireStoreManager.Instance;
        if (finalManager != null &&
            finalManager.IsInitialized &&
            finalManager.TryGetStore(out NyangQuariumFirestoreSO finalStore) &&
            finalStore != null)
        {
            finalStore.TryEnsureDatabaseReady();
            return finalStore;
        }

        return null;
    }

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        InitDataBase(database, userId);
        ResetToDefault();
        await SetDataAsync(ToFirestoreDictionary());
    }

    public override void ApplyFromSnapshot(DocumentSnapshot snapshot)
    {
        base.ApplyFromSnapshot(snapshot);
        NormalizeAquariumLevel();
        RebuildCache();
        NormalizePlacedFishLists();
        NormalizePlacedNatureList();
    }

    public async Task<bool> LoadOrCreateFromServerAsync()
    {
        if (!TryEnsureDatabaseReady())
            return false;

        DocumentSnapshot snapshot = await UpdateFromServerAsync(false);
        if (snapshot != null && snapshot.Exists)
            return true;

        ResetToDefault();
        await SetDataAsync(ToFirestoreDictionary());
        return true;
    }

    public async Task<bool> AddAquariumExpAsync(
        int expAmount,
        NyangQuariumAquariumLevelSO aquariumLevelSO = null)
    {
        if (expAmount <= 0)
            return false;

        NormalizeAquariumLevel();
        _aquariumExp += expAmount;
        ApplyAquariumLevelUps(aquariumLevelSO);

        if (!TryEnsureDatabaseReady())
        {
            DebugTool.Warning("[NyangQuariumFirestoreSO] Firestore is not ready. Aquarium EXP was updated locally only.", DebugType.Data, this);
            return false;
        }

        await SetDataAsync(ToFirestoreDictionary());
        return true;
    }

    public bool TryGetCurrentAquariumLevelData(
        NyangQuariumAquariumLevelSO aquariumLevelSO,
        out NyangQuariumAquariumLevelData levelData)
    {
        levelData = null;

        if (aquariumLevelSO == null)
            return false;

        NormalizeAquariumLevel();
        return aquariumLevelSO.TryGetByLevel(_aquariumLevel, out levelData);
    }

    public bool TryGetCurrentMaxPlaceableCount(
        NyangQuariumAquariumLevelSO aquariumLevelSO,
        out int maxPlaceableFish,
        out int maxPlaceableEnvironment)
    {
        maxPlaceableFish = 0;
        maxPlaceableEnvironment = 0;

        if (aquariumLevelSO == null)
            return false;

        NormalizeAquariumLevel();
        return aquariumLevelSO.TryGetMaxPlaceableCount(
            _aquariumLevel,
            out maxPlaceableFish,
            out maxPlaceableEnvironment);
    }

    public bool IsUnlocked(int fishId)
    {
        if (fishId <= 0)
            return false;

        RebuildCacheIfNeeded();
        return _unlockedFishIdSet.Contains(fishId);
    }

    public bool IsRegistered(int fishId) => IsUnlocked(fishId);

    public IReadOnlyList<NyangQuariumCollectionEntry> GetFishEntries(
        NyangQuariumFishSO fishSO,
        FishType fishType = FishType.None)
    {
        return BuildFishEntries(fishSO, fishType, false);
    }

    public IReadOnlyList<NyangQuariumCollectionEntry> GetUnlockedFishEntries(
        NyangQuariumFishSO fishSO,
        FishType fishType = FishType.None)
    {
        return BuildFishEntries(fishSO, fishType, true);
    }

    public bool TryGetFishEntry(
        NyangQuariumFishSO fishSO,
        int fishId,
        out NyangQuariumCollectionEntry entry)
    {
        entry = null;

        if (fishSO == null || fishSO.FishData == null || fishId <= 0)
            return false;

        RebuildCacheIfNeeded();

        foreach (NyangQuariumFishData fishData in fishSO.FishData)
        {
            if (fishData == null || fishData.FishId != fishId)
                continue;

            entry = new NyangQuariumCollectionEntry(fishData, _unlockedFishIdSet.Contains(fishId));
            return true;
        }

        return false;
    }

    public NyangQuariumCollectionProgress GetProgress(
        NyangQuariumFishSO fishSO,
        FishType fishType = FishType.None)
    {
        if (fishSO == null || fishSO.FishData == null)
            return new NyangQuariumCollectionProgress(0, 0);

        RebuildCacheIfNeeded();

        int totalCount = 0;
        int unlockedCount = 0;

        foreach (NyangQuariumFishData fishData in fishSO.FishData)
        {
            if (fishData == null || !MatchesFishType(fishData, fishType))
                continue;

            totalCount++;

            if (_unlockedFishIdSet.Contains(fishData.FishId))
                unlockedCount++;
        }

        return new NyangQuariumCollectionProgress(unlockedCount, totalCount);
    }

    public IReadOnlyList<NyangquariumPlacedFishData> GetPlacedFishData(
        FishType aquariumType,
        NyangQuariumFishSO fishSO)
    {
        List<NyangquariumPlacedFishData> result = new();
        IReadOnlyList<int> placedFishIds = GetPlacedFishIds(aquariumType);

        if (placedFishIds == null || fishSO == null || fishSO.FishData == null)
            return result;

        foreach (int fishId in placedFishIds)
        {
            if (fishId <= 0)
                continue;

            if (!TryFindFishData(fishSO, fishId, out NyangQuariumFishData fishData))
                continue;

            result.Add(new NyangquariumPlacedFishData(
                fishId,
                fishData.FishKey));
        }

        return result;
    }

    public async Task<bool> SavePlacedFishAsync(
        FishType aquariumType,
        IEnumerable<NyangquariumPlacedFishData> placedFishData,
        NyangQuariumFishSO fishSO = null)
    {
        if (!TryEnsureDatabaseReady())
        {
            DebugTool.Warning("[NyangQuariumFirestoreSO] Firestore가 준비되지 않아 수조 배치 저장을 생략합니다.", DebugType.Data, this);
            return false;
        }

        SetPlacedFishData(aquariumType, placedFishData, fishSO);
        await SetDataAsync(ToFirestoreDictionary());
        return true;
    }

    /// <summary>
    /// 현재 수조에 배치된 자연 요소의 식별 정보, 위치, 크기를 저장합니다.
    /// </summary>
    public async Task<bool> SavePlacedNatureAsync(
        FishType aquariumType,
        IEnumerable<NyangQuariumPlacedNatureData> placedNatureData)
    {
        if (!TryEnsureDatabaseReady())
        {
            DebugTool.Warning(
                "[NyangQuariumFirestoreSO] Firestore가 준비되지 않아 자연 요소 배치 저장을 생략합니다.",
                DebugType.Data,
                this);
            return false;
        }

        SetPlacedNatureData(aquariumType, placedNatureData);
        await SetDataAsync(ToFirestoreDictionary());
        return true;
    }


    public void SetPlacedFishData(
        FishType aquariumType,
        IEnumerable<NyangquariumPlacedFishData> placedFishData,
        NyangQuariumFishSO fishSO = null)
    {
        List<int> targetList = GetMutablePlacedFishIds(aquariumType);
        targetList.Clear();

        if (placedFishData == null)
            return;

        foreach (NyangquariumPlacedFishData placedFish in placedFishData)
        {
            if (placedFish == null)
                continue;

            int fishId = ResolveFishId(placedFish, fishSO);

            if (fishId <= 0)
                continue;

            targetList.Add(fishId);
        }
    }

    public IReadOnlyList<NyangQuariumPlacedNatureData> GetPlacedNatureData(
        FishType aquariumType)
    {
        return GetMutablePlacedNatureData(aquariumType);
    }

    public void SetPlacedNatureData(
        FishType aquariumType,
        IEnumerable<NyangQuariumPlacedNatureData> placedNatureData)
    {
        List<NyangQuariumPlacedNatureData> targetData =
            GetMutablePlacedNatureData(aquariumType);

        targetData.Clear();

        if (placedNatureData == null)
            return;

        foreach (NyangQuariumPlacedNatureData natureData in placedNatureData)
        {
            if (natureData == null ||
                natureData.ItemId <= 0 ||
                string.IsNullOrWhiteSpace(natureData.SpriteKey))
            {
                continue;
            }

            targetData.Add(natureData);
        }
    }


    public async Task<bool> UnlockFishAsync(int fishId)
    {
        if (!TryEnsureDatabaseReady())
        {
            DebugTool.Warning("[NyangQuariumFirestoreSO] Firestore가 준비되지 않아 도감 해금을 저장하지 못했습니다.", DebugType.Data, this);
            return false;
        }

        if (!UnlockFish(fishId))
            return false;

        await SetDataAsync(ToFirestoreDictionary());
        return true;
    }

    private bool UnlockFish(int fishId)
    {
        if (fishId <= 0)
            return false;

        RebuildCacheIfNeeded();

        if (_unlockedFishIdSet.Contains(fishId))
            return false;

        _unlockedFishIds.Add(fishId);
        _unlockedFishIdSet.Add(fishId);
        return true;
    }

    private IReadOnlyList<NyangQuariumCollectionEntry> BuildFishEntries(
        NyangQuariumFishSO fishSO,
        FishType fishType,
        bool unlockedOnly)
    {
        List<NyangQuariumCollectionEntry> entries = new();

        if (fishSO == null || fishSO.FishData == null)
            return entries;

        RebuildCacheIfNeeded();

        foreach (NyangQuariumFishData fishData in fishSO.FishData)
        {
            if (fishData == null || !MatchesFishType(fishData, fishType))
                continue;

            bool isUnlocked = _unlockedFishIdSet.Contains(fishData.FishId);

            if (unlockedOnly && !isUnlocked)
                continue;

            entries.Add(new NyangQuariumCollectionEntry(fishData, isUnlocked));
        }

        return entries;
    }

    private int ResolveFishId(NyangquariumPlacedFishData placedFish, NyangQuariumFishSO fishSO)
    {
        if (placedFish.FishId > 0)
            return placedFish.FishId;

        if (fishSO == null ||
            fishSO.FishData == null ||
            string.IsNullOrWhiteSpace(placedFish.SpriteKey))
        {
            return 0;
        }

        foreach (NyangQuariumFishData fishData in fishSO.FishData)
        {
            if (fishData != null && fishData.FishKey == placedFish.SpriteKey)
                return fishData.FishId;
        }

        return 0;
    }

    private static bool TryFindFishData(
        NyangQuariumFishSO fishSO,
        int fishId,
        out NyangQuariumFishData fishData)
    {
        fishData = null;

        if (fishSO == null || fishSO.FishData == null || fishId <= 0)
            return false;

        foreach (NyangQuariumFishData data in fishSO.FishData)
        {
            if (data == null || data.FishId != fishId)
                continue;

            fishData = data;
            return true;
        }

        return false;
    }

    public IReadOnlyList<int> GetPlacedFishIds(FishType aquariumType)
    {
        return aquariumType == FishType.Saltwater ? _saltwaterPlacedFishIds : _freshwaterPlacedFishIds;
    }


    private List<int> GetMutablePlacedFishIds(FishType aquariumType)
    {
        if (aquariumType == FishType.Saltwater)
        {
            _saltwaterPlacedFishIds ??= new List<int>();
            return _saltwaterPlacedFishIds;
        }

        _freshwaterPlacedFishIds ??= new List<int>();
        return _freshwaterPlacedFishIds;
    }


    private List<NyangQuariumPlacedNatureData> GetMutablePlacedNatureData(
        FishType aquariumType)
    {
        if (aquariumType == FishType.Saltwater)
        {
            _saltwaterPlacedNatureData ??= new List<NyangQuariumPlacedNatureData>();
            return _saltwaterPlacedNatureData;
        }

        _freshwaterPlacedNatureData ??= new List<NyangQuariumPlacedNatureData>();
        return _freshwaterPlacedNatureData;
    }


    private static bool MatchesFishType(NyangQuariumFishData fishData, FishType fishType)
    {
        return fishType == FishType.None || fishData.FishType == fishType;
    }


    public bool HasReadStory(int storyId)
    {
        return _readStoryIds != null && _readStoryIds.Contains(storyId);
    }

    public async Task MarkStoryReadAsync(int storyId)
    {
        _readStoryIds ??= new List<int>();

        if (_readStoryIds.Contains(storyId))
            return;

        if (!TryEnsureDatabaseReady())
        {
            DebugTool.Warning("[NyangQuariumFirestoreSO] Firestore가 준비되지 않아 스토리 읽음 저장을 생략합니다.", DebugType.Data, this);
            return;
        }

        _readStoryIds.Add(storyId);
        await SetDataAsync(ToFirestoreDictionary());
    }

    private void ResetToDefault()
    {
        _unlockedFishIds ??= new List<int>();
        _unlockedFishIds.Clear();
        _unlockedFishIdSet.Clear();

        _freshwaterPlacedFishIds ??= new List<int>();
        _freshwaterPlacedFishIds.Clear();

        _saltwaterPlacedFishIds ??= new List<int>();
        _saltwaterPlacedFishIds.Clear();

        _readStoryIds ??= new List<int>();
        _readStoryIds.Clear();

        _aquariumLevel = 1;
        _aquariumExp = 0;

        _freshwaterPlacedNatureData ??= new List<NyangQuariumPlacedNatureData>();
        _freshwaterPlacedNatureData.Clear();

        _saltwaterPlacedNatureData ??= new List<NyangQuariumPlacedNatureData>();
        _saltwaterPlacedNatureData.Clear();
    }

    private void RebuildCacheIfNeeded()
    {
        if (_unlockedFishIds != null && _unlockedFishIdSet.Count == _unlockedFishIds.Count)
            return;

        RebuildCache();
    }

    private void RebuildCache()
    {
        _unlockedFishIdSet.Clear();

        if (_unlockedFishIds == null)
        {
            _unlockedFishIds = new List<int>();
            return;
        }

        for (int i = _unlockedFishIds.Count - 1; i >= 0; i--)
        {
            int fishId = _unlockedFishIds[i];

            if (fishId <= 0 || !_unlockedFishIdSet.Add(fishId))
                _unlockedFishIds.RemoveAt(i);
        }
    }

    private void ApplyAquariumLevelUps(NyangQuariumAquariumLevelSO aquariumLevelSO)
    {
        if (aquariumLevelSO == null)
            return;

        while (aquariumLevelSO.TryGetByLevel(_aquariumLevel, out NyangQuariumAquariumLevelData levelData))
        {
            if (levelData.RequiredExp <= 0 || _aquariumExp < levelData.RequiredExp)
                break;

            _aquariumExp -= levelData.RequiredExp;
            _aquariumLevel++;
        }
    }

    private void NormalizeAquariumLevel()
    {
        if (_aquariumLevel <= 0)
            _aquariumLevel = 1;

        if (_aquariumExp < 0)
            _aquariumExp = 0;
    }

    private void NormalizePlacedFishLists()
    {
        NormalizePlacedFishList(ref _freshwaterPlacedFishIds);
        NormalizePlacedFishList(ref _saltwaterPlacedFishIds);
    }
    private void NormalizePlacedNatureList()
    {
        NormalizePlacedNatureDataList(ref _freshwaterPlacedNatureData);
        NormalizePlacedNatureDataList(ref _saltwaterPlacedNatureData);
    }

    private static void NormalizePlacedNatureDataList(
        ref List<NyangQuariumPlacedNatureData> placedNatureData)
    {
        placedNatureData ??= new List<NyangQuariumPlacedNatureData>();

        for (int i = placedNatureData.Count - 1; i >= 0; i--)
        {
            NyangQuariumPlacedNatureData data = placedNatureData[i];
            if (data == null || data.ItemId <= 0 || string.IsNullOrWhiteSpace(data.SpriteKey))
                placedNatureData.RemoveAt(i);
        }
    }

    private static void NormalizePlacedFishList(ref List<int> placedFishIds)
    {
        if (placedFishIds == null)
        {
            placedFishIds = new List<int>();
            return;
        }

        for (int i = placedFishIds.Count - 1; i >= 0; i--)
        {
            if (placedFishIds[i] <= 0)
                placedFishIds.RemoveAt(i);
        }
    }
}

public sealed class NyangQuariumCollectionEntry
{
    public NyangQuariumFishData FishData { get; }
    public bool IsUnlocked { get; }
    public bool IsRegistered => IsUnlocked;

    public int FishId => FishData?.FishId ?? 0;
    public FishType FishType => FishData?.FishType ?? FishType.None;
    public int Level => FishData?.Level ?? 0;
    public string Category => FishData?.Category ?? string.Empty;
    public string FishName => FishData?.FishName ?? string.Empty;
    public string FishDescription => FishData?.FishDescription ?? string.Empty;
    public string FishKey => FishData?.FishKey ?? string.Empty;

    public NyangQuariumCollectionEntry(NyangQuariumFishData fishData, bool isUnlocked)
    {
        FishData = fishData;
        IsUnlocked = isUnlocked;
    }
}

public readonly struct NyangQuariumCollectionProgress
{
    public int UnlockedCount { get; }
    public int TotalCount { get; }
    public float Ratio => TotalCount == 0 ? 0f : (float)UnlockedCount / TotalCount;

    public NyangQuariumCollectionProgress(int unlockedCount, int totalCount)
    {
        UnlockedCount = unlockedCount;
        TotalCount = totalCount;
    }
}
