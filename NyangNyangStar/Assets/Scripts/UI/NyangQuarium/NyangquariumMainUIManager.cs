using System;
using System.Collections;
using System.Collections.Generic;
using Core.Managers;
using Data.Loader;
using DG.Tweening;
using TMPro;
using UI.Base;
using UI.NyangQuarium.Quest;
using UI.Transition;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium
{
    public sealed class NyangquariumMainUIManager : UIPopup
    {
        private static readonly string[] CollectionContentNames =
        {
            "NyangQuariumCollectionPopup",
            "NyangquariumCollectionPopup",
            "NyangQuariumCollectionCanvas",
            "NyangquariumCollectionCanvas",
            "CollectionContent",
            "CollectionPopup",
            "FishCollectionContent",
            "FishCollectionPopup"
        };

        private static readonly string[] CollectionDetailOnlyContentNames =
        {
            "NyangQuariumFishInfoPopup",
            "NyangquariumFishInfoPopup",
            "FishInfoPopup",
            "CollectionFishInfoPopup"
        };

        private const string CollectionButtonSpriteKey = "NQ_Btn_Collection";
        private const string FreshAquariumButtonSpriteKey = "NQ_Btn_FreshWater";
        private const string OceanAquariumButtonSpriteKey = "NQ_Btn_SaltWater";
        private const string TransitionSpriteKey = "NQ_BG_Transition";
        private const string TankLevelBubbleSpriteKey = "NQ_Icon_TankLevel";

        [Header("메인 버튼")]
        [FormerlySerializedAs("_boardQuestButton")]
        [SerializeField] private Button _boardButton;
        [SerializeField] private Button _collectionButton;
        [SerializeField] private Button _layoutButton;
        [SerializeField] private Button _backButton;

        [Header("하위 콘텐츠 오브젝트 연결")]
        [Tooltip("기본 메인 버튼 묶음입니다. 비워두면 ContentButtons를 자동으로 찾습니다.")]
        [FormerlySerializedAs("_hubMenuRoot")]
        [SerializeField] private GameObject _mainMenuRoot;
        [Tooltip("메인 버튼이 아니라 최초 진입 이벤트에서 OpenStory를 호출할 때 사용합니다.")]
        [SerializeField] private GameObject _storyContent;
        [FormerlySerializedAs("_boardQuestContent")]
        [SerializeField] private GameObject _boardContent;
        [SerializeField] private GameObject _collectionContent;
        [Tooltip("수조 레이아웃 버튼을 눌렀을 때 먼저 여는 담수/해수 선택 UI입니다.")]
        [SerializeField] private GameObject _aquariumSelectRoot;
        [Tooltip("담수 선택 시 열 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
        [SerializeField] private GameObject _freshAquariumContent;
        [Tooltip("해수 선택 시 열 하위 오브젝트입니다. 담당자가 프리팹을 넣은 뒤 연결하면 됩니다.")]
        [SerializeField] private GameObject _oceanAquariumContent;

        [Header("수조 선택 버튼")]
        [SerializeField] private Button _freshAquariumButton;
        [SerializeField] private Button _oceanAquariumButton;
        [SerializeField] private Button _aquariumSelectBackButton;

        [Header("수조 선택 버튼 잠금")]
        [SerializeField] private GameObject _oceanLockIcon;
        [SerializeField] private TMP_Text _oceanLockMessageText;
        [SerializeField] private int _oceanUnlockLevel = 5;
        [SerializeField] private string _oceanLockedMessage = "수조 레벨 5 이상에서 해수 수조를 열 수 있습니다.";
        [SerializeField] private Color _lockedButtonColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("기본 화면 Addressables 스프라이트")]
        [FormerlySerializedAs("_tankImage")]
        [SerializeField] private Image _backgroundImage;
        [FormerlySerializedAs("_tankSpriteKey")]
        [SerializeField] private string _backgroundSpriteKey = "NQ_BG_SaltWater";
        [SerializeField] private Image _titleLogoImage;
        [SerializeField] private string _titleLogoSpriteKey = "NQ_Img_Title";
        [SerializeField] private string _boardSpriteKey = "NQ_Btn_Mergeboard";
        [SerializeField] private string _collectionSpriteKey = CollectionButtonSpriteKey;
        [SerializeField] private string _layoutSpriteKey = "NQ_Btn_Tank";
        [SerializeField] private string _backSpriteKey = "NQ_Btn_Back";
        [SerializeField] private string _aquariumSelectBackSpriteKey = "Btn_Close";
        [SerializeField] private string _freshAquariumSpriteKey = FreshAquariumButtonSpriteKey;
        [SerializeField] private string _oceanAquariumSpriteKey = OceanAquariumButtonSpriteKey;
        [SerializeField] private string _lockIconSpriteKey = "NQ_Icon_Lock";

        [Header("수조 레벨 방울")]
        [SerializeField] private NyangQuariumLevelBubbleUI levelBubbleUI;
        [SerializeField] private string _tankLevelBubbleSpriteKey = TankLevelBubbleSpriteKey;
        [Tooltip("거북이 이미지가 Addressable에 추가되면 키를 넣어 연결합니다. 비워두면 숨깁니다.")]
        [SerializeField] private string _tankLevelTurtleSpriteKey;
        [SerializeField] private int _tankLevel = 1;
        [SerializeField, Range(0f, 1f)] private float _tankLevelExpRatio;

        [Header("연출")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private float _openDuration = 0.2f;
        [SerializeField] private float _openStartScale = 0.96f;
        [SerializeField] private bool _useScreenTransition = true;
        [SerializeField] private string _clickSfxKey = "Main_SFX_Touch";

        private GameObject _activeContent;
        private bool _isTransitioning;
        private NyangquariumEntryMode _pendingAquariumEntryMode = NyangquariumEntryMode.Layout;
        private UISpriteController _backgroundSprite;
        private UISpriteController _titleLogoSprite;
        private UISpriteController _boardSprite;
        private UISpriteController _collectionSprite;
        private UISpriteController _layoutSprite;
        private UISpriteController _backSprite;
        private UISpriteController _freshAquariumSprite;
        private UISpriteController _oceanAquariumSprite;
        private UISpriteController _aquariumSelectBackSprite;
        private UISpriteController _oceanLockIconSprite;
        private readonly List<GameObject> _ownedContents = new();
        private readonly HashSet<UIPopup> _initializedChildPopups = new();
        private NyangQuariumFirestoreSO _nyangquariumFirestoreSO;
        private NyangQuariumAquariumLevelSO _aquariumLevelSO;
        private bool _isTankLevelBubbleVisible = true;
        private bool _warnedCollectionDetailOnlyContent;

        public static NyangquariumMainUIManager Active { get; private set; }

        private void OnEnable()
        {
            if (Application.isPlaying)
                BindAquariumProgressStoreAsync();
        }

        private void OnDisable()
        {
            UnbindAquariumProgressStore();
        }

        private void OnValidate()
        {
            _tankLevel = Mathf.Max(1, _tankLevel);
            _tankLevelExpRatio = Mathf.Clamp01(_tankLevelExpRatio);

            if (levelBubbleUI != null)
                levelBubbleUI.SetLevelProgress(_tankLevel, _tankLevelExpRatio);
        }

        public override void Init()
        {
            Active = this;
            ResolveReferences();
            BindButtons();
            BindAquariumSelectButtons();
            BindAddressableSprites();
            EnsureTankLevelBubbleUI();
            BindAquariumProgressStoreAsync();
            ApplyAquariumSelectStyle();
            ShowMainViewImmediately();
            RefreshButtonStates();
        }

        public override void PlayOpenAnimation()
        {
            Active = this;
            ResolveAnimationReferences();
            ShowMainViewImmediately();

            if (_canvasGroup == null || _contentRoot == null)
            {
                DebugTool.Warning(
                    "[NyangquariumMainUIManager] 오픈 연출에 필요한 CanvasGroup 또는 RectTransform을 찾지 못했습니다.",
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

            EnsureTankLevelBubbleUI();
            BindAquariumProgressStoreAsync();
            ShowMainViewImmediately();
            RefreshButtonStates();
            EnsureFishSpritePreload();
        }

        public void OpenStory() => OpenChildContent(_storyContent, NyangquariumEntryMode.Story);

        public void SetTankLevelProgress(int level, float expRatio)
        {
            ApplyTankLevelProgress(level, expRatio);
        }

        public void SetTankLevelExperience(int level, int currentExp, int maxExp)
        {
            float ratio = maxExp <= 0 ? 0f : (float)Mathf.Clamp(currentExp, 0, maxExp) / maxExp;
            SetTankLevelProgress(level, ratio);
        }

        public void SetTankLevelTurtleSpriteKey(string turtleSpriteKey)
        {
            _tankLevelTurtleSpriteKey = turtleSpriteKey ?? string.Empty;
            EnsureTankLevelBubbleUI();
            levelBubbleUI?.SetTurtleSpriteKey(_tankLevelTurtleSpriteKey);
        }

        public void NotifyPopupContentClosed(GameObject content)
        {
            if (content != null && ReferenceEquals(_activeContent, content))
                _activeContent = null;

            if (HasVisibleChildContent())
                return;

            SetTankLevelBubbleVisible(true);
            RefreshButtonStates();
        }

        public void RefreshTankLevelFromFirestore()
        {
            if (_nyangquariumFirestoreSO == null)
                TryBindLoadedAquariumProgressStore();

            if (_nyangquariumFirestoreSO == null)
                return;

            int level = _nyangquariumFirestoreSO.AquariumLevel;
            int exp = _nyangquariumFirestoreSO.AquariumExp;
            float expRatio = CalculateTankLevelExpRatio(level, exp);
            ApplyTankLevelProgress(level, expRatio);
        }

        private async void BindAquariumProgressStoreAsync()
        {
            if (TryBindLoadedAquariumProgressStore())
                return;

            NyangQuariumFirestoreSO store = await NyangQuariumFirestoreSO.WaitForReadyAsync();

            if (this == null || !isActiveAndEnabled || store == null)
                return;

            BindAquariumProgressStore(store);
            RefreshTankLevelFromFirestore();
        }

        private bool TryBindLoadedAquariumProgressStore()
        {
            FireStoreManager manager = FireStoreManager.Instance;

            if (manager == null ||
                !manager.IsInitialized ||
                !manager.TryGetStore(out NyangQuariumFirestoreSO store) ||
                store == null)
            {
                return false;
            }

            BindAquariumProgressStore(store);
            RefreshTankLevelFromFirestore();
            return true;
        }

        private void BindAquariumProgressStore(NyangQuariumFirestoreSO store)
        {
            if (_nyangquariumFirestoreSO == store)
                return;

            UnbindAquariumProgressStore();
            _nyangquariumFirestoreSO = store;
            _nyangquariumFirestoreSO.AquariumProgressChanged += OnAquariumProgressChanged;
        }

        private void UnbindAquariumProgressStore()
        {
            if (_nyangquariumFirestoreSO == null)
                return;

            _nyangquariumFirestoreSO.AquariumProgressChanged -= OnAquariumProgressChanged;
            _nyangquariumFirestoreSO = null;
        }

        private void OnAquariumProgressChanged()
        {
            RefreshTankLevelFromFirestore();
        }

        private float CalculateTankLevelExpRatio(int level, int exp)
        {
            NyangQuariumAquariumLevelSO levelSO = ResolveAquariumLevelSO();

            if (levelSO == null ||
                !levelSO.TryGetByLevel(level, out NyangQuariumAquariumLevelData levelData))
            {
                return 0f;
            }

            if (levelData.RequiredExp <= 0)
                return 1f;

            return Mathf.Clamp01((float)Mathf.Max(0, exp) / levelData.RequiredExp);
        }

        private NyangQuariumAquariumLevelSO ResolveAquariumLevelSO()
        {
            if (_aquariumLevelSO == null)
                _aquariumLevelSO = NyangQuariumQuestSOLocator.ResolveAquariumLevelSO();

            return _aquariumLevelSO;
        }

        private void ApplyTankLevelProgress(int level, float expRatio)
        {
            _tankLevel = Mathf.Max(1, level);
            _tankLevelExpRatio = Mathf.Clamp01(expRatio);
            EnsureTankLevelBubbleUI();
            levelBubbleUI?.SetLevelProgress(_tankLevel, _tankLevelExpRatio);

            ApplyOceanAquariumLockState();

            DebugTool.Log($"[NyangquariumMainUIManager] 수조 레벨: {_tankLevel}, 경험치 비율: {_tankLevelExpRatio:P1}", DebugType.UI, this);
        }

        public void OpenBoard()
        {
            EnsureFishSpritePreload();
            OpenChildContent(_boardContent, NyangquariumEntryMode.Board);
        }
        public void OpenCollection() => OpenPopupContent(_collectionContent, NyangquariumEntryMode.Collection);
        public void OpenLayout() => OpenAquariumSelect(NyangquariumEntryMode.Layout);

        public void ReturnToMain()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() =>
            {
                ShowMainViewImmediately();
            });
        }

        public void RegisterOwnedContent(UIPopup popup, bool setActiveContent = true)
            => RegisterOwnedContent(popup != null ? popup.gameObject : null, setActiveContent);

        public void PrepareOwnedContent(UIPopup popup, NyangquariumEntryMode entryMode, bool setActiveContent = true)
            => PrepareOwnedContent(popup != null ? popup.gameObject : null, entryMode, setActiveContent);

        public void PrepareOwnedContent(GameObject content, NyangquariumEntryMode entryMode, bool setActiveContent = true)
        {
            if (content == null)
                return;

            NyangquariumEntryContext.Set(entryMode);
            RegisterOwnedContent(content, setActiveContent);
            InitializeChildContent(content);
            NotifyEntryMode(content, entryMode);
        }

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

        public void CloseMain()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            RunCoveredTransition(() =>
            {
                HideChildContents();
                SetTankLevelBubbleVisible(false);
                SetMainMenuVisible(true);
                gameObject.SetActive(false);
            });
        }

        private void OpenAquariumSelect(NyangquariumEntryMode entryMode)
        {
            if (_isTransitioning || _aquariumSelectRoot == null)
                return;

            PlayClickSfx();
            _pendingAquariumEntryMode = entryMode;
            NyangquariumEntryContext.Set(entryMode);

            HideChildContents();
            SetTankLevelBubbleVisible(false);
            SetMainMenuVisible(false);
            RegisterOwnedContent(_aquariumSelectRoot);
            InitializeChildContent(_aquariumSelectRoot);
            NotifyEntryMode(_aquariumSelectRoot, entryMode);
            ApplyAquariumSelectStyle();
            ApplyOceanAquariumLockState();
            ShowChildContentImmediately(_aquariumSelectRoot);
            RefreshButtonStates();
        }

        private void ReturnFromAquariumSelectToMain()
        {
            if (_isTransitioning)
                return;

            PlayClickSfx();
            ShowMainViewImmediately();
            RefreshButtonStates();
        }

        public void OpenFreshAquarium()
            => OpenChildContent(_freshAquariumContent, _pendingAquariumEntryMode);

        public void OpenOceanAquarium()
        {
            if (!IsOceanAquariumUnlocked())
            {
                PlayClickSfx();
                ShowOceanLockedMessage();

                return;
            }

            OpenChildContent(_oceanAquariumContent, _pendingAquariumEntryMode);
        }

        private void OpenChildContent(GameObject content, NyangquariumEntryMode entryMode)
        {
            if (_isTransitioning || content == null)
                return;

            PlayClickSfx();
            SetButtonsInteractable(false);
            _isTransitioning = true;
            ScreenTransitionManager transition = GetTransition();

            void ShowContent()
            {
                NyangquariumEntryContext.Set(entryMode);
                HideChildContents();
                SetTankLevelBubbleVisible(false);
                SetMainMenuVisible(false);
                PrepareOwnedContent(content, entryMode);

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

            transition.Cover(TransitionSpriteKey, () =>
            {
                ShowContent();
                transition.Reveal(() =>
                {
                    transition.RestoreDefaultCoverSprite();
                    Unlock();
                });
            });
        }

        private void OpenPopupContent(GameObject content, NyangquariumEntryMode entryMode)
        {
            if (_isTransitioning || content == null)
                return;

            PlayClickSfx();
            NyangquariumEntryContext.Set(entryMode);
            RegisterOwnedContent(content);
            InitializeChildContent(content);
            NotifyEntryMode(content, entryMode);
            content.transform.SetAsLastSibling();
            SetTankLevelBubbleVisible(false);
            ShowChildContentImmediately(content);
            RefreshButtonStates();
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

            transition.Cover(TransitionSpriteKey, () =>
            {
                coveredAction?.Invoke();
                transition.Reveal(() =>
                {
                    transition.RestoreDefaultCoverSprite();
                    Unlock();
                });
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

        private void ShowMainViewImmediately()
        {
            gameObject.SetActive(true);
            HideChildContents();
            SetMainMenuVisible(true);
            SetTankLevelBubbleVisible(true);
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

        private bool HasVisibleChildContent()
        {
            if (IsContentVisible(_storyContent) ||
                IsContentVisible(_boardContent) ||
                IsContentVisible(_collectionContent) ||
                IsContentVisible(_aquariumSelectRoot) ||
                IsContentVisible(_freshAquariumContent) ||
                IsContentVisible(_oceanAquariumContent))
            {
                return true;
            }

            for (int i = 0; i < _ownedContents.Count; i++)
            {
                if (IsContentVisible(_ownedContents[i]))
                    return true;
            }

            return false;
        }

        private void SetMainMenuVisible(bool isVisible)
        {
            if (_mainMenuRoot != null)
                _mainMenuRoot.SetActive(isVisible);
        }

        private void SetTankLevelBubbleVisible(bool isVisible)
        {
            _isTankLevelBubbleVisible = isVisible;

            if (levelBubbleUI != null)
                levelBubbleUI.gameObject.SetActive(isVisible);
        }

        private static void SetContentActive(GameObject content, bool isActive)
        {
            if (content != null)
                content.SetActive(isActive);
        }

        private static bool IsContentVisible(GameObject content)
            => content != null && content.activeSelf;

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

            if (_backgroundImage == null)
                _backgroundImage = FindImage("Background", "BackgroundImage");

            if (_titleLogoImage == null)
                _titleLogoImage = FindImage("LogoImage", "TitleLogoImage", "TitleImage", "NyangquariumTitle");

            ResolveChildContentReferences();

            if (_backButton == null)
                _backButton = FindButtonIn(_mainMenuRoot, "BackButton", "CloseButton", "ExitButton")
                    ?? FindButton("BackButton", "CloseButton", "ExitButton");

            ResolveAnimationReferences();
        }

        private void ResolveChildContentReferences()
        {
            if (_mainMenuRoot == null)
                _mainMenuRoot = FindGameObject("ContentButtons", "ControlPanel");

            if (_aquariumSelectRoot == null)
                _aquariumSelectRoot = FindGameObject("SelectAquariumButton", "AquariumSelectPanel", "AquariumSelectionUI");

            if (_collectionContent == null || IsNamed(_collectionContent, CollectionDetailOnlyContentNames))
                _collectionContent = ResolveCollectionContent(_collectionContent);

            if (_boardContent == null || !IsChildContent(_boardContent))
                _boardContent = FindGameObject("NyangQuariumMergeBoard", "Merge Board", "MergeBoard", "BoardContent");

            if (_freshAquariumContent == null)
                _freshAquariumContent = FindContent<global::NyangQuariumFreshLayoutUI>(
                    "NyangQuariumFreshCanvas",
                    "NyangQuariumFreshLayoutCanvas",
                    "FreshAquariumContent",
                    "FreshLayoutContent");

            if (_oceanAquariumContent == null)
                _oceanAquariumContent = FindContent<global::NyangQuariumOceanLayoutUI>(
                    "NyangQuariumOceanCanvas",
                    "NyangQuariumOceanLayoutCanvas",
                    "OceanAquariumContent",
                    "OceanLayoutContent");

            if (_freshAquariumButton == null)
                _freshAquariumButton = FindButtonIn(_aquariumSelectRoot, "FreshAquariumButton", "FreshWaterAquariumButton", "FreshButton")
                    ?? FindButton("FreshAquariumButton", "FreshWaterAquariumButton", "FreshButton");

            if (_oceanAquariumButton == null)
                _oceanAquariumButton = FindButtonIn(_aquariumSelectRoot, "OceanAquariumButton", "SaltAquariumButton", "OceanButton")
                    ?? FindButton("OceanAquariumButton", "SaltAquariumButton", "OceanButton");

            if (_aquariumSelectBackButton == null)
                _aquariumSelectBackButton = FindButtonIn(_aquariumSelectRoot, "BackButton", "AquariumSelectBackButton", "SelectBackButton");
        }

        private GameObject ResolveCollectionContent(GameObject currentContent)
        {
            if (IsNamed(currentContent, CollectionContentNames))
                return currentContent;

            if (IsNamed(currentContent, CollectionDetailOnlyContentNames))
                WarnCollectionDetailOnlyContent(currentContent);

            GameObject collectionContent = FindGameObject(CollectionContentNames);

            if (collectionContent != null)
                return collectionContent;

            GameObject detailOnlyContent = FindGameObject(CollectionDetailOnlyContentNames);

            if (detailOnlyContent != null)
                WarnCollectionDetailOnlyContent(detailOnlyContent);

            return IsNamed(currentContent, CollectionDetailOnlyContentNames) ? null : currentContent;
        }

        private void WarnCollectionDetailOnlyContent(GameObject content)
        {
            if (_warnedCollectionDetailOnlyContent || content == null)
                return;

            _warnedCollectionDetailOnlyContent = true;
            DebugTool.Warning(
                $"[NyangquariumMainUIManager] {content.name} is a fish detail popup. Add NyangQuariumCollectionPopup under NyanquariumUI for CollectionButton.",
                DebugType.UI,
                this);
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
            return FindButtonIn(buttons, names);
        }

        private static Button FindButtonIn(GameObject root, params string[] names)
        {
            if (root == null)
                return null;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            return FindButtonIn(buttons, names);
        }

        private static Button FindButtonIn(Button[] buttons, params string[] names)
        {
            if (buttons == null)
                return null;

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

        private GameObject FindContent<T>(params string[] names) where T : Component
        {
            T component = GetComponentInChildren<T>(true);

            if (component != null)
                return component.gameObject;

            return FindGameObject(names);
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

        private static Image FindImageIn(GameObject root, params string[] names)
        {
            if (root == null)
                return null;

            Image[] images = root.GetComponentsInChildren<Image>(true);

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

        private bool IsChildContent(GameObject content)
            => content != null && content.transform.IsChildOf(transform);

        private static bool IsNamed(GameObject gameObject, string[] names)
        {
            if (gameObject == null || names == null)
                return false;

            foreach (string targetName in names)
            {
                if (string.Equals(gameObject.name, targetName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void BindButtons()
        {
            BindButton(_boardButton, OpenBoard);
            BindButton(_collectionButton, OpenCollection);
            BindButton(_layoutButton, OpenLayout);
            BindButton(_backButton, CloseMain);
        }

        private void BindAquariumSelectButtons()
        {
            BindButton(_freshAquariumButton, OpenFreshAquarium);
            BindButton(_oceanAquariumButton, OpenOceanAquarium);
            BindButton(_aquariumSelectBackButton, ReturnFromAquariumSelectToMain);
        }

        private void BindAddressableSprites()
        {
            DisposeSpriteControllers();
            RegisterNyangquariumSpriteKeys();

            _backgroundSprite = BindSprite(_backgroundImage, _backgroundSpriteKey);
            _titleLogoSprite = BindSprite(_titleLogoImage, _titleLogoSpriteKey, true);
            _boardSprite = BindSprite(_boardButton, _boardSpriteKey, true);
            _collectionSprite = BindSprite(_collectionButton, _collectionSpriteKey);
            _layoutSprite = BindSprite(_layoutButton, _layoutSpriteKey);
            _backSprite = BindSprite(_backButton, _backSpriteKey);
            _freshAquariumSprite = BindSprite(_freshAquariumButton, _freshAquariumSpriteKey);
            _oceanAquariumSprite = BindSprite(_oceanAquariumButton, _oceanAquariumSpriteKey);
            _aquariumSelectBackSprite = BindSprite(_aquariumSelectBackButton, _aquariumSelectBackSpriteKey, true);

            Image lockIconImage = _oceanLockIcon != null
                ? _oceanLockIcon.GetComponent<Image>()
                : null;

            _oceanLockIconSprite = BindSprite(lockIconImage, _lockIconSpriteKey, true);
        }

        private void ApplyAquariumSelectStyle()
        {
            if (_aquariumSelectRoot == null)
                return;

            if (_aquariumSelectRoot.transform is RectTransform rootRect)
                StretchRect(rootRect);

            Image dimImage = FindImageIn(_aquariumSelectRoot, "Dim");
            if (dimImage != null)
            {
                StretchRect(dimImage.rectTransform);
                dimImage.color = new Color(0f, 0f, 0f, 0.62f);
                dimImage.raycastTarget = true;
                dimImage.transform.SetAsFirstSibling();
            }

            Image frameImage = FindImageIn(_aquariumSelectRoot, "Frame", "Panel");
            if (frameImage != null)
            {
                RectTransform frameRect = frameImage.rectTransform;
                frameRect.anchorMin = new Vector2(0f, 0.5f);
                frameRect.anchorMax = new Vector2(1f, 0.5f);
                frameRect.pivot = new Vector2(0.5f, 0.5f);
                frameRect.anchoredPosition = Vector2.zero;
                frameRect.sizeDelta = new Vector2(0f, 360f);
                frameImage.color = new Color(0f, 0f, 0f, 0.42f);
                frameImage.raycastTarget = false;

                if (frameImage.transform.parent == _aquariumSelectRoot.transform)
                    frameImage.transform.SetSiblingIndex(Mathf.Min(1, _aquariumSelectRoot.transform.childCount - 1));
            }

            StyleAquariumSelectButton(_freshAquariumButton, new Vector2(-155f, 35f), "담수");
            StyleAquariumSelectButton(_oceanAquariumButton, new Vector2(155f, 35f), "해수");
        }

        private void EnsureTankLevelBubbleUI()
        {
            if (levelBubbleUI == null)
                levelBubbleUI = GetComponentInChildren<NyangQuariumLevelBubbleUI>(true);

            Transform parent = _mainMenuRoot != null ? _mainMenuRoot.transform : transform;
            bool createdRuntimeBubble = false;

            if (levelBubbleUI == null)
            {
                GameObject bubbleObject = new("TankLevelBubble", typeof(RectTransform));
                bubbleObject.layer = parent.gameObject.layer;
                bubbleObject.transform.SetParent(parent, false);
                levelBubbleUI = bubbleObject.AddComponent<NyangQuariumLevelBubbleUI>();
                createdRuntimeBubble = true;
            }

            levelBubbleUI.gameObject.SetActive(_isTankLevelBubbleVisible);

            if (createdRuntimeBubble)
                ConfigureTankLevelBubbleRect(levelBubbleUI.transform as RectTransform);

            levelBubbleUI.transform.SetAsLastSibling();
            levelBubbleUI.Initialize(
                _tankLevelBubbleSpriteKey,
                _tankLevelTurtleSpriteKey,
                _tankLevel,
                _tankLevelExpRatio);
        }

        private static void ConfigureTankLevelBubbleRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-18f, 28f);
            rectTransform.sizeDelta = new Vector2(118f, 118f);
            rectTransform.localScale = Vector3.one;
        }

        private static void StyleAquariumSelectButton(Button button, Vector2 anchoredPosition, string label)
        {
            if (button == null)
                return;

            if (button.transform is RectTransform buttonRect)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = anchoredPosition;
                buttonRect.sizeDelta = new Vector2(220f, 220f);
            }

            if (button.targetGraphic is Image buttonImage)
            {
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = true;
                buttonImage.color = Color.white;
                buttonImage.raycastTarget = true;
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                return;

            text.text = label;
            text.color = Color.white;
            text.fontSize = 60f;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            if (text.transform is RectTransform textRect)
            {
                textRect.anchorMin = new Vector2(0.5f, 0f);
                textRect.anchorMax = new Vector2(0.5f, 0f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = new Vector2(0f, -10f);
                textRect.sizeDelta = new Vector2(240f, 90f);
            }
        }

        private static void StretchRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private void RegisterNyangquariumSpriteKeys()
        {
            RegisterSpriteKeyIfMissing(_backgroundSpriteKey);
            RegisterSpriteKeyIfMissing(_titleLogoSpriteKey);
            RegisterSpriteKeyIfMissing(_boardSpriteKey);
            RegisterSpriteKeyIfMissing(_collectionSpriteKey);
            RegisterSpriteKeyIfMissing(_layoutSpriteKey);
            RegisterSpriteKeyIfMissing(_backSpriteKey);
            RegisterSpriteKeyIfMissing(_freshAquariumSpriteKey);
            RegisterSpriteKeyIfMissing(_oceanAquariumSpriteKey);
            RegisterSpriteKeyIfMissing(_lockIconSpriteKey);
            RegisterSpriteKeyIfMissing(_tankLevelBubbleSpriteKey);
            RegisterSpriteKeyIfMissing(_tankLevelTurtleSpriteKey);
            RegisterSpriteKeyIfMissing(CollectionButtonSpriteKey);
            RegisterSpriteKeyIfMissing(FreshAquariumButtonSpriteKey);
            RegisterSpriteKeyIfMissing(OceanAquariumButtonSpriteKey);
            RegisterSpriteKeyIfMissing(TankLevelBubbleSpriteKey);
            RegisterSpriteKeyIfMissing(_aquariumSelectBackSpriteKey);
        }

        private static void RegisterSpriteKeyIfMissing(string spriteKey)
        {
            if (!string.IsNullOrWhiteSpace(spriteKey))
                KeyContainer.Sprites.Add(spriteKey);
        }

        private UISpriteController BindSprite(Button button, string key, bool fitHeightToSpriteAspect = false)
        {
            Image image = button != null ? button.targetGraphic as Image : null;

            if (image == null || string.IsNullOrWhiteSpace(key))
                return null;

            if (fitHeightToSpriteAspect && image.rectTransform != null)
            {
                float referenceHeight = GetReferenceHeight(image.rectTransform);

                if (referenceHeight > 0f)
                    StartCoroutine(FitRectWidthToSpriteAspectWhenReady(image, referenceHeight));
            }

            return BindSprite(image, key, true);
        }

        private static UISpriteController BindSprite(Image image, string key, bool preserveAspect = false)
        {
            if (image == null || string.IsNullOrWhiteSpace(key))
                return null;

            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;

            UISpriteController controller = new(image);
            controller.ChangeSprite(key);
            return controller;
        }

        private IEnumerator FitRectWidthToSpriteAspectWhenReady(Image image, float referenceHeight)
        {
            Sprite lastSprite = null;

            for (int i = 0; i < 120; i++)
            {
                if (image == null)
                    yield break;

                Sprite currentSprite = image.sprite;

                if (currentSprite != null && !ReferenceEquals(currentSprite, lastSprite))
                {
                    FitRectWidthToSpriteAspect(image.rectTransform, currentSprite, referenceHeight);
                    lastSprite = currentSprite;
                }

                yield return null;
            }
        }

        private static float GetReferenceHeight(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return 0f;

            float referenceHeight = rectTransform.sizeDelta.y;

            if (referenceHeight <= 0f)
                referenceHeight = rectTransform.rect.height;

            return referenceHeight;
        }

        private static void FitRectWidthToSpriteAspect(RectTransform rectTransform, Sprite sprite, float referenceHeight)
        {
            if (rectTransform == null || sprite == null || referenceHeight <= 0f)
                return;

            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;

            if (spriteWidth <= 0f || spriteHeight <= 0f)
                return;

            float spriteAspect = spriteWidth / spriteHeight;
            rectTransform.sizeDelta = new Vector2(referenceHeight * spriteAspect, referenceHeight);
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
            RefreshRouteButton(_freshAquariumButton, _freshAquariumContent);

            // Lv.5 미만이어도 버튼 클릭은 가능해야 안내 문구를 보여줄 수 있음.
            RefreshRouteButton(_oceanAquariumButton, _oceanAquariumContent);

            SetInteractable(_aquariumSelectBackButton, !_isTransitioning);
            ApplyOceanAquariumLockState();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            SetInteractable(_boardButton, interactable);
            SetInteractable(_collectionButton, interactable);
            SetInteractable(_layoutButton, interactable);
            SetInteractable(_backButton, interactable);
            SetInteractable(_freshAquariumButton, interactable);
            SetInteractable(_oceanAquariumButton, interactable);
            SetInteractable(_aquariumSelectBackButton, interactable);
        }

        private void RefreshRouteButton(Button button, GameObject targetContent)
        {
            if (button == null)
                return;

            button.interactable = !_isTransitioning && targetContent != null;
        }
        private bool IsOceanAquariumUnlocked()
        {
            return _tankLevel >= _oceanUnlockLevel;
        }

        private void ApplyOceanAquariumLockState()
        {
            bool isUnlocked = IsOceanAquariumUnlocked();

            if (_oceanLockIcon != null)
                _oceanLockIcon.SetActive(!isUnlocked);

            if (_oceanLockMessageText != null)
                _oceanLockMessageText.gameObject.SetActive(false);

            Image oceanButtonImage = _oceanAquariumButton != null
                ? _oceanAquariumButton.targetGraphic as Image
                : null;

            if (oceanButtonImage != null)
            {
                oceanButtonImage.color = isUnlocked ? Color.white : _lockedButtonColor;
            }
        }

        private void ShowOceanLockedMessage()
        {
            if (_oceanLockMessageText == null)
            {
                DebugTool.Warning(
                    "[NyangquariumMainUIManager] _oceanLockMessageText가 연결되지 않아 해수 잠금 안내 문구를 표시할 수 없습니다.",
                    DebugType.UI,
                    this);

                return;
            }

            _oceanLockMessageText.text = _oceanLockedMessage;
            _oceanLockMessageText.gameObject.SetActive(true);

            _oceanLockMessageText.DOKill();
            _oceanLockMessageText.alpha = 1f;

            _oceanLockMessageText
                .DOFade(0f, 1.2f)
                .SetDelay(1.2f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (_oceanLockMessageText != null)
                        _oceanLockMessageText.gameObject.SetActive(false);
                });
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

        private static void NotifyEntryMode(GameObject content, NyangquariumEntryMode entryMode)
        {
            if (content == null)
                return;

            MonoBehaviour[] behaviours = content.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is INyangquariumEntryReceiver receiver)
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
            _backSprite?.Dispose();
            _freshAquariumSprite?.Dispose();
            _oceanAquariumSprite?.Dispose();
            _aquariumSelectBackSprite?.Dispose();
            _oceanLockIconSprite?.Dispose();

            _backgroundSprite = null;
            _titleLogoSprite = null;
            _boardSprite = null;
            _collectionSprite = null;
            _layoutSprite = null;
            _backSprite = null;
            _freshAquariumSprite = null;
            _oceanAquariumSprite = null;
            _aquariumSelectBackSprite = null;
            _oceanLockIconSprite = null;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Active, this))
                Active = null;

            UnbindAquariumProgressStore();
            DisposeSpriteControllers();
            _canvasGroup?.DOKill();
            _contentRoot?.DOKill();
        }

        // NyangQuariumFishSpriteCache 클래스에서 물고기 스프라이트 백그라운드 로드 시작
        // 냥쿠 열 때 / 머지보드 들어갈 때 물고기 스프라이트 백그라운드 확인 용
        // 이미 로드 중이거나 완료됐으면 그냥 return (중복 요청 방지)
        private void EnsureFishSpritePreload()
        {
            if (NyangQuariumFishSpriteCache.IsLoaded || NyangQuariumFishSpriteCache.IsLoading)
                return;

            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumFishSO(out NyangQuariumFishSO fishSO))
            {
                NyangQuariumFishSpriteCache.BeginPreload(this, fishSO);
            }
        }
    }
}
