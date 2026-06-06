using System;

using System.Collections;

using Core.Managers;

using Data.LibrarySystem;

using Data.Parsing;

using Services.Enums;

using UnityEngine;



// 시트,서버 데이터 로드 및 ScratchingSo 동기화

public partial class ScratchingTimeManager

{

    // ScratchingSo,시트 데이터가 준비될 때까지 로드 시작

    private void EnsureRequiredDataLoad()

    {

        GameManager.Init();

        EnsureLocalDataAccess();



        if (_scratching == null)

        {

            DebugTool.Warning(

                "ScratchingSo 참조가 비어 있습니다. ScratchingTimeScreen 프리팹의 Scratching Time Manager에 SO를 연결하세요.",

                DebugType.ScratchingTime);

            return;

        }



        if (IsRequiredDataReady())

        {

            UnsubscribeDataReady();

            StopScratchingDataLoad();

            return;

        }



        SubscribeDataReady();

        StartScratchingDataLoad();

    }





    // 스크래칭 시트 직접 로드 코루틴 시작

    private void StartScratchingDataLoad()

    {

        if (_scratchingDataLoadCoroutine != null)

            return;



        _scratchingDataLoadCoroutine = StartCoroutine(LoadScratchingDataRoutine());

    }





    // 진행 중인 시트 로드 코루틴 중단

    private void StopScratchingDataLoad()

    {

        if (_scratchingDataLoadCoroutine == null)

            return;



        StopCoroutine(_scratchingDataLoadCoroutine);

        _scratchingDataLoadCoroutine = null;

    }





    // 스크래칭 시트만 즉시 요청. SheetLoader 긴 대기 없이 네트워크 1회로 완료

    private IEnumerator LoadScratchingDataRoutine()

    {

        if (IsRequiredDataReady())

        {

            OnScratchingDataLoaded();

            yield break;

        }



        yield return LoadScratchingSheetDirectly();



        _scratchingDataLoadCoroutine = null;



        if (IsRequiredDataReady())

            OnScratchingDataLoaded();

        else

            DebugTool.Warning(

                "스크래칭 타임 시트 로드에 실패했습니다. Fallback URL,시트 공개 설정을 확인하세요.",

                DebugType.ScratchingTime);

    }





    // 시트 로드 완료 후 UI,서버 진행도를 초기화

    private void OnScratchingDataLoaded()

    {

        UnsubscribeDataReady();



        if (!gameObject.activeInHierarchy)

            return;



        ResetAllState();

        LoadScratchingProgressFromServer();

        _moongchiStatController?.PrintMoongchiStat();



        if (HasSelectedStage)

            RefreshSelectionInfo();

    }





    // Google Sheets에서 스크래칭 밸런스만 받아 SO에 채움

    private IEnumerator LoadScratchingSheetDirectly()

    {

        if (_scratching == null || string.IsNullOrWhiteSpace(_scratchingSheetFallbackUrl))

            yield break;



        _scratching.Init();

        SheetData sheet = new(_scratchingSheetFallbackUrl.Trim(), SheetType.TSV);



        yield return sheet.Load(ParseScratchingSheetLines);

    }





    // TSV 시트 한 줄씩 파싱해 ScratchingSo.SetData 호출

    private void ParseScratchingSheetLines(char split, string[] lines)

    {

        if (lines == null || lines.Length <= ScratchingSheetHeaderRowCount)

            return;



        for (int i = ScratchingSheetHeaderRowCount; i < lines.Length; i++)

        {

            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))

                continue;



            string[] cols = line.Split(split);

            if (cols.Length < 5)

                continue;



            _scratching.SetData(cols);

        }

    }





    // 에디터 등에서 LocalDataAccess가 없을 때 런타임 인스턴스 보장

    private static void EnsureLocalDataAccess()

    {

        if (LocalDataAccess.Instance != null)

            return;



        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();

    }





    // ScratchingSo에 시트 데이터가 채워졌는지 확인

    private bool IsRequiredDataReady()

        => _scratching != null && _scratching.HasData;





    // 선택 단계가 없으면 1단계를 기본 선택 후 카드 정보 갱신

    private void RefreshInitialSelectionInfo()

    {

        if (HasSelectedStage)

        {

            RefreshSelectionInfo();

            return;

        }



        _selectedStage = 1;

        _selectedStageType = StageType.None;

        RefreshSelectionInfo();

    }





    // 서버에서 ScratchingProgressSO를 불러와 ScratchingSo에 반영

    private async void LoadScratchingProgressFromServer()

    {

        if (_scratchingProgress == null || _scratching == null)

            return;



        try

        {

            await _scratchingProgress.UpdateFromServerAsync(false);

            _scratchingProgress.ApplyTo(_scratching);

            RefreshStageButtonUnlockState();

            RefreshInitialSelectionInfo();

        }

        catch (Exception e)

        {

            DebugTool.Warning($"ScratchingProgressSO 불러오기 실패: {e.Message}", DebugType.ScratchingTime);

        }

    }





    // ScratchingSo 진행 상태를 ScratchingProgressSO에 담아 서버에 저장

    private async void SaveScratchingProgressToServer()

    {

        if (_scratchingProgress == null || _scratching == null)

            return;



        _scratchingProgress.CaptureFrom(_scratching);



        try

        {

            await _scratchingProgress.UpdateDataAsync();

        }

        catch (Exception e)

        {

            DebugTool.Warning($"ScratchingProgressSO 저장 실패: {e.Message}", DebugType.ScratchingTime);

        }

    }





    // LocalDataAccess.Game.OnReady 구독 (다른 시트 로드 완료 대기용)

    private void SubscribeDataReady()

    {

        if (LocalDataAccess.Instance == null || _isWaitingForDataReady)

            return;



        LocalDataAccess.Instance.Game.OnReady += HandleDataReady;

        _isWaitingForDataReady = true;

    }





    // OnReady 구독 해제

    private void UnsubscribeDataReady()

    {

        if (LocalDataAccess.Instance != null && _isWaitingForDataReady)

            LocalDataAccess.Instance.Game.OnReady -= HandleDataReady;



        _isWaitingForDataReady = false;

    }





    // 전역 데이터 준비 완료 시 스크래칭 데이터 로드 후처리 호출

    private void HandleDataReady()

    {

        if (!IsRequiredDataReady())

            return;



        OnScratchingDataLoaded();

    }

}

