using System.Collections;
using Data.LibrarySystem;
using System.Threading.Tasks;
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
        [SerializeField] private bool _normalizeDocumentIds = true;

        [Header("초기화 대기")]
        [SerializeField] private float _checkInterval = 0.1f;

        public bool IsLoaded { get; private set; }

        private IEnumerator Start()
        {
            IsLoaded = false;

            if (_boardSystem == null)
                _boardSystem = FindFirstObjectByType<BoardSystem>();

            if (_rewardQueue == null)
                _rewardQueue = FindFirstObjectByType<BoardRewardQueue>();

            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

            while (_boardSystem == null || !_boardSystem.IsBoardReady)
            {
                if (_boardSystem == null)
                    _boardSystem = FindFirstObjectByType<BoardSystem>();

                yield return new WaitForSeconds(_checkInterval);
            }

            while (_specialItemBoardSystem != null && !_specialItemBoardSystem.IsBoardReady)
                yield return new WaitForSeconds(_checkInterval);

            while (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
                yield return new WaitForSeconds(_checkInterval);

            while (LocalDataAccess.Instance == null || LocalDataAccess.Instance.Game == null || !LocalDataAccess.Instance.Game.IsReady)
                yield return new WaitForSeconds(_checkInterval);

            Task loadTask = LoadFromServerAsync();

            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.Exception != null)
            {
                Debug.LogException(loadTask.Exception);
                yield break;
            }

            IsLoaded = true;
            DebugTool.Log("MergeBoard 서버 데이터 로드 완료", DebugType.Board, this);
        }

        private async Task LoadFromServerAsync()
        {
            await _boardSystem.LoadBoardFromServerAsync(_normalizeDocumentIds);

            if (_rewardQueue != null)
                await _rewardQueue.LoadQueueFromServerAsync(_normalizeDocumentIds);

            if (_specialItemBoardSystem != null)
                await _specialItemBoardSystem.LoadSpecialBoardFromServerAsync(_normalizeDocumentIds);
        }
    }
}
