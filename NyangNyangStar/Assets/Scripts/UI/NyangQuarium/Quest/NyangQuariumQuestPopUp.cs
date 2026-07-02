using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.NyangQuariumSO;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace UI.NyangQuarium.Quest
{
    // NyangQuariumQuestPopUp.prefab — 냥쿼리움 퀘스트 상세 팝업 UI
    // NyangQuariumQuestManager 데이터를 화면에 바인딩하고 완료/찾기 동작 처리
    public sealed partial class NyangQuariumQuestPopUp : UIPopup
    {
        [Header("Root")]
        [SerializeField] private GameObject _questPanelFrame;
        [SerializeField] private Button _closeButton;

        [Header("Story Theme (Main 퀘스트)")]
        [SerializeField] private GameObject _storyThemeRoot;
        [SerializeField] private TMP_Text _storyThemeText;

        [Header("Chapter Progress")]
        [SerializeField] private GameObject _chapterProgressRoot;
        [SerializeField] private TMP_Text _chapterTitleText;
        [SerializeField] private Slider _chapterProgressSlider;
        [SerializeField] private TMP_Text _chapterProgressText;

        [Header("Quest Info")]
        [SerializeField] private TMP_Text _questNameText;
        [SerializeField] private Image _conditionIcon;
        [SerializeField] private GameObject _conditionCompleteMark;

        [Header("Reward")]
        [SerializeField] private GameObject _rewardRoot;
        [SerializeField] private Image _rewardIconPrimary;
        [SerializeField] private TMP_Text _rewardPrimaryText;

        [Header("Action Buttons")]
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _findButton;
        [SerializeField] private GameObject _alertBadge;

        [Header("Data")]
        [SerializeField] private ItemDatabaseSo _itemDatabase;

        private NyangQuariumQuestData _boundQuest;
        // SetStoryContext로만 설정. Main 퀘스트일 때 스토리/챕터 영역 표시에 사용
        private string _storyThemeName;
        private string _chapterTitle;
        private float _chapterProgress;
        // PopupOpener가 Instantiate한 경우 닫을 때 Destroy (Addressable 팝업은 false)
        private bool _closeDestroysInstance;
        private bool _referencesResolved;
        private bool _staticSpritesBound;
        private bool _openRequested;
        private int _staticSpriteRequestCount;
        private int _staticSpriteResolvedCount;
        private int _conditionIconBindId;

        private Image _questPanelFrameImage;
        private Image _questInfoPanelImage;
        private Image _rewardSlotPanelImage;
        private Image _chapterProgressBackgroundImage;
        private Image _chapterProgressFillImage;
        private Image _conditionCompleteMarkImage;
        private Image _alertBadgeImage;
        private readonly List<NyangQuariumQuestPopUpSpriteController> _spriteControllers = new();
        private readonly List<AsyncOperationHandle<Sprite>> _loadedIconHandles = new();
        private Canvas _canvas;
        private RectTransform _rectTransform;

        public override void Init()
        {
            ResolveReferences();
            EnsureRequiredSpriteKeys();
            BindAddressableSprites();
            BindButtons();
        }

        // NyangQuariumQuestPopupOpener가 Instantiate로 열 때 true
        public void ConfigureDirectLifecycle(bool destroyOnClose)
        {
            _closeDestroysInstance = destroyOnClose;
        }

        public void OpenWhenReady()
        {
            ResolveReferences();
            BindAddressableSprites();

            if (StaticSpritesReady)
            {
                ShowPreparedPopup();
                return;
            }

            _openRequested = true;
        }

        // 프리팹 루트 scale이 0으로 저장되어 있어 열 때 1로 복원
        public override void PlayOpenAnimation()
        {
            ResolveReferences();
            BindAddressableSprites();
            BringToFront();

            transform.localScale = Vector3.one;

            if (_questPanelFrame != null)
                _questPanelFrame.transform.localScale = Vector3.one;
        }

        public void HideImmediately()
        {
            ResolveReferences();

            transform.localScale = Vector3.zero;

            if (_questPanelFrame != null)
                _questPanelFrame.transform.localScale = Vector3.zero;

            SendBehindMainUI();
            gameObject.SetActive(false);
        }

        // 표시할 퀘스트 지정 후 전체 UI 갱신
        public void BindQuest(NyangQuariumQuestData quest)
        {
            _boundQuest = quest;
            RefreshView();
        }

        public void RefreshBoundQuestView()
        {
            if (_boundQuest == null)
                return;

            RefreshView();
        }

        // Main 퀘스트 상단 스토리 테마, 챕터 진행률 (호출하지 않으면 해당 영역 숨김)
        public void SetStoryContext(string storyThemeName, string chapterTitle, float chapterProgress01)
        {
            _storyThemeName = storyThemeName;
            _chapterTitle = chapterTitle;
            _chapterProgress = Mathf.Clamp01(chapterProgress01);
            RefreshStoryAndChapter();
        }

        private void ResolveReferences()
        {
            if (_referencesResolved)
                return;

            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();

            Transform questPanelFrame = transform.Find("Quest_Panel_Frame");
            _questPanelFrame = questPanelFrame != null ? questPanelFrame.gameObject : null;
            _questPanelFrameImage = questPanelFrame != null ? questPanelFrame.GetComponent<Image>() : null;

            Transform panel = questPanelFrame != null ? questPanelFrame : transform;

            _closeButton = panel.Find("CloseButton")?.GetComponent<Button>();

            Transform storyThemeRoot = panel.Find("StoryThemeRoot");
            _storyThemeRoot = storyThemeRoot != null ? storyThemeRoot.gameObject : null;
            _storyThemeText = storyThemeRoot?.GetComponentInChildren<TMP_Text>(true);

            Transform chapterProgressRoot = panel.Find("Quest_ContentArea/ChapterProgressRoot");
            _chapterProgressRoot = chapterProgressRoot != null ? chapterProgressRoot.gameObject : null;
            _chapterTitleText = chapterProgressRoot?.Find("ChapterTitleText")?.GetComponent<TMP_Text>();
            _chapterProgressSlider = chapterProgressRoot?.Find("Slider")?.GetComponent<Slider>();

            if (_chapterProgressText == null && _chapterProgressSlider != null)
                _chapterProgressText = _chapterProgressSlider.GetComponentInChildren<TMP_Text>(true);

            Transform questInfoRoot = panel.Find("Quest_ContentArea/QuestInfoRoot");
            _questInfoPanelImage = questInfoRoot?.GetComponent<Image>();
            _questNameText = questInfoRoot?.Find("Header/QuestNameText")?.GetComponent<TMP_Text>();
            _conditionIcon = questInfoRoot?.Find("Body/ConditionIcon")?.GetComponent<Image>();
            Transform conditionCompleteMark = questInfoRoot?.Find("Body/ConditionCompleteMark");
            _conditionCompleteMark = conditionCompleteMark != null ? conditionCompleteMark.gameObject : null;
            _conditionCompleteMarkImage = conditionCompleteMark != null ? conditionCompleteMark.GetComponent<Image>() : null;

            Transform rewardRoot = panel.Find("Quest_ContentArea/RewardRoot");
            _rewardRoot = rewardRoot != null ? rewardRoot.gameObject : null;
            Transform rewardSlotPrimary = rewardRoot?.Find("RewardSlotPrimary");
            _rewardSlotPanelImage = rewardSlotPrimary?.GetComponent<Image>();
            _rewardIconPrimary = rewardSlotPrimary?.Find("RewardIconPrimary")?.GetComponent<Image>();
            _rewardPrimaryText = rewardRoot?.Find("RewardSlotPrimary/RewardPrimaryText")?.GetComponent<TMP_Text>();

            _completeButton = panel.Find("CompleteButton")?.GetComponent<Button>();
            _findButton = panel.Find("FindButton")?.GetComponent<Button>();
            Transform alertBadge = panel.Find("CompleteButton/AlertBadge");
            _alertBadge = alertBadge != null ? alertBadge.gameObject : null;
            _alertBadgeImage = alertBadge != null ? alertBadge.GetComponent<Image>() : null;

            if (_chapterProgressSlider != null)
            {
                Transform sliderRoot = _chapterProgressSlider.transform;
                _chapterProgressBackgroundImage = sliderRoot.Find("Background")?.GetComponent<Image>();
                _chapterProgressFillImage = sliderRoot.Find("Fill Area/Fill")?.GetComponent<Image>();
            }

            _referencesResolved = true;
        }

        private void BringToFront()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 5;
            }

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
                _rectTransform.SetAsLastSibling();
        }

        private void SendBehindMainUI()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 1;
            }
        }

        private void BindButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_completeButton != null)
            {
                _completeButton.onClick.RemoveListener(OnCompleteClicked);
                _completeButton.onClick.AddListener(OnCompleteClicked);
            }

            if (_findButton != null)
            {
                _findButton.onClick.RemoveListener(OnFindClicked);
                _findButton.onClick.AddListener(OnFindClicked);
            }
        }

        // 활성 퀘스트가 바뀌면 팝업이 열린 상태에서도 내용 자동 갱신
        private void OnEnable()
        {
            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.ActiveQuestChanged += HandleActiveQuestChanged;

            RefreshCurrentQuestIfNeeded();
        }

        private void OnDisable()
        {
            if (NyangQuariumQuestManager.Instance != null)
                NyangQuariumQuestManager.Instance.ActiveQuestChanged -= HandleActiveQuestChanged;
        }

        private void HandleActiveQuestChanged(NyangQuariumQuestData quest)
        {
            if (quest == null)
            {
                ClosePopup();
                return;
            }

            BindQuest(quest);
        }

        private void RefreshCurrentQuestIfNeeded()
        {
            if (_boundQuest != null)
            {
                RefreshView();
                return;
            }

            if (NyangQuariumQuestManager.Instance != null &&
                NyangQuariumQuestManager.Instance.TryGetActiveQuest(out NyangQuariumQuestData quest))
            {
                BindQuest(quest);
            }
        }

        private void OnCloseClicked()
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            ClosePopup();
        }

        private async void OnCompleteClicked()
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_boundQuest == null)
                return;

            bool completed = NyangQuariumQuestManager.Instance != null &&
                             await NyangQuariumQuestManager.Instance.CompleteQuestAsync(_boundQuest);

            if (completed)
            {
                NyangQuariumStoryQuestMapUI.RequestMapRefresh();
                ClosePopup();
            }
        }

        private void OnFindClicked()
        {
            if (_boundQuest == null)
                return;

            ClosePopup();

            if (MainUI.Instance != null)
            {
                MainUI.Instance.OpenMergeBoardFromQuest();
                return;
            }

            DebugTool.Warning(
                "[NyangQuariumQuestPopUp] MainUI를 찾지 못해 머지보드를 열 수 없습니다.",
                DebugType.UI,
                this);
        }

        public override void ClosePopup()
        {
            if (_closeDestroysInstance)
            {
                Destroy(gameObject);
                return;
            }

            HideImmediately();
        }

        private void OnDestroy()
        {
            DisposeSpriteControllers();
            ReleaseLoadedIconHandles();

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);

            if (_completeButton != null)
                _completeButton.onClick.RemoveListener(OnCompleteClicked);

            if (_findButton != null)
                _findButton.onClick.RemoveListener(OnFindClicked);
        }

        private void DisposeSpriteControllers()
        {
            for (int i = 0; i < _spriteControllers.Count; i++)
                _spriteControllers[i]?.Dispose();

            _spriteControllers.Clear();
        }

        private void ReleaseLoadedIconHandles()
        {
            for (int i = 0; i < _loadedIconHandles.Count; i++)
            {
                AsyncOperationHandle<Sprite> handle = _loadedIconHandles[i];

                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _loadedIconHandles.Clear();
        }
    }
}
