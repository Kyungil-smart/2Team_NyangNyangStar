using System.Collections;
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

        private IEnumerator Start()
        {
            yield return WaitUntilReady();
            yield return LoadFromServerRoutine();
        }

        private IEnumerator WaitUntilReady()
        {
            while (_boardSystem == null || !_boardSystem.IsBoardReady)
                yield return new WaitForSeconds(_checkInterval);

            while (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
                yield return new WaitForSeconds(_checkInterval);

            while (LocalDataAccess.Instance == null ||
                   LocalDataAccess.Instance.Game == null ||
                   !LocalDataAccess.Instance.Game.IsReady)
            {
                yield return new WaitForSeconds(_checkInterval);
            }
        }

        private IEnumerator LoadFromServerRoutine()
        {
            if (_isLoading || _isLoaded)
                yield break;

            _isLoading = true;

            Task loadTask = LoadFromServerAsync();

            while (!loadTask.IsCompleted)
                yield return null;

            if (loadTask.Exception != null)
                Debug.LogException(loadTask.Exception);

            _isLoading = false;
            _isLoaded = true;

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