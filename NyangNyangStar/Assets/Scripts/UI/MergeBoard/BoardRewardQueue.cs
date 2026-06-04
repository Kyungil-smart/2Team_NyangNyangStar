using Data.ScriptableObjects.MergeBoard;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UI.MergeBoard
{
    public class BoardRewardQueue : MonoBehaviour
    {
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private MergeBoardFirestoreSo _mergeBoardFirestore;
        [SerializeField] private List<RewardQueueSlotView> _queueSlotViews = new();
        [SerializeField] private TMP_Text _countText;

        [Header("경고 메시지")]
        [SerializeField] private TMP_Text _alertText;
        [SerializeField] private float _alertDuration = 1.5f;

        private Coroutine _alertCoroutine;

        private readonly Queue<ItemData> _rewardQueue = new();

        private const int PreviewCount = 3;

        public bool IsLoaded { get; private set; }

        private void Start()
        {
            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (_queueSlotViews[i] == null)
                    continue;

                bool isTopSlot = i == 0;
                _queueSlotViews[i].Init(this, isTopSlot);
            }

            if (_alertText != null)
                _alertText.gameObject.SetActive(false);

            RefreshView();

            if (BoardItemReceiver.Instance != null)
                BoardItemReceiver.Instance.RegisterRewardQueue(this);
        }

        public Task<bool> EnqueueItemAsync(ItemData itemData)
        {
            return EnqueueItemAsync(itemData, 1);
        }

        public async Task<bool> EnqueueItemAsync(ItemData itemData, int count)
        {
            if (!IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 추가할 수 없습니다.", DebugType.Board, this);
                return false;
            }

            if (itemData == null || !itemData.HasItem)
                return false;

            if (itemData.ItemType != Services.Enums.ItemType.Common)
            {
                DebugTool.Warning("보상 큐에는 Common 타입 아이템만 추가할 수 있습니다.", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);

            for (int i = 0; i < safeCount; i++)
                _rewardQueue.Enqueue(itemData.Clone());

            RefreshView();

            if (_mergeBoardFirestore != null)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);

            return true;
        }

        public async void TryMoveTopItemToBoard()
        {
            if (!IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 전에는 아이템을 꺼낼 수 없습니다.", DebugType.Board, this);
                return;
            }

            if (_rewardQueue.Count <= 0)
                return;

            if (_boardSystem == null)
            {
                DebugTool.Warning("BoardSystem이 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            ItemData itemData = _rewardQueue.Peek();

            bool result = await _boardSystem.TryAddItemAsync(itemData);

            if (!result)
            {
                ShowAlert("보드판 공간이 부족합니다.");
                return;
            }

            _rewardQueue.Dequeue();

            RefreshView();

            if (_mergeBoardFirestore != null)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);
        }

        public async Task LoadQueueFromServerAsync(bool normalizeDocumentIds = false)
        {
            IsLoaded = false;

            if (_mergeBoardFirestore == null)
            {
                DebugTool.Warning("MergeBoardFirestoreSO가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            if (!_mergeBoardFirestore.IsReady)
            {
                DebugTool.Warning("Firestore가 초기화되지 않아 보상 큐를 불러올 수 없습니다.", DebugType.Board, this);
                return;
            }

            List<ItemData> loadedItems = await _mergeBoardFirestore.LoadRewardQueueAsync();

            _rewardQueue.Clear();

            for (int i = 0; i < loadedItems.Count; i++)
            {
                ItemData itemData = loadedItems[i];

                if (itemData == null || !itemData.HasItem)
                    continue;

                _rewardQueue.Enqueue(itemData.Clone());
            }

            RefreshView();

            if (normalizeDocumentIds)
                await _mergeBoardFirestore.SaveRewardQueueAsync(_rewardQueue);

            IsLoaded = true;
            DebugTool.Log("보상 큐 서버 데이터 로드 완료", DebugType.Board, this);
        }

        private void ShowAlert(string message)
        {
            if (_alertText == null)
            {
                DebugTool.Warning(message, DebugType.Board, this);
                return;
            }

            if (_alertCoroutine != null)
                StopCoroutine(_alertCoroutine);

            _alertCoroutine = StartCoroutine(AlertRoutine(message));
        }

        private IEnumerator AlertRoutine(string message)
        {
            _alertText.text = message;
            _alertText.gameObject.SetActive(true);

            yield return new WaitForSeconds(_alertDuration);

            _alertText.gameObject.SetActive(false);
            _alertCoroutine = null;
        }

        private void RefreshView()
        {
            List<ItemData> previewItems = new List<ItemData>(_rewardQueue);

            for (int i = 0; i < _queueSlotViews.Count; i++)
            {
                if (_queueSlotViews[i] == null)
                    continue;

                if (i < previewItems.Count && i < PreviewCount)
                    _queueSlotViews[i].SetItem(previewItems[i]);
                else
                    _queueSlotViews[i].SetItem(ItemData.Empty);
            }

            if (_countText != null)
                _countText.text = _rewardQueue.Count.ToString();
        }
    }
}
