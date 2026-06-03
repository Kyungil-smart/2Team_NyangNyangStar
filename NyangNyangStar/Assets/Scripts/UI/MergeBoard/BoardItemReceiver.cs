using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardItemReceiver : MonoBehaviour
    {
        public static BoardItemReceiver Instance { get; private set; }

        [Header("보드 시스템")]
        [SerializeField] private BoardSystem _boardSystem;

        [Header("보상 큐")]
        [SerializeField] private BoardRewardQueue _rewardQueue;

        [Header("아이템 데이터베이스")]
        [SerializeField] private ItemDatabaseSo _itemDatabase;

        [Header("테스트 버튼")]
        [SerializeField] private Button _testReceiveButton;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_boardSystem == null)
                _boardSystem = FindFirstObjectByType<BoardSystem>();

            if (_rewardQueue == null)
                _rewardQueue = FindFirstObjectByType<BoardRewardQueue>();
        }

        private void Start()
        {
            if (_testReceiveButton != null)
                _testReceiveButton.onClick.AddListener(ReceiveRandomTestItem);
            else
                DebugTool.Warning("테스트 아이템 생성 버튼이 연결되지 않았습니다.", DebugType.Board, this);
        }

        public void RegisterBoardSystem(BoardSystem boardSystem)
        {
            _boardSystem = boardSystem;
        }

        public void RegisterRewardQueue(BoardRewardQueue rewardQueue)
        {
            _rewardQueue = rewardQueue;
        }

        public async void ReceiveItem(ItemData itemData)
        {
            if (itemData == null || !itemData.HasItem)
            {
                DebugTool.Warning("유효하지 않은 아이템 데이터입니다.", DebugType.Board, this);
                return;
            }

            if (_rewardQueue == null)
                _rewardQueue = FindFirstObjectByType<BoardRewardQueue>();

            if (_rewardQueue == null)
            {
                DebugTool.Warning("BoardRewardQueue가 등록되지 않았습니다.", DebugType.Board, this);
                return;
            }

            await _rewardQueue.EnqueueItemAsync(itemData);
        }

        public void ReceiveRandomTestItem()
        {
            if (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
            {
                DebugTool.Warning("Firestore 초기화가 완료되지 않았습니다. 로그인 후 다시 시도하세요.", DebugType.Board, this);
                return;
            }
            
            if (_itemDatabase == null)
            {
                DebugTool.Warning("ItemDatabaseSo가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            if (!_itemDatabase.TryGetRandomItem(ItemType.General, out ItemData itemData))
            {
                DebugTool.Warning("생성 가능한 General 아이템 데이터가 없습니다.", DebugType.Board, this);
                return;
            }

            ReceiveItem(itemData);
        }

        private void OnDestroy()
        {
            if (_testReceiveButton != null)
                _testReceiveButton.onClick.RemoveListener(ReceiveRandomTestItem);

            if (Instance == this)
                Instance = null;
        }
    }
}
