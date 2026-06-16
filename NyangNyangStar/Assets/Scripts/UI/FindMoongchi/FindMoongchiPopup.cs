using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.HideAndSeekSO;
using Data.ScriptableObjects.MergeBoard;
using UI.Base;
using UnityEngine;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiPopup : UIPopup
    {
        [Header("Panels")]
        [SerializeField] private FindMoongchiMainPanel _mainPanel;
        [SerializeField] private FindMoongchiGamePanel _gamePanel;
        [SerializeField] private FindMoongchiMissionPanel _missionPanel;
        [SerializeField] private FindMoongchiShopPanel _shopPanel;

        [Header("Modal")]
        [SerializeField] private GameObject _modalLayer;
        [SerializeField] private PurchasePopupView _purchasePopup;
        [SerializeField] private NoticePopupView _noticePopup;
        [SerializeField] private ErrorPopupView _errorPopup;

        [Header("Data")]
        [Tooltip("같은 오브젝트에 붙어있으면 비워도 자동으로 찾습니다.")]
        [SerializeField] private FindMoongchiDataManager _dataManager;

        [Header("임시 유저 값 - Firestore 연결 전")]
        [SerializeField] private int _eventCoin = 1000;
        [SerializeField] private int _searchChance = FindMoongchiConstants.DailySearchChance;
        [SerializeField] private int _energySpendProgress;
        [SerializeField] private int _currentWeek = 1;
        [SerializeField] private string _remainTimeText = "1d 12h";
        [SerializeField] private bool _completedMissionMock;

        private readonly HashSet<int> _revealedTileIndices = new();
        private readonly Dictionary<int, int> _shopPurchaseCounts = new();
        private readonly HashSet<int> _claimedMissionIds = new();

        private bool _initialized;
        private bool _isUsingTool;
        private FindMoongchiPanelType _currentPanelType = FindMoongchiPanelType.Main;

        private HideAndSeekShopSO ShopSO => _dataManager != null ? _dataManager.ShopSO : null;
        private HideAndSeekMissionSO MissionSO => _dataManager != null ? _dataManager.MissionSO : null;
        private HideAndSeekProfileSO ProfileSO => _dataManager != null ? _dataManager.ProfileSO : null;

        public override void Init()
        {
            ResolveReferences();
            ResolveDataManager();
            InitChildren();
            BindEvents();

            _initialized = true;
            ShowPanel(FindMoongchiPanelType.Main);
        }

        public override void PlayOpenAnimation()
        {
            if (!_initialized)
                Init();

            ShowPanel(FindMoongchiPanelType.Main);
        }

        public void ShowPanel(FindMoongchiPanelType panelType)
        {
            _currentPanelType = panelType;

            SetActive(_mainPanel, panelType == FindMoongchiPanelType.Main);
            SetActive(_gamePanel, panelType == FindMoongchiPanelType.Game);
            SetActive(_missionPanel, panelType == FindMoongchiPanelType.Mission);
            SetActive(_shopPanel, panelType == FindMoongchiPanelType.Shop);

            CloseAllModal();

            switch (panelType)
            {
                case FindMoongchiPanelType.Game:
                    RefreshGamePanel();
                    break;
                case FindMoongchiPanelType.Mission:
                    RefreshMissionPanel();
                    break;
                case FindMoongchiPanelType.Shop:
                    RefreshShopPanel();
                    break;
            }
        }

        public void OpenNotice(string message)
        {
            OpenModalLayer();
            _noticePopup?.Open(message);
        }

        public void OpenError(string message, int errorCode = 0)
        {
            OpenModalLayer();
            _errorPopup?.Open(message, errorCode);
        }

        public override void ClosePopup()
        {
            CloseAllModal();
            GameManager.UI.ClosePopupUI(this);
        }

        private void ResolveReferences()
        {
            if (_mainPanel == null)
                _mainPanel = GetComponentInChildren<FindMoongchiMainPanel>(true);

            if (_gamePanel == null)
                _gamePanel = GetComponentInChildren<FindMoongchiGamePanel>(true);

            if (_missionPanel == null)
                _missionPanel = GetComponentInChildren<FindMoongchiMissionPanel>(true);

            if (_shopPanel == null)
                _shopPanel = GetComponentInChildren<FindMoongchiShopPanel>(true);

            if (_purchasePopup == null)
                _purchasePopup = GetComponentInChildren<PurchasePopupView>(true);

            if (_noticePopup == null)
                _noticePopup = GetComponentInChildren<NoticePopupView>(true);

            if (_errorPopup == null)
                _errorPopup = GetComponentInChildren<ErrorPopupView>(true);

            if (_modalLayer == null && _purchasePopup != null)
                _modalLayer = _purchasePopup.transform.parent != null ? _purchasePopup.transform.parent.gameObject : null;
        }

        private void ResolveDataManager()
        {
            if (_dataManager == null)
                _dataManager = GetComponent<FindMoongchiDataManager>();

            if (_dataManager == null)
                _dataManager = GetComponentInChildren<FindMoongchiDataManager>(true);

            if (_dataManager == null)
                _dataManager = FindFirstObjectByType<FindMoongchiDataManager>();

            if (_dataManager != null)
            {
                _dataManager.OnLoadCompleted -= HandleDataLoadCompleted;
                _dataManager.OnLoadCompleted += HandleDataLoadCompleted;
            }
        }

        private void InitChildren()
        {
            _mainPanel?.Init();
            _gamePanel?.Init();
            _missionPanel?.Init();
            _shopPanel?.Init();
            _purchasePopup?.Init();
            _noticePopup?.Init();
            _errorPopup?.Init();

            CloseAllModal();
        }

        private void BindEvents()
        {
            if (_mainPanel != null)
            {
                _mainPanel.OnGameButtonClicked -= HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked -= HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked -= HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked -= HandleCloseButtonClicked;

                _mainPanel.OnGameButtonClicked += HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked += HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked += HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked += HandleCloseButtonClicked;
            }

            if (_gamePanel != null)
            {
                _gamePanel.OnBackButtonClicked -= HandleBackToMain;
                _gamePanel.OnToolDropped -= HandleToolDropped;
                _gamePanel.OnBackButtonClicked += HandleBackToMain;
                _gamePanel.OnToolDropped += HandleToolDropped;
            }

            if (_missionPanel != null)
            {
                _missionPanel.OnBackButtonClicked -= HandleBackToMain;
                _missionPanel.OnClaimMissionClicked -= HandleClaimMissionClicked;
                _missionPanel.OnBackButtonClicked += HandleBackToMain;
                _missionPanel.OnClaimMissionClicked += HandleClaimMissionClicked;
            }

            if (_shopPanel != null)
            {
                _shopPanel.OnBackButtonClicked -= HandleBackToMain;
                _shopPanel.OnShopItemClicked -= HandleShopItemClicked;
                _shopPanel.OnBackButtonClicked += HandleBackToMain;
                _shopPanel.OnShopItemClicked += HandleShopItemClicked;
            }
        }

        private void HandleDataLoadCompleted()
        {
            if (!_initialized)
                return;

            RefreshCurrentPanel();
        }

        private void RefreshCurrentPanel()
        {
            switch (_currentPanelType)
            {
                case FindMoongchiPanelType.Game:
                    RefreshGamePanel();
                    break;
                case FindMoongchiPanelType.Mission:
                    RefreshMissionPanel();
                    break;
                case FindMoongchiPanelType.Shop:
                    RefreshShopPanel();
                    break;
            }
        }

        private void HandleGameButtonClicked() => ShowPanel(FindMoongchiPanelType.Game);
        private void HandleMissionButtonClicked() => ShowPanel(FindMoongchiPanelType.Mission);
        private void HandleShopButtonClicked() => ShowPanel(FindMoongchiPanelType.Shop);
        private void HandleBackToMain() => ShowPanel(FindMoongchiPanelType.Main);
        private void HandleCloseButtonClicked() => ClosePopup();

        private async void HandleToolDropped(int toolItemId, int tileIndex)
        {
            if (_isUsingTool)
                return;

            if (_searchChance <= 0)
            {
                OpenNotice("탐색 기회가 없습니다.");
                return;
            }

            int boardItemCount = FindMoongchiMergeBoardBridge.GetBoardItemCountById(toolItemId);

            if (boardItemCount <= 0)
            {
                OpenNotice("보유한 탐색 도구가 없습니다.");
                return;
            }

            _isUsingTool = true;

            try
            {
                bool consumed = await FindMoongchiMergeBoardBridge.ConsumeBoardItemByIdAsync(toolItemId, 1);

                if (!consumed)
                {
                    OpenError("탐색 도구 소비에 실패했습니다.", 1001);
                    return;
                }

                _searchChance = Mathf.Max(0, _searchChance - 1);

                IReadOnlyList<int> affectedTiles = _gamePanel.GetAffectedTiles(toolItemId, tileIndex);
                foreach (int affectedTile in affectedTiles)
                    _revealedTileIndices.Add(affectedTile);

                _gamePanel.RevealTiles(affectedTiles);
                RefreshGamePanel();
            }
            finally
            {
                _isUsingTool = false;
            }
        }

        private void HandleClaimMissionClicked(int missionId)
        {
            FindMoongchiMissionViewData mission = BuildMissionViewDataById(missionId);

            if (mission == null)
            {
                OpenError("미션 정보를 찾을 수 없습니다.", 2001);
                return;
            }

            if (mission.State == FindMoongchiMissionSlotState.Claimed)
            {
                OpenNotice("이미 수령한 미션입니다.");
                return;
            }

            if (mission.State != FindMoongchiMissionSlotState.Completed)
            {
                OpenNotice("아직 완료하지 않은 미션입니다.");
                return;
            }

            ApplyMissionReward(mission.Reward1);
            ApplyMissionReward(mission.Reward2);
            _claimedMissionIds.Add(missionId);

            OpenNotice("미션 보상을 수령했습니다.");
            RefreshMissionPanel();
        }

        private void HandleShopItemClicked(FindMoongchiShopViewData data)
        {
            if (data == null)
                return;

            int maxBuyCount = GetMaxBuyCount(data);

            if (maxBuyCount <= 0)
            {
                if (data.IsSoldOut)
                    OpenNotice("구매 횟수를 모두 사용했습니다.");
                else
                    OpenNotice("이벤트 재화가 부족합니다.");

                return;
            }

            OpenModalLayer();
            _purchasePopup?.Open(data, maxBuyCount, HandlePurchaseConfirmed);
        }

        private void HandlePurchaseConfirmed(FindMoongchiShopViewData data, int count)
        {
            if (data == null || count <= 0)
                return;

            int totalCost = data.CostAmount * count;

            if (_eventCoin < totalCost)
            {
                OpenNotice("이벤트 재화가 부족합니다.");
                return;
            }

            if (data.HasLimit && data.PurchasedCount + count > data.LimitCount)
            {
                OpenNotice("구매 가능 횟수를 초과했습니다.");
                return;
            }

            _eventCoin -= totalCost;

            if (!_shopPurchaseCounts.ContainsKey(data.ShopItemId))
                _shopPurchaseCounts[data.ShopItemId] = 0;

            _shopPurchaseCounts[data.ShopItemId] += count;

            _purchasePopup?.Close();
            OpenNotice("구매가 완료되었습니다.");
            RefreshShopPanel();
        }

        private void RefreshGamePanel()
        {
            _gamePanel?.SetData(BuildGameViewData());
        }

        private FindMoongchiGameViewData BuildGameViewData()
        {
            FindMoongchiGameViewData data = new FindMoongchiGameViewData
            {
                CurrentWeek = _currentWeek,
                RemainTimeText = _remainTimeText,
                SearchChance = _searchChance,
                EnergySpendProgress = _energySpendProgress,
                EnergySpendTarget = FindMoongchiConstants.EnergySpendTarget,
                RevealedTileIndices = new HashSet<int>(_revealedTileIndices)
            };

            foreach (int toolId in FindMoongchiConstants.ToolIds)
            {
                data.Tools.Add(new FindMoongchiToolViewData
                {
                    ToolItemId = toolId,
                    ToolName = GetItemName(toolId),
                    Icon = GetItemSprite(toolId),
                    Count = FindMoongchiMergeBoardBridge.GetBoardItemCountById(toolId),
                    IsUsable = _searchChance > 0
                });
            }

            data.TargetHints.Add(new FindMoongchiTargetHintViewData { TargetName = "뭉치", IsFound = false });
            data.TargetHints.Add(new FindMoongchiTargetHintViewData { TargetName = "목표물 1", IsFound = false });
            data.TargetHints.Add(new FindMoongchiTargetHintViewData { TargetName = "목표물 2", IsFound = false });

            return data;
        }

        private void RefreshMissionPanel()
        {
            _missionPanel?.SetData(BuildMissionViewDataList());
        }

        private List<FindMoongchiMissionViewData> BuildMissionViewDataList()
        {
            List<FindMoongchiMissionViewData> result = new List<FindMoongchiMissionViewData>();
            HideAndSeekMissionSO missionSO = MissionSO;

            if (missionSO == null)
                return result;

            foreach (HideAndSeekMissionData mission in missionSO.Missions)
            {
                if (mission == null)
                    continue;

                result.Add(BuildMissionViewData(mission));
            }

            return result;
        }

        private FindMoongchiMissionViewData BuildMissionViewDataById(int missionId)
        {
            HideAndSeekMissionSO missionSO = MissionSO;

            if (missionSO == null || !missionSO.TryGetMission(missionId, out HideAndSeekMissionData mission))
                return null;

            return BuildMissionViewData(mission);
        }

        private FindMoongchiMissionViewData BuildMissionViewData(HideAndSeekMissionData mission)
        {
            int currentAmount = _completedMissionMock ? mission.TargetAmount : 0;
            FindMoongchiMissionSlotState state = FindMoongchiMissionSlotState.InProgress;

            if (_claimedMissionIds.Contains(mission.ID))
                state = FindMoongchiMissionSlotState.Claimed;
            else if (currentAmount >= mission.TargetAmount)
                state = FindMoongchiMissionSlotState.Completed;

            return new FindMoongchiMissionViewData
            {
                MissionId = mission.ID,
                MissionType = mission.MissionType,
                MissionDescription = mission.MissionContent,
                CurrentAmount = currentAmount,
                TargetAmount = mission.TargetAmount,
                State = state,
                Reward1 = mission.Reward1,
                Reward2 = mission.Reward2
            };
        }

        private void RefreshShopPanel()
        {
            List<FindMoongchiShopViewData> itemProducts = new List<FindMoongchiShopViewData>();
            List<FindMoongchiShopViewData> profileProducts = new List<FindMoongchiShopViewData>();
            HideAndSeekShopSO shopSO = ShopSO;

            if (shopSO != null)
            {
                foreach (HideAndSeekShopItemData item in shopSO.ShopItems)
                {
                    if (item == null)
                        continue;

                    FindMoongchiShopViewData data = BuildShopViewData(item);

                    if (data.ProductType == HideAndSeekProductType.PROFILE)
                        profileProducts.Add(data);
                    else
                        itemProducts.Add(data);
                }
            }

            _shopPanel?.SetData(_eventCoin, itemProducts, profileProducts);
        }

        private FindMoongchiShopViewData BuildShopViewData(HideAndSeekShopItemData item)
        {
            _shopPurchaseCounts.TryGetValue(item.ID, out int purchasedCount);

            return new FindMoongchiShopViewData
            {
                ShopItemId = item.ID,
                ProductType = item.ProductType,
                ProductId = item.ProductID,
                ProductName = GetProductName(item.ProductType, item.ProductID),
                Icon = GetProductSprite(item.ProductType, item.ProductID),
                Quantity = item.Quantity,
                CostType = item.Cost,
                CostAmount = item.CostAmount,
                LimitCount = item.LimitCount,
                PurchasedCount = purchasedCount
            };
        }

        private int GetMaxBuyCount(FindMoongchiShopViewData data)
        {
            if (data == null || data.CostAmount <= 0)
                return 0;

            int affordableCount = _eventCoin / data.CostAmount;
            int remainingLimit = data.HasLimit ? data.RemainingLimit : int.MaxValue;
            return Mathf.Max(0, Mathf.Min(affordableCount, remainingLimit));
        }

        private void ApplyMissionReward(HideAndSeekRewardData reward)
        {
            if (reward == null || !reward.IsValid)
                return;

            switch (reward.RewardType)
            {
                case HideAndSeekCurrencyType.EVENT_COIN:
                    _eventCoin += reward.RewardAmount;
                    break;
                case HideAndSeekCurrencyType.ENERGY:
                    DebugTool.Log($"에너지 보상 지급 예정: {reward.RewardAmount}", DebugType.UI, this);
                    break;
            }
        }

        private string GetProductName(HideAndSeekProductType productType, int productId)
        {
            if (productType == HideAndSeekProductType.ITEM)
                return GetItemName(productId);

            HideAndSeekProfileSO profileSO = ProfileSO;
            if (productType == HideAndSeekProductType.PROFILE && profileSO != null && profileSO.TryGetProfile(productId, out HideAndSeekProfileData profile))
                return profile.ProfileName;

            if (productType == HideAndSeekProductType.CURRENCY)
                return productId switch
                {
                    1 => "에너지",
                    2 => "골드",
                    3 => "이벤트 코인",
                    _ => $"재화 {productId}"
                };

            return $"{productType} {productId}";
        }

        private Sprite GetProductSprite(HideAndSeekProductType productType, int productId)
        {
            if (productType == HideAndSeekProductType.ITEM)
                return GetItemSprite(productId);

            return null;
        }

        private string GetItemName(int itemId)
        {
            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out ItemData itemData) &&
                itemData != null &&
                !string.IsNullOrEmpty(itemData.ItemName))
            {
                return itemData.ItemName;
            }

            return itemId.ToString();
        }

        private Sprite GetItemSprite(int itemId)
        {
            if (LocalDataAccess.Instance?.Game != null &&
                LocalDataAccess.Instance.Game.TryGetMergeBoardItemById(itemId, out ItemData itemData) &&
                itemData != null)
            {
                return itemData.ItemSprite;
            }

            return null;
        }

        private void OpenModalLayer()
        {
            if (_modalLayer != null)
                _modalLayer.SetActive(true);
        }

        private void CloseAllModal()
        {
            _purchasePopup?.Close();
            _noticePopup?.Close();
            _errorPopup?.Close();

            if (_modalLayer != null)
                _modalLayer.SetActive(false);
        }

        private static void SetActive(MonoBehaviour target, bool isActive)
        {
            if (target != null)
                target.gameObject.SetActive(isActive);
        }

        private void OnDestroy()
        {
            if (_dataManager != null)
                _dataManager.OnLoadCompleted -= HandleDataLoadCompleted;

            if (_mainPanel != null)
            {
                _mainPanel.OnGameButtonClicked -= HandleGameButtonClicked;
                _mainPanel.OnMissionButtonClicked -= HandleMissionButtonClicked;
                _mainPanel.OnShopButtonClicked -= HandleShopButtonClicked;
                _mainPanel.OnCloseButtonClicked -= HandleCloseButtonClicked;
            }

            if (_gamePanel != null)
            {
                _gamePanel.OnBackButtonClicked -= HandleBackToMain;
                _gamePanel.OnToolDropped -= HandleToolDropped;
            }

            if (_missionPanel != null)
            {
                _missionPanel.OnBackButtonClicked -= HandleBackToMain;
                _missionPanel.OnClaimMissionClicked -= HandleClaimMissionClicked;
            }

            if (_shopPanel != null)
            {
                _shopPanel.OnBackButtonClicked -= HandleBackToMain;
                _shopPanel.OnShopItemClicked -= HandleShopItemClicked;
            }
        }
    }
}
