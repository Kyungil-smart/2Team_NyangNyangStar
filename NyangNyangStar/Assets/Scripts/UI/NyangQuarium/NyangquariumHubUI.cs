using System;
using Core.Managers;
using DG.Tweening;
using UI.Base;
using UI.Transition;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumHubUI : UIPopup
    {
        [Header("허브 버튼")]
        [SerializeField] private Button _storyButton;
        [SerializeField] private Button _boardQuestButton;
        [SerializeField] private Button _collectionButton;
        [SerializeField] private Button _layoutButton;
        [SerializeField] private Button _aquariumButton;
        [SerializeField] private Button _backButton;

        [Header("콘텐츠 Addressables 주소")]
        [Tooltip("주소가 비어 있는 콘텐츠 버튼은 비활성화됩니다.")]
        [SerializeField] private string _storyKey;
        [SerializeField] private string _boardQuestKey;
        [SerializeField] private string _collectionKey;
        [SerializeField] private string _layoutKey;
        [SerializeField] private string _aquariumKey;

        [Header("연출")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private float _openDuration = 0.2f;
        [SerializeField] private float _openStartScale = 0.96f;
        [SerializeField] private bool _useScreenTransition = true;
        [SerializeField] private string _clickSfxKey = "Main_SFX_Touch";

        private UIPopup _activeContent;
        private bool _isTransitioning;

        public static NyangquariumHubUI Active { get; private set; }

        public override void Init()
        {
            Active = this;
            ResolveReferences();
            BindButtons();
            RefreshButtonStates();
        }

        public override void PlayOpenAnimation()
        {
            Active = this;
            ResolveAnimationReferences();

            _canvasGroup.DOKill();
            _contentRoot.DOKill();

            _canvasGroup.alpha = 0f;
            _contentRoot.localScale = Vector3.one * _openStartScale;

            _canvasGroup
                .DOFade(1f, _openDuration)
                .SetUpdate(true);

            _contentRoot
                .DOScale(Vector3.one, _openDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        public void OpenStory() => OpenContent(_storyKey);
        public void OpenBoardQuest() => OpenContent(_boardQuestKey);
        public void OpenCollection() => OpenContent(_collectionKey);
        public void OpenLayout() => OpenContent(_layoutKey);
        public void OpenAquarium() => OpenContent(_aquariumKey);

        public void ReturnToHub()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() =>
            {
                if (_activeContent != null)
                    GameManager.UI.ClosePopupUI(_activeContent);

                _activeContent = null;
                gameObject.SetActive(true);
                PlayOpenAnimation();
            });
        }

        public void CloseHub()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() => gameObject.SetActive(false));
        }

        private void OpenContent(string addressableKey)
        {
            if (_isTransitioning || string.IsNullOrWhiteSpace(addressableKey))
                return;

            PlayClickSfx();
            SetButtonsInteractable(false);
            _isTransitioning = true;

            void LoadContent()
            {
                KeyContainer.EnsurePrefabKey(addressableKey);

                GameManager.UI.ShowPopupUI<UIPopup>(
                    addressableKey,
                    popup =>
                    {
                        _activeContent = popup;
                        gameObject.SetActive(false);
                        popup.gameObject.SetActive(true);
                        popup.PlayOpenAnimation();
                        RevealAndUnlock();
                    },
                    false,
                    true,
                    failedKey =>
                    {
                        DebugTool.Warning(
                            $"[NyangquariumHubUI] 콘텐츠를 열지 못했습니다: {failedKey}",
                            DebugType.UI,
                            this);

                        RevealAndUnlock();
                    });
            }

            ScreenTransitionManager transition = GetTransition();

            if (transition == null)
            {
                LoadContent();
                return;
            }

            transition.Cover(LoadContent);
        }

        private void RunCoveredTransition(Action coveredAction)
        {
            SetButtonsInteractable(false);
            _isTransitioning = true;

            ScreenTransitionManager transition = GetTransition();

            if (transition == null)
            {
                coveredAction?.Invoke();
                Unlock();
                return;
            }

            transition.Cover(() =>
            {
                coveredAction?.Invoke();
                transition.Reveal(Unlock);
            });
        }

        private void RevealAndUnlock()
        {
            ScreenTransitionManager transition = GetTransition();

            if (transition == null)
            {
                Unlock();
                return;
            }

            transition.Reveal(Unlock);
        }

        private void Unlock()
        {
            _isTransitioning = false;
            SetButtonsInteractable(true);
            RefreshButtonStates();
        }

        private ScreenTransitionManager GetTransition()
            => _useScreenTransition ? ScreenTransitionManager.Instance : null;

        private void ResolveReferences()
        {
            _storyButton ??= FindButton("StoryButton", "StoryContentButton");
            _boardQuestButton ??= FindButton("BoardQuestButton", "MergeGameButton", "QuestButton");
            _collectionButton ??= FindButton("CollectionButton", "FishCollectionButton");
            _layoutButton ??= FindButton("LayoutButton", "AquariumLayoutButton");
            _aquariumButton ??= FindButton("AquariumButton", "WaterGazeButton");
            _backButton ??= FindButton("BackButton", "CloseButton", "ExitButton");
            ResolveAnimationReferences();
        }

        private void ResolveAnimationReferences()
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _contentRoot ??= transform as RectTransform;
        }

        private Button FindButton(params string[] names)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                foreach (string targetName in names)
                {
                    if (string.Equals(button.name, targetName, StringComparison.OrdinalIgnoreCase))
                        return button;
                }
            }

            return null;
        }

        private void BindButtons()
        {
            BindButton(_storyButton, OpenStory);
            BindButton(_boardQuestButton, OpenBoardQuest);
            BindButton(_collectionButton, OpenCollection);
            BindButton(_layoutButton, OpenLayout);
            BindButton(_aquariumButton, OpenAquarium);
            BindButton(_backButton, CloseHub);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void RefreshButtonStates()
        {
            SetRouteButtonState(_storyButton, _storyKey);
            SetRouteButtonState(_boardQuestButton, _boardQuestKey);
            SetRouteButtonState(_collectionButton, _collectionKey);
            SetRouteButtonState(_layoutButton, _layoutKey);
            SetRouteButtonState(_aquariumButton, _aquariumKey);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            SetInteractable(_storyButton, interactable);
            SetInteractable(_boardQuestButton, interactable);
            SetInteractable(_collectionButton, interactable);
            SetInteractable(_layoutButton, interactable);
            SetInteractable(_aquariumButton, interactable);
            SetInteractable(_backButton, interactable);
        }

        private static void SetRouteButtonState(Button button, string key)
        {
            if (button != null)
                button.interactable = !string.IsNullOrWhiteSpace(key);
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        private void PlayClickSfx()
        {
            if (!string.IsNullOrWhiteSpace(_clickSfxKey))
                GameManager.Audio.PlaySfx(_clickSfxKey);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Active, this))
                Active = null;

            _canvasGroup?.DOKill();
            _contentRoot?.DOKill();
        }
    }
}
