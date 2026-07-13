using Core.Managers;
using Data.ScriptableObjects.MergeBoard;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MergeBoard
{
    public class MergeBoardController : MonoBehaviour
    {
        private const string DefaultButtonSfxKey = "Main_SFX_Touch";

        [Header("보드 창")]
        [SerializeField] private GameObject _boardRoot;
        [SerializeField] private MergeBoardLoader _mergeBoardLoader;
        [SerializeField] private BoardSystem _boardSystem;
        [SerializeField] private BoardRewardQueue _boardRewardQueue;
        [SerializeField] private PlayerResourceDisplay _resourceDisplay;
        [SerializeField] private Button _closeButton;
        [SerializeField] private string _buttonSfxKey = DefaultButtonSfxKey;

        [Header("옵션")]
        [SerializeField] private bool _reloadOnOpen;

        public bool IsOpen;

        private readonly List<Button> _sfxBoundButtons = new();

        private string ButtonSfxKey => string.IsNullOrWhiteSpace(_buttonSfxKey) ? DefaultButtonSfxKey : _buttonSfxKey;

        private void OnEnable()
        {
            BindButtonSfx();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(CloseBoard);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(CloseBoard);

            UnbindButtonSfx();
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

            ApplyBoardRootVisibility(true);
            BindButtonSfx();
            RefreshBoardLayout();
            IsOpen = true;
            EnsureResourceDisplay();

            await Task.Yield();
            RefreshBoardLayout();

            if (_mergeBoardLoader == null)
                _mergeBoardLoader = _boardRoot.GetComponentInChildren<MergeBoardLoader>(true);

            CacheRewardQueue();
            _boardRewardQueue?.ClearAlert();

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
            CacheRewardQueue();
            _boardRewardQueue?.ClearAlert();

            IsOpen = isOpen;

            ApplyBoardRootVisibility(isOpen);

            if (isOpen)
            {
                RefreshBoardLayout();
                EnsureResourceDisplay();
                _boardRewardQueue?.ClearAlert();
            }
        }

        private void ApplyBoardRootVisibility(bool isOpen)
        {
            if (_boardRoot == null)
                return;

            _boardRoot.transform.localScale = isOpen ? Vector3.one : Vector3.zero;
            _boardRoot.SetActive(isOpen);
        }

        private void RefreshBoardLayout()
        {
            if (_boardSystem == null && _boardRoot != null)
                _boardSystem = _boardRoot.GetComponentInChildren<BoardSystem>(true);

            _boardSystem?.RefreshResponsiveLayout();
        }

        private void CacheRewardQueue()
        {
            if (_boardRewardQueue != null || _boardRoot == null)
                return;

            _boardRewardQueue = _boardRoot.GetComponentInChildren<BoardRewardQueue>(true);
        }

        private void BindButtonSfx()
        {
            if (_boardRoot == null)
                return;

            Button[] buttons = _boardRoot.GetComponentsInChildren<Button>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button == null || _sfxBoundButtons.Contains(button))
                    continue;

                button.onClick.AddListener(PlayButtonSfx);
                _sfxBoundButtons.Add(button);
            }
        }

        private void UnbindButtonSfx()
        {
            for (int i = 0; i < _sfxBoundButtons.Count; i++)
            {
                Button button = _sfxBoundButtons[i];

                if (button != null)
                    button.onClick.RemoveListener(PlayButtonSfx);
            }

            _sfxBoundButtons.Clear();
        }

        private void PlayButtonSfx()
        {
            if (GameManager.Audio == null)
                return;

            GameManager.Audio.PlaySfx(ButtonSfxKey);
        }

        public void ForceReloadOnNextOpen()
        {
            if (_mergeBoardLoader != null)
                _mergeBoardLoader.ResetLoadedState();
        }

        private void EnsureResourceDisplay()
        {
            if (_resourceDisplay == null && _boardRoot != null)
                _resourceDisplay = _boardRoot.GetComponentInChildren<PlayerResourceDisplay>(true);

            if (_resourceDisplay == null)
                _resourceDisplay = GetComponentInChildren<PlayerResourceDisplay>(true);

            if (_resourceDisplay == null)
                return;

            _resourceDisplay.ResolveReferencesFrom(_resourceDisplay.transform);
            _resourceDisplay.RefreshDisplay();
            _ = PlayerResourceManager.Instance.RefreshAsync();
        }
    }
}
