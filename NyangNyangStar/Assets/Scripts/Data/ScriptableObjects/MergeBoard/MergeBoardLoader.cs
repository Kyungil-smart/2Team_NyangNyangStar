using System.Collections;
using System.Threading.Tasks;
using UI.MergeBoard;
using UnityEngine;

namespace Data.ScriptableObjects.MergeBoard
{
    public class MergeBoardLoader : MonoBehaviour
    {
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private BoardRewardQueue _rewardQueue;

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

            while (_boardSystem == null || !_boardSystem.IsBoardReady)
            {
                if (_boardSystem == null)
                    _boardSystem = FindFirstObjectByType<BoardSystem>();

                yield return new WaitForSeconds(_checkInterval);
            }

            while (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
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
        }
    }
}
