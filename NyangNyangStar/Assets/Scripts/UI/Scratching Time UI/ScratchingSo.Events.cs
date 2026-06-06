using System;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        // 지정한 스테이지의 내구도 변경 이벤트에 리스너를 등록
        public void AddDamagedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged += listener;
        }

        // 지정한 스테이지의 파괴 이벤트에 리스너를 등록
        public void AddDestroyedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed += listener;
        }

        // 지정한 스테이지의 내구도 변경 이벤트 리스너를 모두 제거
        public void RemoveAllDamagedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged = null;
        }

        // 지정한 스테이지의 파괴 이벤트 리스너를 모두 제거
        public void RemoveAllDestroyedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed = null;
        }
    }
}
