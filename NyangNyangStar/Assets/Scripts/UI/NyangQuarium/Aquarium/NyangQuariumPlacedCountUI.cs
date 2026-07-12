using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Managers;
using TMPro;
using UI.NyangQuarium.Quest;
using UnityEngine;

/// <summary>
/// 선택된 수조 타입에 배치된 물고기와 자연 요소 개수를 표시합니다.
/// 담수와 바다 수조에서 공통으로 사용합니다.
/// </summary>
public sealed class NyangQuariumPlacedCountUI : MonoBehaviour
{
    /// <summary>
    /// 특정 수조의 배치 데이터가 변경됐을 때 발생합니다.
    /// 별도 이벤트 스크립트 없이 이 클래스 내부에서 관리합니다.
    /// </summary>
    private static event Action<FishType> CountChanged;

    [Header("수조 타입")]
    [SerializeField]private FishType _aquariumType = FishType.Freshwater;

    [Header("수조 레벨")]
    [Tooltip("레벨별 물고기와 자연 요소 최대 배치 개수 데이터")]
    [SerializeField]private NyangQuariumAquariumLevelSO _aquariumLevelSO;

    [Header("배치 개수 Text")]
    [SerializeField]
    private TMP_Text _fishCountText;

    [SerializeField]
    private TMP_Text _natureCountText;

    private bool _isRefreshing;

    private void OnEnable()
    {
        CountChanged -= HandleCountChanged;
        CountChanged += HandleCountChanged;

        Refresh();
    }

    private void OnDisable()
    {
        CountChanged -= HandleCountChanged;
    }

    /// <summary>
    /// 배치 또는 삭제가 완료된 수조 타입을 알립니다.
    /// 물고기와 자연 요소 개수를 캐시에서 다시 확인합니다.
    /// </summary>
    public static void NotifyCountChanged(
        FishType aquariumType)
    {
        CountChanged?.Invoke(aquariumType);
    }

    /// <summary>
    /// Firestore 서버 데이터를 다시 불러온 후
    /// 현재 수조의 배치 개수를 갱신합니다.
    /// </summary>
    public async void Refresh()
    {
        await RefreshFromFirestoreAsync();
    }

    /// <summary>
    /// 수조 배치 데이터 변경 이벤트를 받습니다.
    /// 자신의 수조 타입과 다른 이벤트는 무시합니다.
    /// </summary>
    private void HandleCountChanged(
        FishType changedAquariumType)
    {
        if (changedAquariumType != _aquariumType)
            return;

        RefreshCachedCount();

        DebugTool.Log(
            $"[{nameof(NyangQuariumPlacedCountUI)}] " +
            $"{GetAquariumTypeName()} 개수 변경 이벤트 수신",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// Firestore에 저장된 최신 데이터를 불러와
    /// 현재 선택된 수조의 개수를 표시합니다.
    /// </summary>
    private async Task RefreshFromFirestoreAsync()
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;

        try
        {
            NyangQuariumFirestoreSO firestoreSO =
                await NyangQuariumFirestoreSO.WaitForReadyAsync();

            if (firestoreSO == null)
            {
                DebugTool.Warning(
                    $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                    "NyangQuariumFirestoreSO를 가져오지 못했습니다.",
                    DebugType.UI,
                    this);

                SetCountText(0, 0, 0, 0);
                return;
            }

            bool isLoaded =
                await firestoreSO.LoadOrCreateFromServerAsync();

            if (!isLoaded)
            {
                DebugTool.Warning(
                    $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                    "Firestore 데이터를 불러오지 못했습니다.",
                    DebugType.UI,
                    this);

                SetCountText(0, 0, 0, 0);
                return;
            }

            UpdateCount(firestoreSO);

            DebugTool.Log(
                $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                $"{GetAquariumTypeName()} 배치 개수 갱신 완료",
                DebugType.UI,
                this);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// 서버를 다시 조회하지 않고,
    /// 메모리에 저장된 최신 데이터로 즉시 갱신합니다.
    /// </summary>
    public async void RefreshCachedCount()
    {
        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
        {
            DebugTool.Warning(
                $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                "캐시 갱신 중 NyangQuariumFirestoreSO를 가져오지 못했습니다.",
                DebugType.UI,
                this);

            return;
        }

        UpdateCount(firestoreSO);
    }

    /// <summary>
    /// 현재 수조 타입에 해당하는 물고기와 자연 요소만 조회합니다.
    /// </summary>
    private void UpdateCount(
    NyangQuariumFirestoreSO firestoreSO)
    {
        IReadOnlyList<int> placedFishIds =
            firestoreSO.GetPlacedFishIds(
                _aquariumType);

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            firestoreSO.GetPlacedNatureData(
                _aquariumType);

        int fishCount =
            placedFishIds?.Count ?? 0;

        int natureCount =
            placedNatureData?.Count ?? 0;

        if (!TryGetCurrentPlacementLimits(
                firestoreSO,
                out int maxPlaceableFish,
                out int maxPlaceableNature))
        {
            SetCountText(
                fishCount,
                natureCount,
                0,
                0);

            return;
        }

        SetCountText(
            fishCount,
            natureCount,
            maxPlaceableFish,
            maxPlaceableNature);

        DebugTool.Log(
            $"[{nameof(NyangQuariumPlacedCountUI)}] " +
            $"{GetAquariumTypeName()} 개수 - " +
            $"물고기:{fishCount}/{maxPlaceableFish}, " +
            $"자연 요소:{natureCount}/{maxPlaceableNature}",
            DebugType.UI,
            this);
    }

    /// <summary>
    /// 현재 수조 레벨에 해당하는 물고기와 자연 요소 최대 배치 수를 가져옵니다.
    /// </summary>
    private bool TryGetCurrentPlacementLimits(
        NyangQuariumFirestoreSO firestoreSO,
        out int maxPlaceableFish,
        out int maxPlaceableNature)
    {
        maxPlaceableFish = 0;
        maxPlaceableNature = 0;

        if (firestoreSO == null)
            return false;

        if (_aquariumLevelSO == null)
        {
            _aquariumLevelSO =
                NyangQuariumQuestSOLocator.ResolveAquariumLevelSO();
        }

        if (_aquariumLevelSO == null)
        {
            DebugTool.Warning(
                $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                "NyangQuariumAquariumLevelSO를 찾지 못했습니다.",
                DebugType.UI,
                this);

            return false;
        }

        if (!firestoreSO.TryGetCurrentMaxPlaceableCount(
                _aquariumLevelSO,
                out maxPlaceableFish,
                out maxPlaceableNature))
        {
            DebugTool.Warning(
                $"[{nameof(NyangQuariumPlacedCountUI)}] " +
                $"현재 수조 레벨의 배치 한도를 가져오지 못했습니다. " +
                $"AquariumLevel:{firestoreSO.AquariumLevel}",
                DebugType.UI,
                this);

            return false;
        }

        return true;
    }


    private void SetCountText(int fishCount,int natureCount,int maxPlaceableFish, int maxPlaceableNature)
    {
        if (_fishCountText != null)
        {
            _fishCountText.text =
                $"{fishCount}/{maxPlaceableFish}";
        }

        if (_natureCountText != null)
        {
            _natureCountText.text =
                $"{natureCount}/{maxPlaceableNature}";
        }
    }

    private string GetAquariumTypeName()
    {
        return _aquariumType switch
        {
            FishType.Freshwater => "담수 수조",
            FishType.Saltwater => "바다 수조",
            _ => _aquariumType.ToString()
        };
    }
}