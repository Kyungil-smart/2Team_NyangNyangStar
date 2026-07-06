using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    public sealed class NyangQuariumClearAllButton : MonoBehaviour
    {
        private Button _button;
        private NyangQuariumItemBoard _board;
        private bool _isClearing;

        private void Awake()
        {
            BindButton();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(ClearAllItems);
        }

        public void Init(NyangQuariumItemBoard board)
        {
            _board = board;
            BindButton();
        }

        private void BindButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null)
                return;

            _button.onClick.RemoveListener(ClearAllItems);
            _button.onClick.AddListener(ClearAllItems);
        }

        private async void ClearAllItems()
        {
            if (_isClearing || _board == null)
                return;

            _isClearing = true;

            if (_button != null)
                _button.interactable = false;

            try
            {
                await ClearAllItemsAsync();
            }
            finally
            {
                _isClearing = false;

                if (_button != null)
                    _button.interactable = true;
            }
        }

        private async Task ClearAllItemsAsync()
        {
            bool result = await _board.ClearAllItemsAsync();
            if (!result)
                DebugTool.Warning("[NyangQuariumClearAllButton] 냥쿠아리움 머지보드 전체 삭제 실패", DebugType.Board, this);
        }
    }
}
