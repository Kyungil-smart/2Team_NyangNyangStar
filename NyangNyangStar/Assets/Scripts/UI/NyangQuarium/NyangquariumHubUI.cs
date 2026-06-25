using System;
using System.Collections.Generic;
using Core.Managers;
using DG.Tweening;
using UI.Base;
using UI.Transition;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumHubUI : UIPopup
    {
        [Header("허브 버튼")]
        [FormerlySerializedAs("_boardQuestButton")]
        [SerializeField] private Button _boardButton;
        [SerializeField] private Button _collectionButton;
        [SerializeField] private Button _layoutButton;
        [FormerlySerializedAs("_aquariumButton")]
        [SerializeField] private Button _waterGazeButton;
        [SerializeField] private Button _backButton;

        [Header("하위 콘텐츠 오브젝트 연결")]
        [Tooltip("기본 허브 버튼 묶음입니다. 비워두면 ContentButtons를 자동으로 찾습니다.")]
        [SerializeField] private GameObject _hubMenuRoot;
        [Tooltip("허브 버튼이 아니라 최초 진입 이벤트에서 OpenStory를 호출할 때 사용합니다.")]
        [SerializeField] private GameObject _storyContent;
        [FormerlySerializedAs("_boardQuestContent")]
        [SerializeField] private GameObject _boardContent;
        [SerializeField] private GameObject _collectionContent;
        [Tooltip("수조 레이아웃/물멍 버튼을 눌렀을 때 먼저 여는 담수/해수 선택 UI입니다.")]
        [SerializeField] private GameObject _aquariumSelectRoot;
        [Tooltip("담수 선택 시 열 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
        [SerializeField] private GameObject _freshAquariumContent;
        [Tooltip("해수 선택 시 열 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
        [SerializeField] private GameObject _oceanAquariumContent;

        [Header("수조 선택 버튼")]
        [SerializeField] private Button _freshAquariumButton;
        [SerializeField] private Button _oceanAquariumButton;

        [Header("기본 화면 Addressables 스프라이트")]
        [FormerlySerializedAs("_tankImage")]
        [SerializeField] private Image _backgroundImage;
        [FormerlySerializedAs("_tankSpriteKey")]
        [SerializeField] private string _backgroundSpriteKey = "NQ_BG_SaltWater";
        [SerializeField] private Image _titleLogoImage;
        [SerializeField] private string _titleLogoSpriteKey = "NQ_Img_Title";
        [SerializeField] private string _boardSpriteKey = "NQ_Btn_Mergeboard";
        [SerializeField] private string _collectionSpriteKey = "NQ_Btn_FishBook";
        [SerializeField] private string _layoutSpriteKey = "NQ_Btn_Tank";
        [SerializeField] private string _waterGazeSpriteKey = "NQ_Btn_AquaView";
        [SerializeField] private string _backSpriteKey = "NQ_Btn_Back";

        [Header("연출")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private float _openDuration = 0.2f;
        [SerializeField] private float _openStartScale = 0.96f;
        [SerializeField] private bool _useScreenTransition = true;
        [SerializeField] private string _clickSfxKey = "Main_SFX_Touch";

        private GameObject _activeContent;
        private bool _isTransitioning;
        private NyangquariumHubEntryMode _pendingAquariumEntryMode = NyangquariumHubEntryMode.Layout;
        private UISpriteController _backgroundSprite;
        private UISpriteController _titleLogoSprite;
        private UISpriteController _boardSprite;
        private UISpriteController _collectionSprite;
        private UISpriteController _layoutSprite;
        private UISpriteController _waterGazeSprite;
        private UISpriteController _backSprite;
        private readonly List<GameObject> _ownedContents = new();
        private readonly HashSet<UIPopup> _initializedChildPopups = new();

        public static NyangquariumHubUI Active { get; private set; }

        public override void Init()
        {
            Active = this;
            ResolveReferences();
            BindButtons();
            BindAquariumSelectButtons();
            BindAddressableSprites();
            ShowHubViewImmediately();
            RefreshButtonStates();
        }

        public override void PlayOpenAnimation()
        {
            Active = this;
            ResolveAnimationReferences();
            ShowHubViewImmediately();

            if (_canvasGroup == null || _contentRoot == null)
            {
                DebugTool.Warning(
                    "[NyangquariumHubUI] 오픈 연출에 필요한 CanvasGroup 또는 RectTransform을 찾지 못했습니다.",
                    DebugType.UI,
                    this);
                return;
            }

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

        public void ShowImmediately()
        {
            Active = this;
            gameObject.SetActive(true);
            ResolveAnimationReferences();

            _canvasGroup?.DOKill();
            _contentRoot?.DOKill();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (_contentRoot != null)
                _contentRoot.localScale = Vector3.one;

            ShowHubViewImmediately();
            RefreshButtonStates();
        }

        public void OpenStory() => OpenChildContent(_storyContent, NyangquariumHubEntryMode.Story);
        public void OpenBoard() => OpenChildContent(_boardContent, NyangquariumHubEntryMode.Board);
        public void OpenCollection() => OpenChildContent(_collectionContent, NyangquariumHubEntryMode.Collection);
        public void OpenLayout() => OpenAquariumSelect(NyangquariumHubEntryMode.Layout);
        public void OpenWaterGaze() => OpenAquariumSelect(NyangquariumHubEntryMode.WaterGaze);

        public void ReturnToHub()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() =>
            {
                ShowHubViewImmediately();
            });
        }

        public void RegisterOwnedContent(UIPopup popup, bool setActiveContent = true)
            => RegisterOwnedContent(popup != null ? popup.gameObject : null, setActiveContent);

        public void RegisterOwnedContent(GameObject content, bool setActiveContent = true)
        {
            if (content == null)
                return;

            _ownedContents.RemoveAll(item => item == null);

            if (!_ownedContents.Contains(content))
                _ownedContents.Add(content);

            if (setActiveContent)
                _activeContent = content;
        }

        public void CloseHub()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() =>
            {
                HideChildContents();
                SetHubMenuVisible(true);
                gameObject.SetActive(false);
            });
        }

        private void OpenAquariumSelect(NyangquariumHubEntryMode entryMode)
        {
            _pendingAquariumEntryMode = entryMode;
            OpenChildContent(_aquariumSelectRoot, entryMode);
        }

        public void OpenFreshAquarium()
            => OpenChildContent(_freshAquariumContent, _pendingAquariumEntryMode);

        public void OpenOceanAquarium()
            => OpenChildContent(_oceanAquariumContent, _pendingAquariumEntryMode);

        private void OpenChildContent(GameObject content, NyangquariumHubEntryMode entryMode)
        {
            if (_isTransitioning || content == null)
                return;

            PlayClickSfx();
            SetButtonsInteractable(false);
            _isTransitioning = true;
            ScreenTransitionManager transition = GetTransition();

            void ShowContent()
            {
                NyangquariumHubEntryContext.Set(entryMode);
                HideChildContents();
                SetHubMenuVisible(false);
                RegisterOwnedContent(content);
                InitializeChildContent(content);
                NotifyEntryMode(content, entryMode);

                if (transition == null)
                    ShowChildContentWithOpenAnimation(content);
                else
                    ShowChildContentImmediately(content);
            }

            if (transition == null)
            {
                ShowContent();
                Unlock();
                return;
            }

            transition.Cover(() =>
            {
                ShowContent();
                transition.Reveal(Unlock);
            });
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

        private void ShowHubViewImmediately()
        {
            gameObject.SetActive(true);
            HideChildContents();
            SetHubMenuVisible(true);
        }

        private void HideChildContents()
        {
            SetContentActive(_storyContent, false);
            SetContentActive(_boardContent, false);
            SetContentActive(_collectionContent, false);
            SetContentActive(_aquariumSelectRoot, false);
            SetContentActive(_freshAquariumContent, false);
            SetContentActive(_oceanAquariumContent, false);

            for (int i = _ownedContents.Count - 1; i >= 0; i--)
            {
                GameObject content = _ownedContents[i];

                if (content != null)
                    content.SetActive(false);
            }

            _activeContent = null;
        }

        private void SetHubMenuVisible(bool isVisible)
        {
            if (_hubMenuRoot != null)
                _hubMenuRoot.SetActive(isVisible);
        }

        private static void SetContentActive(GameObject content, bool isActive)
        {
            if (content != null)
                content.SetActive(isActive);
        }

        private void InitializeChildContent(GameObject content)
        {
            if (content == null)
                return;

            UIPopup[] popups = content.GetComponentsInChildren<UIPopup>(true);

            foreach (UIPopup popup in popups)
            {
                if (popup == null || ReferenceEquals(popup, this))
                    continue;

                if (!_initializedChildPopups.Add(popup))
                    continue;

                popup.Init();
                popup.SetAddressableKey(string.Empty);
            }
        }

        private static void ShowChildContentImmediately(GameObject content)
        {
            if (content == null)
                return;

            content.SetActive(true);

            CanvasGroup canvasGroup = content.GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
            }

            if (content.transform is RectTransform rectTransform)
            {
                rectTransform.DOKill();
                rectTransform.localScale = Vector3.one;
            }
        }

        private static void ShowChildContentWithOpenAnimation(GameObject content)
        {
            if (content == null)
                return;

            content.SetActive(true);

            UIPopup popup = content.GetComponent<UIPopup>();

            if (popup != null)
                popup.PlayOpenAnimation();
        }

        private void ResolveReferences()
        {
            if (_boardButton == null)
                _boardButton = FindButton("BoardButton", "MergeGameButton", "BoardQuestButton");

            if (_collectionButton == null)
                _collectionButton = FindButton("CollectionButton", "FishCollectionButton");

            if (_layoutButton == null)
                _layoutButton = FindButton("LayoutButton", "AquariumLayoutButton");

            if (_waterGazeButton == null)
                _waterGazeButton = FindButton("WaterGazeButton", "AquariumButton");

            if (_backButton == null)
                _backButton = FindButton("BackButton", "CloseButton", "ExitButton");

            if (_backgroundImage == null)
                _backgroundImage = FindImage("Background", "BackgroundImage");

            if (_titleLogoImage == null)
                _titleLogoImage = FindImage("LogoImage", "TitleLogoImage", "TitleImage", "NyangquariumTitle");

            ResolveChildContentReferences();
            ResolveAnimationReferences();
        }

        private void ResolveChildContentReferences()
        {
            if (_hubMenuRoot == null)
                _hubMenuRoot = FindGameObject("ContentButtons", "ControlPanel");

            if (_aquariumSelectRoot == null)
                _aquariumSelectRoot = FindGameObject("SelectAquariumButton", "AquariumSelectPanel", "AquariumSelectionUI");

            if (_freshAquariumButton == null)
                _freshAquariumButton = FindButton("FreshAquariumButton", "FreshWaterAquariumButton", "FreshButton");

            if (_oceanAquariumButton == null)
                _oceanAquariumButton = FindButton("OceanAquariumButton", "SaltAquariumButton", "OceanButton");
        }

        private void ResolveAnimationReferences()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_contentRoot == null)
                _contentRoot = transform as RectTransform;
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

        private Image FindImage(params string[] names)
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                foreach (string targetName in names)
                {
                    if (string.Equals(image.name, targetName, StringComparison.OrdinalIgnoreCase))
                        return image;
                }
            }

            return null;
        }

        private GameObject FindGameObject(params string[] names)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

            foreach (Transform child in transforms)
            {
                foreach (string targetName in names)
                {
                    if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                        return child.gameObject;
                }
            }

            return null;
        }

        private void BindButtons()
        {
            BindButton(_boardButton, OpenBoard);
            BindButton(_collectionButton, OpenCollection);
            BindButton(_layoutButton, OpenLayout);
            BindButton(_waterGazeButton, OpenWaterGaze);
            BindButton(_backButton, CloseHub);
        }

        private void BindAquariumSelectButtons()
        {
            BindButton(_freshAquariumButton, OpenFreshAquarium);
            BindButton(_oceanAquariumButton, OpenOceanAquarium);
        }

        private void BindAddressableSprites()
        {
            DisposeSpriteControllers();

            _backgroundSprite = BindSprite(_backgroundImage, _backgroundSpriteKey);
            _titleLogoSprite = BindSprite(_titleLogoImage, _titleLogoSpriteKey);
            _boardSprite = BindSprite(_boardButton, _boardSpriteKey);
            _collectionSprite = BindSprite(_collectionButton, _collectionSpriteKey);
            _layoutSprite = BindSprite(_layoutButton, _layoutSpriteKey);
            _waterGazeSprite = BindSprite(_waterGazeButton, _waterGazeSpriteKey);
            _backSprite = BindSprite(_backButton, _backSpriteKey);
        }

        private static UISpriteController BindSprite(Button button, string key)
            => BindSprite(button != null ? button.targetGraphic as Image : null, key);

        private static UISpriteController BindSprite(Image image, string key)
        {
            if (image == null || string.IsNullOrWhiteSpace(key))
                return null;

            UISpriteController controller = new(image);
            controller.ChangeSprite(key);
            return controller;
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
            RefreshRouteButton(_boardButton, _boardContent);
            RefreshRouteButton(_collectionButton, _collectionContent);
            RefreshRouteButton(_layoutButton, _aquariumSelectRoot);
            RefreshRouteButton(_waterGazeButton, _aquariumSelectRoot);
            RefreshRouteButton(_freshAquariumButton, _freshAquariumContent);
            RefreshRouteButton(_oceanAquariumButton, _oceanAquariumContent);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            SetInteractable(_boardButton, interactable);
            SetInteractable(_collectionButton, interactable);
            SetInteractable(_layoutButton, interactable);
            SetInteractable(_waterGazeButton, interactable);
            SetInteractable(_backButton, interactable);
            SetInteractable(_freshAquariumButton, interactable);
            SetInteractable(_oceanAquariumButton, interactable);
        }

        private void RefreshRouteButton(Button button, GameObject targetContent)
        {
            if (button == null)
                return;

            button.interactable = !_isTransitioning && targetContent != null;
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

        private static void NotifyEntryMode(GameObject content, NyangquariumHubEntryMode entryMode)
        {
            if (content == null)
                return;

            MonoBehaviour[] behaviours = content.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is INyangquariumHubEntryReceiver receiver)
                    receiver.SetNyangquariumEntryMode(entryMode);
            }
        }

        private void DisposeSpriteControllers()
        {
            _backgroundSprite?.Dispose();
            _titleLogoSprite?.Dispose();
            _boardSprite?.Dispose();
            _collectionSprite?.Dispose();
            _layoutSprite?.Dispose();
            _waterGazeSprite?.Dispose();
            _backSprite?.Dispose();

            _backgroundSprite = null;
            _titleLogoSprite = null;
            _boardSprite = null;
            _collectionSprite = null;
            _layoutSprite = null;
            _waterGazeSprite = null;
            _backSprite = null;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Active, this))
                Active = null;

            DisposeSpriteControllers();
            _canvasGroup?.DOKill();
            _contentRoot?.DOKill();
        }
    }
}
