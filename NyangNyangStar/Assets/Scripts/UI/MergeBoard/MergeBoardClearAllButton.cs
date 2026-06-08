using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class MergeBoardClearAllButton : MonoBehaviour
    {
        [Header("대상 보드")]
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private SpecialItemBoardSystem _specialItemBoardSystem;
        [SerializeField] private BoardRewardQueue _boardRewardQueue;

        [Header("버튼")]
        [SerializeField] private Button _clearButton;

        private bool _isClearing;

        private void Awake()
        {
            if (_clearButton == null)
                _clearButton = GetComponent<Button>();

            if (_clearButton != null)
                _clearButton.onClick.AddListener(ClearAllItems);
        }

        private void OnDestroy()
        {
            if (_clearButton != null)
                _clearButton.onClick.RemoveListener(ClearAllItems);
        }

        public async void ClearAllItems()
        {
            if (_isClearing)
                return;

            _isClearing = true;

            if (_clearButton != null)
                _clearButton.interactable = false;

            try
            {
                await ClearAllItemsAsync();
            }
            finally
            {
                _isClearing = false;

                if (_clearButton != null)
                    _clearButton.interactable = true;
            }
        }

        private async Task ClearAllItemsAsync()
        {
            if (_boardSystem == null)
                _boardSystem = FindFirstObjectByType<BoardSystem>();

            if (_specialItemBoardSystem == null)
                _specialItemBoardSystem = FindFirstObjectByType<SpecialItemBoardSystem>();

            if (_boardRewardQueue == null)
                _boardRewardQueue = FindFirstObjectByType<BoardRewardQueue>();

            bool boardResult = true;
            bool specialResult = true;

            if (_boardSystem != null)
                boardResult = await _boardSystem.ClearAllItemsAsync();
            else
                DebugTool.Warning("BoardSystem이 연결되지 않아 일반 보드 전체 삭제를 건너뜁니다.", DebugType.Board, this);

            if (_specialItemBoardSystem != null)
                specialResult = await _specialItemBoardSystem.ClearAllItemsAsync();
            else
                DebugTool.Warning("SpecialItemBoardSystem이 연결되지 않아 특수 아이템 보드 전체 삭제를 건너뜁니다.", DebugType.Board, this);

            if (boardResult && specialResult)
            {
                DebugTool.Log("일반 보드와 특수 아이템 보드 전체 삭제 완료", DebugType.Board, this);

                if (_boardRewardQueue != null)
                    _boardRewardQueue.ShowAlert("모든 아이템 삭제됨");
                else
                    DebugTool.Warning("모든 아이템 삭제됨", DebugType.Board, this);
            }
        }
    }
}
