using Core.Managers;
using Data.ScriptableObjects.MergeBoard;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class MergeBoardController : MonoBehaviour
    {
        [Header("보드 창")]
        [SerializeField] private GameObject _boardRoot;
        [SerializeField] private MergeBoardLoader _mergeBoardLoader;
        [SerializeField] private Button _closeButton;

        [Header("옵션")]
        [SerializeField] private bool _reloadOnOpen;

        public bool IsOpen;

        private void OnEnable()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(CloseBoard);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseBoard);
        }

        public async void OpenBoard()
        {
            await OpenBoardAsync();
        }

        private async Task OpenBoardAsync()
        {
            if (_boardRoot == null)
            {
                DebugTool.Warning("보드 창 Root가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            _boardRoot.SetActive(true);
            IsOpen = true;

            await Task.Yield();

            if (_mergeBoardLoader == null)
                _mergeBoardLoader = _boardRoot.GetComponentInChildren<MergeBoardLoader>(true);

            if (_mergeBoardLoader == null)
            {
                DebugTool.Warning("MergeBoardLoader가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            await _mergeBoardLoader.LoadAsync(_reloadOnOpen);
        }

        public void CloseBoard()
        {
            if (global::MainUI.Instance == null)
            {
                DebugTool.Warning("MainUI Instance를 찾을 수 없습니다.", DebugType.UI, this);
                return;
            }

            global::MainUI.Instance.CloseMergeBoard();
        }
        
        public void SetVisible(bool isOpen)
        {
            IsOpen = isOpen;

            if (_boardRoot != null)
                _boardRoot.SetActive(isOpen);
        }

        public void ForceReloadOnNextOpen()
        {
            if (_mergeBoardLoader != null)
                _mergeBoardLoader.ResetLoadedState();
        }
    }
}
