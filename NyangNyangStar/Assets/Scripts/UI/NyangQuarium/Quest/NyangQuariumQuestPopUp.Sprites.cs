using Data.ScriptableObjects.KeyContainerSO;
using Services.Enums;
using UnityEngine.UI;
using Util;

namespace UI.NyangQuarium.Quest
{
    public sealed partial class NyangQuariumQuestPopUp
    {
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

        private bool StaticSpritesReady =>
            _staticSpritesBound &&
            _staticSpriteRequestCount > 0 &&
            _staticSpriteResolvedCount >= _staticSpriteRequestCount;

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

            NyangQuariumQuestPopUpSpriteController controller = new(image);
            controller.ChangeSprite(key, OnStaticSpriteResolved);
            _spriteControllers.Add(controller);
        }

        private void OnStaticSpriteResolved()
        {
            _staticSpriteResolvedCount++;
            TryShowPendingPopup();
        }
    }
}
