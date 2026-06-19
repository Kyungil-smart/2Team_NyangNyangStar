using System;

namespace UI.FindMoongchi
{
    public static class FindMoongchiPanelEventBinder
    {
        public static void Bind(
            FindMoongchiMainPanel mainPanel,
            FindMoongchiGamePanel gamePanel,
            FindMoongchiMissionPanel missionPanel,
            FindMoongchiShopPanel shopPanel,
            Action onGameButtonClicked,
            Action onMissionButtonClicked,
            Action onShopButtonClicked,
            Action onCloseButtonClicked,
            Action onBackButtonClicked,
            Action<int, int> onToolDropped,
            Action<int> onClaimMissionClicked,
            Action<FindMoongchiShopViewData> onShopItemClicked)
        {
            Unbind(
                mainPanel,
                gamePanel,
                missionPanel,
                shopPanel,
                onGameButtonClicked,
                onMissionButtonClicked,
                onShopButtonClicked,
                onCloseButtonClicked,
                onBackButtonClicked,
                onToolDropped,
                onClaimMissionClicked,
                onShopItemClicked);

            if (mainPanel != null)
            {
                mainPanel.OnGameButtonClicked += onGameButtonClicked;
                mainPanel.OnMissionButtonClicked += onMissionButtonClicked;
                mainPanel.OnShopButtonClicked += onShopButtonClicked;
                mainPanel.OnCloseButtonClicked += onCloseButtonClicked;
            }

            if (gamePanel != null)
            {
                gamePanel.OnBackButtonClicked += onBackButtonClicked;
                gamePanel.OnToolDropped += onToolDropped;
            }

            if (missionPanel != null)
            {
                missionPanel.OnBackButtonClicked += onBackButtonClicked;
                missionPanel.OnClaimMissionClicked += onClaimMissionClicked;
            }

            if (shopPanel != null)
            {
                shopPanel.OnBackButtonClicked += onBackButtonClicked;
                shopPanel.OnShopItemClicked += onShopItemClicked;
            }
        }

        public static void Unbind(
            FindMoongchiMainPanel mainPanel,
            FindMoongchiGamePanel gamePanel,
            FindMoongchiMissionPanel missionPanel,
            FindMoongchiShopPanel shopPanel,
            Action onGameButtonClicked,
            Action onMissionButtonClicked,
            Action onShopButtonClicked,
            Action onCloseButtonClicked,
            Action onBackButtonClicked,
            Action<int, int> onToolDropped,
            Action<int> onClaimMissionClicked,
            Action<FindMoongchiShopViewData> onShopItemClicked)
        {
            if (mainPanel != null)
            {
                mainPanel.OnGameButtonClicked -= onGameButtonClicked;
                mainPanel.OnMissionButtonClicked -= onMissionButtonClicked;
                mainPanel.OnShopButtonClicked -= onShopButtonClicked;
                mainPanel.OnCloseButtonClicked -= onCloseButtonClicked;
            }

            if (gamePanel != null)
            {
                gamePanel.OnBackButtonClicked -= onBackButtonClicked;
                gamePanel.OnToolDropped -= onToolDropped;
            }

            if (missionPanel != null)
            {
                missionPanel.OnBackButtonClicked -= onBackButtonClicked;
                missionPanel.OnClaimMissionClicked -= onClaimMissionClicked;
            }

            if (shopPanel != null)
            {
                shopPanel.OnBackButtonClicked -= onBackButtonClicked;
                shopPanel.OnShopItemClicked -= onShopItemClicked;
            }
        }
    }
}
