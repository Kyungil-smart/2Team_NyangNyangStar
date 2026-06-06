using System.Threading.Tasks;
using Data.ScriptableObjects.MergeBoard;
using UI.MainUI;
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

        private UpDownScreenController _upDownCon;

        public bool IsOpen;

        private void OnEnable()
        {
            _closeButton.onClick.AddListener(CloseBoard);
        }

        private void Start()
        {
            _upDownCon = FindObjectOfType<UpDownScreenController>();
        }

        private void OnDisable()
        {
            _closeButton.onClick.RemoveListener(CloseBoard);
        }

        public async void OpenBoard()
        {
            await OpenBoardAsync();
        }

        private async Task OpenBoardAsync()
        {
            if (IsOpen)
                return;

            if (_boardRoot == null)
            {
                DebugTool.Warning("보드 창 Root가 연결되지 않았습니다.", DebugType.Board, this);
                return;
            }

            IsOpen = false;

            try
            {
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
            finally
            {
                IsOpen = false;
            }
        }

        public void CloseBoard()
        {
            if (_boardRoot == null)
                return;
            
            if(_upDownCon == null)
                _upDownCon = FindObjectOfType<UpDownScreenController>();
            _upDownCon.DownAnimation();
        }

        public void ForceReloadOnNextOpen()
        {
            if (_mergeBoardLoader != null)
                _mergeBoardLoader.ResetLoadedState();
        }
    }
}
