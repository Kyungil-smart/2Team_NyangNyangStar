using System;
using System.Collections.Generic;
using Core.Managers;
using Data.Loader;
using Data.LibrarySystem;
using Data.ScriptableObjects.KeyContainerSO;
using Data.ScriptableObjects.MergeBoard;
using Data.ScriptableObjects.NyangQuariumSO;
using Services.Enums;
using UI;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium.Quest
{
    // NyangQuariumQuestPopUp.prefab — 냥쿼리움 퀘스트 상세 팝업 UI
    // NyangQuariumQuestManager 데이터를 화면에 바인딩하고 완료/찾기 동작 처리
    public sealed class NyangQuariumQuestPopUp : UIPopup
    {
        private const string CompleteLabel = "완료";
        private const string FindLabel = "찾기";

        private const string PanelQuestKey = "NQ_Panel_Quest";
        private const string PanelQuestInfoKey = "NQ_Panel_QuestInfo";
        private const string PanelQuestRewardKey = "NQ_Panel_QuestReward";
        private const string BarProgressKey = "NQ_Bar_Progress";
        private const string ProgressFillKey = "Shape_Rectangle";
        private const string MarkerNormalKey = "NQ_Marker_normal";
        private const string MarkerClearKey = "NQ_Marker_clear";
        private const string BtnCloseKey = "Btn_Close";
        private const string CheckIconKey = "FM_Icon_Check";
        private const string AlertBadgeKey = "Shape_Circle";
        private const string ExpRewardIconKey = "NQ_Icon_ExpLarge";
        private const string CoinIconKey = "Main_Icon_Coin";

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
        private readonly List<QuestPopUpSpriteController> _spriteControllers = new();
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

        // Main 퀘스트 상단 스토리 테마, 챕터 진행률 (호출하지 않으면 해당 영역 숨김)
        public void SetStoryContext(string storyThemeName, string chapterTitle, float chapterProgress01)
        {
            _storyThemeName = storyThemeName;
            _chapterTitle = chapterTitle;
            _chapterProgress = Mathf.Clamp01(chapterProgress01);
            RefreshStoryAndChapter();
        }

        // 알아서 연결 해 주는 함수
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

        // 중복 등록 방지를 위해 Remove 후 Add
        private void BindAddressableSprites(bool force = false)
        {
            EnsureRequiredSpriteKeys();

            if (_staticSpritesBound && !force)
                return;

            if (force)
            {
                DisposeSpriteControllers();
                _staticSpritesBound = false;
                _staticSpriteRequestCount = 0;
                _staticSpriteResolvedCount = 0;
            }

            BindSprite(_questPanelFrameImage, PanelQuestKey);
            BindSprite(_questInfoPanelImage, PanelQuestInfoKey);
            BindSprite(_rewardSlotPanelImage, PanelQuestRewardKey);
            BindSprite(_chapterProgressBackgroundImage, BarProgressKey);
            BindSprite(_chapterProgressFillImage, ProgressFillKey);
            BindSprite(_conditionCompleteMarkImage, CheckIconKey, true);
            BindSprite(_alertBadgeImage, AlertBadgeKey);
            BindButtonSprite(_closeButton, BtnCloseKey);
            BindButtonSprite(_completeButton, MarkerClearKey);
            BindButtonSprite(_findButton, MarkerNormalKey);

            _staticSpritesBound = true;
            TryShowPendingPopup();
        }

        private bool StaticSpritesReady =>
            _staticSpritesBound &&
            _staticSpriteRequestCount > 0 &&
            _staticSpriteResolvedCount >= _staticSpriteRequestCount;

        private void ShowPreparedPopup()
        {
            _openRequested = false;
            gameObject.SetActive(true);
            PlayOpenAnimation();
        }

        private void TryShowPendingPopup()
        {
            if (_openRequested && StaticSpritesReady)
                ShowPreparedPopup();
        }

        private static void EnsureRequiredSpriteKeys()
        {
            RegisterSpriteKeyIfMissing(PanelQuestKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(PanelQuestInfoKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(PanelQuestRewardKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(BarProgressKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(ProgressFillKey, AddressableGroupType.Common);
            RegisterSpriteKeyIfMissing(MarkerNormalKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(MarkerClearKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(BtnCloseKey, AddressableGroupType.Common);
            RegisterSpriteKeyIfMissing(CheckIconKey, AddressableGroupType.Finding);
            RegisterSpriteKeyIfMissing(AlertBadgeKey, AddressableGroupType.Common);
            RegisterSpriteKeyIfMissing(ExpRewardIconKey, AddressableGroupType.Nyangquarium);
            RegisterSpriteKeyIfMissing(CoinIconKey, AddressableGroupType.Main);
        }

        private static void RegisterSpriteKeyIfMissing(string key, AddressableGroupType groupType)
        {
            if (KeyContainer.Sprites.Contains(key))
                return;

            KeyContainer.Register(new KeyData
            {
                Key = key,
                FileName = key,
                Usage = "NyangQuariumQuestPopUp",
                GroupType = groupType,
                LabelType = LabelType.Sprite,
                BuildType = BuildType.Local
            });
        }

        private void BindButtonSprite(Button button, string key)
        {
            if (button == null || string.IsNullOrWhiteSpace(key))
                return;

            BindSprite(button.targetGraphic as Image, key, true);
        }

        private void BindSprite(Image image, string key, bool preserveAspect = false)
        {
            if (image == null || string.IsNullOrWhiteSpace(key))
                return;

            _staticSpriteRequestCount++;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;

            QuestPopUpSpriteController controller = new(image);
            controller.ChangeSprite(key, OnStaticSpriteResolved);
            _spriteControllers.Add(controller);
        }

        private void OnStaticSpriteResolved()
        {
            _staticSpriteResolvedCount++;
            TryShowPendingPopup();
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

        private void RefreshView()
        {
            if (_boundQuest == null)
                return;

            RefreshStoryAndChapter();
            RefreshQuestInfo();
            RefreshReward();
            RefreshActionButtons();
        }

        // QuestSection == Main 이고 SetStoryContext 값이 있을 때만 표시
        private void RefreshStoryAndChapter()
        {
            bool isMainQuest = _boundQuest.QuestSection == NyangQuariumQuestSection.Main;
            bool hasTheme = isMainQuest && !string.IsNullOrWhiteSpace(_storyThemeName);

            if (_storyThemeRoot != null)
                _storyThemeRoot.SetActive(hasTheme);

            if (_storyThemeText != null && hasTheme)
                _storyThemeText.text = _storyThemeName;

            bool hasChapter = isMainQuest && !string.IsNullOrWhiteSpace(_chapterTitle);

            if (_chapterProgressRoot != null)
                _chapterProgressRoot.SetActive(hasChapter);

            if (!hasChapter)
                return;

            if (_chapterTitleText != null)
                _chapterTitleText.text = _chapterTitle;

            if (_chapterProgressSlider != null)
                _chapterProgressSlider.value = _chapterProgress;

            if (_chapterProgressText != null)
                _chapterProgressText.text = $"{Mathf.RoundToInt(_chapterProgress * 100f)}%";
        }

        private void RefreshQuestInfo()
        {
            NyangQuariumQuestStringSO stringSO = ResolveStringSO();

            if (_questNameText != null)
            {
                _questNameText.text = stringSO != null
                    ? stringSO.GetStringOrKey(_boundQuest.QuestNameKey)
                    : _boundQuest.QuestNameKey;
            }

            bool hasRequiredResource = NyangQuariumQuestManager.Instance != null &&
                                       NyangQuariumQuestManager.Instance.HasConditionResource(
                                           _boundQuest.QuestCondition1,
                                           _boundQuest.ConditionAmount1);

            if (_conditionCompleteMark != null)
                _conditionCompleteMark.SetActive(hasRequiredResource);

            ApplyConditionIcon(_boundQuest.QuestCondition1);
        }

        private void RefreshReward()
        {
            bool hasReward = _boundQuest.HasReward;

            if (_rewardRoot != null)
                _rewardRoot.SetActive(hasReward);

            if (!hasReward)
                return;

            NyangQuariumQuestRewardSO rewardSO = ResolveRewardSO();
            int primaryAmount = _boundQuest.RewardAmount;

            if (rewardSO != null &&
                rewardSO.TryGetReward(_boundQuest.QuestRewardId, out NyangQuariumQuestRewardData rewardData))
            {
                if (rewardData.HasRewardItem1)
                {
                    ApplyItemIcon(rewardData.RewardItem1, _rewardIconPrimary);
                    primaryAmount = rewardData.RewardAmount1;
                }
                else if (rewardData.HasExpReward)
                {
                    ApplyExpRewardIcon();
                    primaryAmount = rewardData.ExpAmount;
                }
            }

            if (_rewardPrimaryText != null)
                _rewardPrimaryText.text = primaryAmount > 0 ? primaryAmount.ToString() : string.Empty;
        }

        // 조건 충족 -> 완료 버튼 + 알림 뱃지, 미충족 -> 찾기 버튼
        private void RefreshActionButtons()
        {
            bool canComplete = NyangQuariumQuestManager.Instance != null &&
                               NyangQuariumQuestManager.Instance.CanCompleteQuest(_boundQuest);

            if (_completeButton != null)
                _completeButton.gameObject.SetActive(canComplete);

            if (_findButton != null)
                _findButton.gameObject.SetActive(!canComplete);

            if (_alertBadge != null)
                _alertBadge.SetActive(canComplete);

            TMP_Text completeText = _completeButton != null
                ? _completeButton.GetComponentInChildren<TMP_Text>(true)
                : null;

            if (completeText != null)
                completeText.text = CompleteLabel;

            TMP_Text findText = _findButton != null
                ? _findButton.GetComponentInChildren<TMP_Text>(true)
                : null;

            if (findText != null)
                findText.text = FindLabel;
        }

        // 조건 문자열 : "Coin"/"코인" -> 코인 아이콘, 숫자 -> ItemDatabase 아이템 스프라이트
        private void ApplyConditionIcon(string condition)
        {
            if (_conditionIcon == null)
                return;

            if (IsCoinCondition(condition))
            {
                _conditionIconBindId = -1;

                LoadQuestSprite(
                    CoinIconKey,
                    (sprite, _) =>
                    {
                        if (_conditionIcon != null)
                        {
                            _conditionIcon.enabled = true;
                            _conditionIcon.sprite = sprite;
                        }
                    },
                    _ => ClearIcon(_conditionIcon));
                return;
            }

            if (int.TryParse(condition, out int itemId))
            {
                ApplyConditionItemIcon(itemId);
                return;
            }

            _conditionIconBindId = -1;
            ClearIcon(_conditionIcon);
        }

        private void ApplyConditionItemIcon(int itemId)
        {
            _conditionIconBindId = itemId;
            ApplyItemIconInternal(itemId, _conditionIcon, itemId);
        }

        private void ApplyItemIcon(int itemId, Image target)
        {
            ApplyItemIconInternal(itemId, target, -1);
        }

        private void ApplyItemIconInternal(int itemId, Image target, int conditionBindId)
        {
            if (target == null)
                return;

            if (!TryGetItemData(itemId, out ItemData itemData))
            {
                if (conditionBindId >= 0 && conditionBindId == _conditionIconBindId)
                    ClearIcon(target);
                return;
            }

            if (string.IsNullOrEmpty(itemData.AddressableKey))
            {
                if (itemData.ItemSprite != null)
                {
                    target.enabled = true;
                    target.sprite = itemData.ItemSprite;
                    return;
                }

                if (conditionBindId >= 0 && conditionBindId == _conditionIconBindId)
                    ClearIcon(target);
                return;
            }

            string addressableKey = itemData.AddressableKey;

            LoadQuestSprite(
                addressableKey,
                (sprite, _) =>
                {
                    if (target == null)
                        return;

                    if (conditionBindId >= 0 && conditionBindId != _conditionIconBindId)
                        return;

                    target.enabled = true;
                    target.sprite = sprite;
                },
                _ =>
                {
                    if (target == null)
                        return;

                    if (conditionBindId >= 0 && conditionBindId != _conditionIconBindId)
                        return;

                    ClearIcon(target);
                });
        }

        private void ApplyExpRewardIcon()
        {
            if (_rewardIconPrimary == null)
                return;

            LoadQuestSprite(
                ExpRewardIconKey,
                (sprite, _) =>
                {
                    if (_rewardIconPrimary == null)
                        return;

                    _rewardIconPrimary.enabled = true;
                    _rewardIconPrimary.sprite = sprite;
                },
                _ => ClearIcon(_rewardIconPrimary));
        }

        private void LoadQuestSprite(
            string key,
            Action<Sprite, AsyncOperationHandle<Sprite>> onLoaded,
            Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                onFailed?.Invoke(key);
                return;
            }

            GameManager.Addressable.LoadSprite(
                key,
                (sprite, handle) =>
                {
                    _loadedIconHandles.Add(handle);
                    onLoaded?.Invoke(sprite, handle);
                },
                failedKey =>
                {
                    DebugTool.Warning($"{failedKey} : QuestPopUp Sprite load failed", DebugType.UI);
                    onFailed?.Invoke(failedKey);
                });
        }

        private bool TryGetItemData(int itemId, out ItemData itemData)
        {
            itemData = null;

            if (itemId <= 0)
                return false;

            if (_itemDatabase != null && _itemDatabase.TryGetItemById(itemId, out itemData))
                return true;

            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out itemData))
                return true;

            return false;
        }

        private static void ClearIcon(Image target)
        {
            if (target == null)
                return;

            target.sprite = null;
            target.enabled = false;
        }

        private static bool IsCoinCondition(string condition)
        {
            return condition != null &&
                   (condition.Equals("Coin", StringComparison.OrdinalIgnoreCase) ||
                    condition.Equals("코인", StringComparison.OrdinalIgnoreCase));
        }

        private NyangQuariumQuestStringSO ResolveStringSO()
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumQuestStringSO(out NyangQuariumQuestStringSO stringSO))
                return stringSO;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            return quariumLoader != null ? quariumLoader.QuestStringSO : null;
        }

        private NyangQuariumQuestRewardSO ResolveRewardSO()
        {
            SheetLoader sheetLoader = FindFirstObjectByType<SheetLoader>();

            if (sheetLoader != null &&
                sheetLoader.TryGetNyangQuariumQuestRewardSO(out NyangQuariumQuestRewardSO rewardSO))
                return rewardSO;

            NyangQuariumSheetLoader quariumLoader = NyangQuariumSheetLoader.Instance;
            return quariumLoader != null ? quariumLoader.QuestRewardSO : null;
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
                             await NyangQuariumQuestManager.Instance.CompleteActiveQuestAsync();

            if (completed)
                ClosePopup();
        }

        // TODO : 조건에 맞는 맵/콘텐츠로 이동하는 찾기 동선 연결
        private void OnFindClicked()
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            if (_boundQuest == null)
                return;

            DebugTool.Log(
                $"[NyangQuariumQuestPopUp] 찾기 클릭. QuestId:{_boundQuest.ID}, Condition:{_boundQuest.QuestCondition1}",
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

        private sealed class QuestPopUpSpriteController
        {
            private readonly Image _image;
            private AsyncOperationHandle<Sprite> _handle;
            private int _requestId;
            private bool _disposed;

            public QuestPopUpSpriteController(Image image)
                => _image = image;

            public void ChangeSprite(string key, Action onResolved = null)
            {
                if (_disposed || _image == null || string.IsNullOrWhiteSpace(key))
                    return;

                int currentRequestId = ++_requestId;

                GameManager.Addressable.LoadSprite(
                    key,
                    (sprite, handle) =>
                    {
                        if (_disposed || currentRequestId != _requestId)
                        {
                            if (handle.IsValid())
                                Addressables.Release(handle);

                            return;
                        }

                        Release();

                        _handle = handle;
                        _image.sprite = sprite;
                        onResolved?.Invoke();
                    },
                    failedKey =>
                    {
                        if (_disposed || currentRequestId != _requestId)
                            return;

                        DebugTool.Warning($"{failedKey} : QuestPopUp Sprite load failed", DebugType.UI);
                        onResolved?.Invoke();
                    });
            }

            public void Dispose()
            {
                _disposed = true;
                ++_requestId;
                Release();
            }

            private void Release()
            {
                if (!_handle.IsValid())
                    return;

                Addressables.Release(_handle);
                _handle = default;
            }
        }
    }
}
