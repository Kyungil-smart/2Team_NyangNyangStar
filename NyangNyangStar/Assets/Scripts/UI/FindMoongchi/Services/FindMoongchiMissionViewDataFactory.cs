using System.Collections.Generic;
using Data.ScriptableObjects.MoongchiSO;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiMissionViewDataFactory
    {
        private readonly FindMoongchiDataManager _dataManager;
        private readonly FindMoongchiProgressController _progressController;
        private readonly bool _isProgressReady;
        private readonly bool _completedMissionMock;
        private readonly HashSet<int> _claimedMissionIds;

        public FindMoongchiMissionViewDataFactory(
            FindMoongchiDataManager dataManager,
            FindMoongchiProgressController progressController,
            bool isProgressReady,
            bool completedMissionMock,
            HashSet<int> claimedMissionIds)
        {
            _dataManager = dataManager;
            _progressController = progressController;
            _isProgressReady = isProgressReady;
            _completedMissionMock = completedMissionMock;
            _claimedMissionIds = claimedMissionIds;
        }

        public List<FindMoongchiMissionViewData> BuildDailyMissions()
        {
            return BuildByType(MoongchiMissionType.DAILY);
        }

        public List<FindMoongchiMissionViewData> BuildWeeklyMissions(int currentWeek)
        {
            List<FindMoongchiMissionViewData> result = new();
            AddByType(result, MoongchiMissionType.WEEKLY);

            if (currentWeek <= 1)
                AddByType(result, MoongchiMissionType.WEEKLY_1ST);
            else
                AddByType(result, MoongchiMissionType.WEEKLY_2ND);

            return result;
        }

        public List<FindMoongchiMissionViewData> BuildByType(MoongchiMissionType missionType)
        {
            List<FindMoongchiMissionViewData> result = new();
            AddByType(result, missionType);
            return result;
        }

        public FindMoongchiMissionViewData BuildById(int missionId)
        {
            if (_dataManager == null || !_dataManager.TryGetMission(missionId, out MoongchiMissionData mission) || mission == null)
                return null;

            return Build(mission);
        }

        public FindMoongchiMissionViewData Build(MoongchiMissionData mission)
        {
            if (mission == null)
                return null;

            int currentAmount;
            FindMoongchiMissionSlotState state;

            if (_isProgressReady && _progressController != null)
            {
                currentAmount = _progressController.GetMissionCurrentAmount(mission.ID);
                state = _progressController.GetMissionSlotState(mission);
            }
            else
            {
                currentAmount = _completedMissionMock ? mission.TargetAmount : 0;
                state = FindMoongchiMissionSlotState.InProgress;

                if (_claimedMissionIds != null && _claimedMissionIds.Contains(mission.ID))
                    state = FindMoongchiMissionSlotState.Claimed;
                else if (currentAmount >= mission.TargetAmount)
                    state = FindMoongchiMissionSlotState.Completed;
            }

            return new FindMoongchiMissionViewData
            {
                MissionId = mission.ID,
                MissionType = mission.MissionType,
                MissionDescription = mission.MissionContent,
                CurrentAmount = currentAmount,
                TargetAmount = mission.TargetAmount,
                State = state,
                Reward1 = mission.Reward1,
                Reward2 = mission.Reward2
            };
        }

        private void AddByType(List<FindMoongchiMissionViewData> result, MoongchiMissionType missionType)
        {
            if (result == null)
                return;

            if (_dataManager == null)
            {
                DebugTool.Warning($"[FindMoongchiMissionViewDataFactory] DataManager missing. Type={missionType}", DebugType.FindMoongchi);
                return;
            }

            IReadOnlyList<MoongchiMissionData> missions = _dataManager.GetMissionsByType(missionType);

            for (int i = 0; i < missions.Count; i++)
            {
                FindMoongchiMissionViewData data = Build(missions[i]);

                if (data != null)
                    result.Add(data);
            }
        }
    }
}
