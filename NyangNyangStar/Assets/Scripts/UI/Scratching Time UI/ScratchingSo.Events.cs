using System;

namespace Data.ScriptableObjects.ScratchingTimeSO
{
    public partial class ScratchingSo
    {
        /// <summary>
        /// 지정한 스테이지의 내구도 변경 이벤트에 리스너를 등록합니다.
        /// </summary>
        /// <param name="stage">리스너를 등록할 스테이지 값</param>
        /// <param name="listener">등록할 리스너</param>
        public void AddDamagedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged += listener;
        }

        /// <summary>
        /// 지정한 스테이지의 파괴 이벤트에 리스너를 등록합니다.
        /// </summary>
        /// <param name="stage">리스너를 등록할 스테이지 값</param>
        /// <param name="listener">등록할 리스너</param>
        public void AddDestroyedListener(int stage, Action listener)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed += listener;
        }

        /// <summary>
        /// 지정한 스테이지의 내구도 변경 이벤트 리스너를 모두 제거합니다.
        /// </summary>
        /// <param name="stage">리스너를 제거할 스테이지 값</param>
        public void RemoveAllDamagedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDamaged = null;
        }

        /// <summary>
        /// 지정한 스테이지의 파괴 이벤트 리스너를 모두 제거합니다.
        /// </summary>
        /// <param name="stage">리스너를 제거할 스테이지 값</param>
        public void RemoveAllDestroyedListener(int stage)
        {
            if (!IsContainsKey(stage))
                return;

            _dataDict[stage].OnScratcherDestroyed = null;
        }
    }
}
