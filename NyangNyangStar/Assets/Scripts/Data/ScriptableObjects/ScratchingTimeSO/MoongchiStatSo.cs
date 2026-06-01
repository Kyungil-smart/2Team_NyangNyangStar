using System;
using UnityEngine;
using Util;
using Random = UnityEngine.Random;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    [CreateAssetMenu(fileName = "MoongChiStat", menuName = "SO/Data/MoongChiStatSO", order = 0)]
    public class MoongchiStatSo : SoBase
    {
        [Header("뭉치의 스텟")]
        [SerializeField] private int _level = 1;
        public int Level
        {
            get => Math.Clamp(_level, 1, 50);
            set
            {
                _level = value;
                OnLevelChanged?.Invoke();
            }
        }
        public int MaxExp = 1000;
        [SerializeField] private int _currentExp;
        [SerializeField] private int _baseSharpness = 8;
        [SerializeField] private int _sharpnessIncreasePerLevel = 2;
        [SerializeField] private int _totalSharpness;
        [SerializeField] private int _baseCriticalChance = 5;
        [SerializeField] private int _totalCriticalChance;

        public Action OnLevelChanged;

        public override void Init()
        {
            OnLevelChanged += LevelUp;
        }

        private void LevelUp()
        {
            _currentExp -= MaxExp;
            
            Level++;

            _totalSharpness = _baseSharpness + (_level * _sharpnessIncreasePerLevel);
            _totalCriticalChance = _baseCriticalChance + (_level%5 * _totalSharpness);
            
            DebugTool.Log($"[Level Up] 레벨 : {Level} " +
                          $"| 공격력 : {_totalSharpness} " +
                          $"| 힘껏 긁기 확률(%) : {_totalCriticalChance}", DebugType.Data);
        }
        
        public int GetSharpness()
            => _totalSharpness;
        
        public int GetCriticalChance()
            => _totalCriticalChance;

        public int GetCurrentExp()
            => _currentExp;

        public int Attack()
        {
            int random = Random.Range(0, 31);
            int value = _totalSharpness * Math.Clamp((100 - random) / 100, 0, 30);
            
            int critChance = Random.Range(0, 100);

            if (critChance <= _totalCriticalChance)
            {
                DebugTool.Log($"[뭉치의 힘껏 긁기!] 확률 : {_totalCriticalChance} " +
                              $"| 데미지 : {_totalSharpness}", DebugType.Data);
                value *= 2;
                return value;
            }
            
            DebugTool.Log($"[뭉치의 일반 공격] 데미지 : {_totalSharpness}", DebugType.Data);
            
            return value;
        }

        public void IncreaseExp(int expAmount)
        {
            if (expAmount < 0)
            {
                DebugTool.Warning($"획득 경험치 양 = {expAmount} 이 0보다 작습니다.", DebugType.Data);
                return;
            }
            _currentExp += expAmount;

            if (_currentExp >= MaxExp)
                LevelUp();
        }
    }
}