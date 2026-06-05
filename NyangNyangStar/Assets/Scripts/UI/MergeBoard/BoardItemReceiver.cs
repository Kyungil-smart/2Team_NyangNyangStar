using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Services.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class BoardItemReceiver : MonoBehaviour
    {
        public static BoardItemReceiver Instance { get; private set; }

        [Header("보드 시스템")]
        [SerializeField] private BoardSystem _boardSystem;

        [Header("특수 아이템 보드")]
        [SerializeField] private SpecialItemBoardSystem _specialItemBoardSystem;

        [Header("보상 큐")]
        [SerializeField] private BoardRewardQueue _rewardQueue;

        [Header("아이템 데이터베이스")]
        [SerializeField] private ItemDatabaseSo _itemDatabase;

        [Header("테스트 아이템 생성")]
        [SerializeField] private TMP_InputField _itemIdInputField;
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

            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

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

        public void RegisterSpecialItemBoardSystem(SpecialItemBoardSystem specialItemBoardSystem)
        {
            _specialItemBoardSystem = specialItemBoardSystem;
        }

        public void RegisterRewardQueue(BoardRewardQueue rewardQueue)
        {
            _rewardQueue = rewardQueue;
        }

        public void RegisterItemDatabase(ItemDatabaseSo itemDatabase)
        {
            _itemDatabase = itemDatabase;
        }

        public void ReceiveItemById(int itemID)
        {
            ReceiveItemById(itemID, 1);
        }

        public void ReceiveItemById(int itemID, int count)
        {
            if (itemID <= 0)
            {
                DebugTool.Warning($"유효하지 않은 아이템 ID입니다. ID: {itemID}", DebugType.Board, this);
                return;
            }

            if (!TryGetItemDataById(itemID, out ItemData itemData))
            {
                DebugTool.Warning($"{itemID} ID에 해당하는 아이템 데이터를 찾을 수 없습니다.", DebugType.Board, this);
                return;
            }

            ReceiveItem(itemData, count);
        }

        public async void ReceiveItem(ItemData itemData)
        {
            await ReceiveItemAsync(itemData, 1);
        }

        public async void ReceiveItem(ItemData itemData, int count)
        {
            await ReceiveItemAsync(itemData, count);
        }

        private async System.Threading.Tasks.Task<bool> ReceiveItemAsync(ItemData itemData, int count)
        {
            if (itemData == null || !itemData.HasItem)
            {
                DebugTool.Warning("유효하지 않은 아이템 데이터입니다.", DebugType.Board, this);
                return false;
            }

            int safeCount = Mathf.Max(1, count);

            if (itemData.ItemType == ItemType.Special)
                return await ReceiveSpecialItemAsync(itemData, safeCount);

            if (itemData.ItemType != ItemType.Common)
            {
                DebugTool.Warning($"지원하지 않는 아이템 타입입니다. Type: {itemData.ItemType}", DebugType.Board, this);
                return false;
            }

            if (_rewardQueue == null)
                _rewardQueue = FindFirstObjectByType<BoardRewardQueue>();

            if (_rewardQueue == null)
            {
                DebugTool.Warning("BoardRewardQueue가 등록되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            bool result = await _rewardQueue.EnqueueItemAsync(itemData, safeCount);

            if (!result)
                DebugTool.Warning("보상 큐에 아이템을 추가하지 못했습니다.", DebugType.Board, this);

            return result;
        }

        private async System.Threading.Tasks.Task<bool> ReceiveSpecialItemAsync(ItemData itemData, int count)
        {
            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

            if (_specialItemBoardSystem == null)
            {
                DebugTool.Warning("SpecialItemBoardSystem이 등록되지 않았습니다.", DebugType.Board, this);
                return false;
            }

            bool result = await _specialItemBoardSystem.TryAddSpecialItemAsync(itemData, count);

            if (!result)
                DebugTool.Warning("특수 아이템 보드에 아이템을 추가하지 못했습니다.", DebugType.Board, this);

            return result;
        }

        public void ReceiveRandomTestItem()
        {
            if (!CanReceiveTestItem())
                return;

            if (TryReadInputItemID(out int itemID, out bool hasInput))
            {
                ReceiveItemById(itemID);
                return;
            }

            if (hasInput)
                return;

            if (!TryGetRandomCommonItem(out ItemData itemData))
            {
                DebugTool.Warning("생성 가능한 Common 아이템 데이터가 없습니다.", DebugType.Board, this);
                return;
            }

            ReceiveItem(itemData);
        }

        private bool CanReceiveTestItem()
        {
            if (FireStoreManager.Instance == null || !FireStoreManager.Instance.IsInitialized)
            {
                DebugTool.Warning("Firestore 초기화가 완료되지 않았습니다. 로그인 후 다시 시도하세요.", DebugType.Board, this);
                return false;
            }

            if (_boardSystem != null && !_boardSystem.IsServerDataLoaded)
            {
                DebugTool.Warning("보드 서버 데이터 로드 완료 전입니다.", DebugType.Board, this);
                return false;
            }

            if (_rewardQueue != null && !_rewardQueue.IsLoaded)
            {
                DebugTool.Warning("보상 큐 서버 데이터 로드 완료 전입니다.", DebugType.Board, this);
                return false;
            }

            return true;
        }

        private bool TryReadInputItemID(out int itemID, out bool hasInput)
        {
            itemID = 0;
            hasInput = false;

            if (_itemIdInputField == null)
                return false;

            string input = _itemIdInputField.text?.Trim();

            if (string.IsNullOrEmpty(input))
                return false;

            hasInput = true;

            if (!int.TryParse(input, out itemID) || itemID <= 0)
            {
                DebugTool.Warning($"아이템 ID 입력값이 올바르지 않습니다. 입력값: {input}", DebugType.Board, this);
                return false;
            }

            return true;
        }

        private bool TryGetRandomCommonItem(out ItemData itemData)
        {
            itemData = null;

            if (_itemDatabase != null && _itemDatabase.TryGetRandomItem(ItemType.Common, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetRandomMergeBoardItem(ItemType.Common, out itemData))
            {
                return true;
            }

            return false;
        }

        private bool TryGetItemDataById(int itemID, out ItemData itemData)
        {
            itemData = null;

            if (_itemDatabase != null && _itemDatabase.TryGetItemById(itemID, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemID, out itemData))
                return true;

            return false;
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
