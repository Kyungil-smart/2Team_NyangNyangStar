using System.Threading.Tasks;
using Data.LibrarySystem;
using UI.MergeBoard;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    public class MergeBoardLoader : MonoBehaviour
    {
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private BoardRewardQueue _rewardQueue;
        [SerializeField] private SpecialItemBoardSystem _specialItemBoardSystem;

        [Header("문서 ID 정리")]
        [SerializeField] private bool _normalizeDocumentIds;

        [Header("초기화 대기")]
        [SerializeField] private float _checkInterval = 0.1f;

        private bool _isLoading;
        private bool _isLoaded;

        public bool IsLoading => _isLoading;
        public bool IsLoaded => _isLoaded;

        public async Task LoadAsync(bool forceReload = false)
        {
            if (_isLoading)
                return;

            if (_isLoaded && !forceReload)
                return;

            _isLoading = true;

            try
            {
                await WaitUntilReadyAsync();
                await LoadFromServerAsync();

                _isLoaded = true;
                DebugTool.Log("MergeBoard 서버 데이터 로드 완료", DebugType.Board, this);
            }
            catch (System.Exception exception)
            {
                _isLoaded = false;
                Debug.LogException(exception, this);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public async Task ReloadAsync()
        {
            await LoadAsync(true);
        }

        public void ResetLoadedState()
        {
            _isLoaded = false;
        }

        private async Task WaitUntilReadyAsync()
        {
            int delayMilliseconds = Mathf.Max(1, Mathf.RoundToInt(_checkInterval * 1000f));

            ResolveReferences();

            while (_boardSystem == null || !_boardSystem.IsBoardReady)
            {
                ResolveReferences();
                await Task.Delay(delayMilliseconds);
            }

            while (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
                await Task.Delay(delayMilliseconds);

            while (LocalDataAccess.Instance == null ||
                   LocalDataAccess.Instance.Game == null ||
                   !LocalDataAccess.Instance.Game.IsReady)
            {
                await Task.Delay(delayMilliseconds);
            }

            while (_specialItemBoardSystem != null && !_specialItemBoardSystem.IsBoardReady)
                await Task.Delay(delayMilliseconds);
        }

        private void ResolveReferences()
        {
            if (_boardSystem == null)
                _boardSystem = GetComponentInChildren<BoardSystem>(true);

            if (_rewardQueue == null)
                _rewardQueue = GetComponentInChildren<BoardRewardQueue>(true);

            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = GetComponentInChildren<SpecialItemBoardSystem>(true);
        }

        private async Task LoadFromServerAsync()
        {
            ResolveReferences();

            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            await _boardSystem.LoadBoardFromServerAsync(_normalizeDocumentIds);

            if (_rewardQueue != null)
                await _rewardQueue.LoadQueueFromServerAsync(_normalizeDocumentIds);

            if (_specialItemBoardSystem != null)
                await _specialItemBoardSystem.LoadSpecialBoardFromServerAsync(_normalizeDocumentIds);
        }
    }
}
