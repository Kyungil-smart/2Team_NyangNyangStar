using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "MoongChiStat", menuName = "SO/Data/MoongChiStatSO", order = 0)]
    public class MoongchiStatSo : SoBase
    {
        [Header("뭉치의 스텟")] [SerializeField] private int _level = 1;

        
        // 뭉치의 현재 레벨을 반환하거나 설정
        // 레벨은 1부터 50 
        public int Level
        {
            get => Math.Clamp(_level, 1, 50);
            set
            {
                _level = value;
                OnLevelChanged?.Invoke();
            }
        }

        /// <summary>
        /// 레벨업에 필요한 최대 경험치입니다.
        /// </summary>
        public int MaxExp = 1000;

        [SerializeField] private int _currentExp;

        /// <summary>
        /// 뭉치의 현재 경험치를 반환하거나 설정합니다.
        /// 경험치가 변경되면 경험치 변경 이벤트를 호출합니다.
        /// </summary>
        public int CurrentExp
        {
            get => _currentExp;
            set
            {
                _currentExp = Math.Max(value, 0);
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

        [Header("총 힘껏 긁기 확률 % (자동 계산)")] [SerializeField]
        private int _totalCriticalChance;

        /// <summary>
        /// 레벨이 변경될 때 호출되는 이벤트입니다.
        /// </summary>
        public Action OnLevelChanged;

        /// <summary>
        /// 경험치가 변경될 때 호출되는 이벤트입니다.
        /// </summary>
        public Action OnExpChanged;

        /// <summary>
        /// 뭉치의 스탯 데이터를 초기화합니다.
        /// </summary>
        public override void Init()
        {
            Level = 1;
            _currentExp = 0;
            RefreshTotalStats();
        }

        public void SetProgressData(int level, int currentExp)
        {
            _level = Mathf.Clamp(level, 1, 50);
            _currentExp = Math.Max(currentExp, 0);
            RefreshTotalStats();
            OnLevelChanged?.Invoke();
            OnExpChanged?.Invoke();
        }

        /// <summary>
        /// 현재 경험치를 차감하고 레벨을 증가시킨 뒤, 예리도와 치명타 확률을 갱신합니다.
        /// </summary>
        private void LevelUp()
        {
            CurrentExp -= MaxExp;

            Level++;
            RefreshTotalStats();

            DebugTool.Log($"[Level Up] 레벨 : {Level} " +
                          $"| 공격력 : {_totalSharpness} " +
                          $"| 힘껏 긁기 확률(%) : {_totalCriticalChance}", DebugType.Data);
        }

        /// <summary>
        /// 뭉치의 현재 총 예리도를 반환합니다.
        /// </summary>
        /// <returns>현재 총 예리도</returns>
        public int GetSharpness()
            => _totalSharpness;

        /// <summary>
        /// 뭉치의 현재 치명타 확률을 반환합니다.
        /// </summary>
        /// <returns>현재 치명타 확률</returns>
        public int GetCriticalChance()
            => _totalCriticalChance;

        /// <summary>
        /// 뭉치의 현재 경험치를 반환합니다.
        /// </summary>
        /// <returns>현재 경험치</returns>
        public int GetCurrentExp()
            => _currentExp;

        /// <summary>
        /// 현재 레벨 구간 경험치 비율(0~1)을 반환합니다. MaxExp는 1000 고정 기준입니다.
        /// </summary>
        public float GetExpFillRatio()
        {
            if (MaxExp <= 0)
                return 0f;

            return Mathf.Clamp01((float)_currentExp / MaxExp);
        }

        private void RefreshTotalStats()
        {
            _totalSharpness = _baseSharpness + (_level * _sharpnessIncreasePerLevel);
            _totalCriticalChance = _baseCriticalChance + (_level / 5 * _baseCriticalChance);
        }

        /// <summary>
        /// 뭉치의 공격 데미지를 계산하여 반환합니다.
        /// 치명타 확률에 따라 힘껏 긁기 공격이 발생하면 데미지가 2배로 증가합니다.
        /// </summary>
        /// <returns>계산된 공격 데미지</returns>
        public int Attack()
        {
            return Attack(out _);
        }

        /// <summary>
        /// 뭉치의 공격 데미지와 크리티컬 여부를 함께 반환합니다.
        /// </summary>
        /// <param name="isCritical">힘껏 긁기(크리티컬) 발생 여부</param>
        /// <returns>계산된 공격 데미지</returns>
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

        /// <summary>
        /// 뭉치의 현재 경험치를 증가시킵니다.
        /// 증가한 경험치가 최대 경험치 이상이면 레벨업을 실행합니다.
        /// </summary>
        /// <param name="expAmount">증가시킬 경험치 양</param>
        public void IncreaseExp(int expAmount)
        {
            if (expAmount < 0)
            {
                DebugTool.Warning($"획득 경험치 양 = {expAmount} 이 0보다 작습니다.", DebugType.Data);
                return;
            }

            CurrentExp = _currentExp + expAmount;

            while (_currentExp >= MaxExp)
                LevelUp();
        }
    }
}
