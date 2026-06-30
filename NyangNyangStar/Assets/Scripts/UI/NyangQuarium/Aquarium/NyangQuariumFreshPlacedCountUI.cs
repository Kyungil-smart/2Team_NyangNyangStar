using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Managers;
using TMPro;
using UnityEngine;

public sealed class NyangQuariumFreshPlacedCountUI : MonoBehaviour
{
    private const int MaxPlaceableFishCount = 15;
    private const int MaxPlaceableNatureCount = 5;

    [Header("담수 수조 배치 개수")]
    [SerializeField]
    private TMP_Text _freshFishCountText;

    [SerializeField]
    private TMP_Text _freshNatureCountText;

    private bool _isRefreshing;

    private async void OnEnable()
    {
        await RefreshFromFirestoreAsync();
    }

    /// <summary>
    /// UnityEvent 또는 다른 스크립트에서 호출할 수 있는 갱신 함수입니다.
    /// </summary>
    public async void Refresh()
    {
        await RefreshFromFirestoreAsync();
    }

    /// <summary>
    /// Firestore에서 최신 냥쿠아리움 데이터를 불러와
    /// 담수 수조의 물고기와 자연 요소 개수를 표시합니다.
    /// </summary>
    private async Task RefreshFromFirestoreAsync()
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;

        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshPlacedCountUI] " +
                "NyangQuariumFirestoreSO를 가져오지 못했습니다.",
                DebugType.UI,
                this);

            SetCountText(0, 0);
            _isRefreshing = false;
            return;
        }

        bool isLoaded =
            await firestoreSO.LoadOrCreateFromServerAsync();

        if (!isLoaded)
        {
            DebugTool.Warning(
                "[NyangQuariumFreshPlacedCountUI] " +
                "Firestore 데이터를 불러오지 못했습니다.",
                DebugType.UI,
                this);

            SetCountText(0, 0);
            _isRefreshing = false;
            return;
        }

        IReadOnlyList<int> placedFishIds =
            firestoreSO.GetPlacedFishIds(
                FishType.Freshwater);

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            firestoreSO.GetPlacedNatureData(
                FishType.Freshwater);

        int fishCount =
            placedFishIds?.Count ?? 0;

        int natureCount =
            placedNatureData?.Count ?? 0;

        SetCountText(
            fishCount,
            natureCount);

        DebugTool.Log(
            "[NyangQuariumFreshPlacedCountUI] " +
            $"담수 배치 개수 갱신 완료 - " +
            $"물고기:{fishCount}/{MaxPlaceableFishCount}, " +
            $"자연 요소:{natureCount}/{MaxPlaceableNatureCount}",
            DebugType.UI,
            this);

        _isRefreshing = false;
    }

    /// <summary>
    /// 이미 메모리에 반영된 FirestoreSO 데이터로 즉시 갱신합니다.
    /// 서버를 다시 조회하지 않습니다.
    /// </summary>
    public async void RefreshCachedCount()
    {
        NyangQuariumFirestoreSO firestoreSO =
            await NyangQuariumFirestoreSO.WaitForReadyAsync();

        if (firestoreSO == null)
            return;

        IReadOnlyList<int> placedFishIds =
            firestoreSO.GetPlacedFishIds(
                FishType.Freshwater);

        IReadOnlyList<NyangQuariumPlacedNatureData> placedNatureData =
            firestoreSO.GetPlacedNatureData(
                FishType.Freshwater);

        SetCountText(
            placedFishIds?.Count ?? 0,
            placedNatureData?.Count ?? 0);
    }

    private void SetCountText(
        int fishCount,
        int natureCount)
    {
        if (_freshFishCountText != null)
        {
            _freshFishCountText.text =
                $"{fishCount}/{MaxPlaceableFishCount}";
        }

        if (_freshNatureCountText != null)
        {
            _freshNatureCountText.text =
                $"{natureCount}/{MaxPlaceableNatureCount}";
        }
    }
}