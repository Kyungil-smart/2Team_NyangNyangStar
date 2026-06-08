using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "MoongChiStat", menuName = "SO/Data/MoongChiStatSO", order = 0)]
    public class MoongchiStatSo : SoBase
    {
        [Header("뭉치의 스텟")]
        [SerializeField] private int _level = 1;
        [SerializeField] private int _currentExp;

        // 서버 / 게임플레이 기준 런타임 진행도
        private int _runtimeLevel = 1;
        private int _runtimeExp;

        // 뭉치의 현재 레벨을 반환하거나 설정
        // 레벨은 1부터 50
        public int Level
        {
            get => Math.Clamp(_runtimeLevel, 1, 50);
            set
            {
                _runtimeLevel = Mathf.Clamp(value, 1, 50);
                SyncInspectorPreview();
                OnLevelChanged?.Invoke();
            }
        }

        // 레벨업에 필요한 최대 경험치
        public int MaxExp = 1000;

        // 뭉치의 현재 경험치를 반환하거나 설정, 변경 시 OnExpChanged 호출
        public int CurrentExp
        {
            get => _runtimeExp;
            set
            {
                _runtimeExp = Math.Max(value, 0);
                SyncInspectorPreview();
                OnExpChanged?.Invoke();
            }
        }

        [Header("기본 예리도 (레벨 1)")] [SerializeField]
        private int _baseSharpness = 8;

        [Header("레벨당 예리도 증가량")] [SerializeField]
        private int _sharpnessIncreasePerLevel = 2;

        [Header("총 예리도 (자동 계산)")] [SerializeField]
        private int _totalSharpness;

        [Header("기본 힘껏 긁기 확률 %")] [SerializeField]
        private int _baseCriticalChance = 5;

        [Header("5레벨마다 힘껏 긁기 확률 증가량 %")] [SerializeField]
        private int _criticalChanceIncreasePerFiveLevels = 5;

        [Header("총 힘껏 긁기 확률 % (자동 계산)")] [SerializeField]
        private int _totalCriticalChance;

        // 레벨이 변경될 때 호출
        public Action OnLevelChanged;

        // 경험치가 변경될 때 호출
        public Action OnExpChanged;

        // 뭉치 스탯 데이터 초기화
        public override void Init()
        {
            _runtimeLevel = 1;
            _runtimeExp = 0;
            RefreshTotalStats();
            SyncInspectorPreview();
            OnLevelChanged?.Invoke();
            OnExpChanged?.Invoke();
        }

        public void SetProgressData(int level, int currentExp)
        {
            _runtimeLevel = Mathf.Clamp(level, 1, 50);
            _runtimeExp = Math.Max(currentExp, 0);
            RefreshTotalStats();
            SyncInspectorPreview();
            OnLevelChanged?.Invoke();
            OnExpChanged?.Invoke();
        }

        // 경험치 차감 후 레벨업, 예리도와 치명타 확률 갱신
        private void LevelUp()
        {
            if (Level >= 50)
                return;

            CurrentExp -= MaxExp;

            Level++;
            RefreshTotalStats();

            DebugTool.Log($"[Level Up] 레벨 : {Level} " +
                          $"| 공격력 : {_totalSharpness} " +
                          $"| 힘껏 긁기 확률(%) : {_totalCriticalChance}", DebugType.Data);
        }

        // 현재 총 예리도 반환
        public int GetSharpness()
            => _totalSharpness;

        // 현재 치명타 확률 반환
        public int GetCriticalChance()
            => _totalCriticalChance;

        // 현재 경험치 반환
        public int GetCurrentExp()
            => _runtimeExp;

        // 현재 레벨 구간 경험치 비율(0~1), MaxExp 1000 고정 기준
        public float GetExpFillRatio()
        {
            if (MaxExp <= 0)
                return 0f;

            return Mathf.Clamp01((float)_runtimeExp / MaxExp);
        }

        private void RefreshTotalStats()
        {
            _totalSharpness = _baseSharpness + (_runtimeLevel * _sharpnessIncreasePerLevel);
            _totalCriticalChance = _baseCriticalChance + (_runtimeLevel / 5 * _criticalChanceIncreasePerFiveLevels);
        }

        // 인스pector 표시용 직렬화 필드를 런타임 진행도와 맞춤 (플레이 중 수동 수정값은 계산에 반영되지 않음)
        private void SyncInspectorPreview()
        {
            _level = _runtimeLevel;
            _currentExp = _runtimeExp;
        }

        // 공격 데미지 계산, 힘껏 긁기 시 데미지 2배
        public int Attack()
        {
            return Attack(out _);
        }

        // 공격 데미지와 크리티컬 여부 함께 반환
        public int Attack(out bool isCritical)
        {
            isCritical = false;

            int random = Random.Range(0, 31);
            float magnification = Mathf.Clamp((100 - random) / 100f, 0, 30);

            int value = (int)(_totalSharpness * magnification);

            int critChance = Random.Range(0, 100);

            if (critChance <= _totalCriticalChance)
            {
                isCritical = true;
                value *= 2;
                DebugTool.Log($"[뭉치의 힘껏 긁기!] 확률 : {_totalCriticalChance} " +
                              $"| 데미지 : {value}", DebugType.Data);
                return value;
            }

            DebugTool.Log($"[뭉치의 일반 공격] 데미지 : {value}", DebugType.Data);

            return value;
        }

        // 경험치 증가, 최대 경험치 이상이면 레벨업
        public void IncreaseExp(int expAmount)
        {
            if (expAmount < 0)
            {
                DebugTool.Warning($"획득 경험치 양 = {expAmount} 이 0보다 작습니다.", DebugType.Data);
                return;
            }

            CurrentExp = _runtimeExp + expAmount;

            while (_runtimeExp >= MaxExp)
                LevelUp();
        }
    }
}
