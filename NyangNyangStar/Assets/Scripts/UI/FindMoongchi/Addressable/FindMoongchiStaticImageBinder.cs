using System.Collections.Generic;
using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    public sealed class FindMoongchiStaticImageBinder : MonoBehaviour
    {
        [Header("옵션")]
        [SerializeField] private bool _loadOnEnable = true;

        [Header("메인 화면")]
        [SerializeField] private Image _mainPanelImage;
        [SerializeField] private Image _mainImage;
        [SerializeField] private Image _missionButtonImage;
        [SerializeField] private Image _shopButtonImage;

        [Header("미니게임 화면")]
        [SerializeField] private Image _gameBoardBackgroundImage;
        [SerializeField] private Image _minigamePanelImage;
        [SerializeField] private Image _timerIconImage;
        [SerializeField] private Image _helpButtonImage;
        [SerializeField] private Image _backButtonImage;
        [SerializeField] private Image _infoPanelImage;
        [SerializeField] private Image _targetTextPanelImage;

        [Header("미션 화면")]
        [SerializeField] private Image _missionPanelImage;

        [Header("상점 화면")]
        [SerializeField] private Image _eventCoinIconImage;

        [Header("미니게임 도구 패널")]
        [SerializeField] private Image _toolPanelImage;

        [Header("설명 / 서브 패널")]
        [Tooltip("FM_Panel_Sub를 사용할 이미지들입니다. 예: PurchasePopup, NoticePopup, ErrorPopup 배경")]
        [SerializeField] private Image[] _subPanelImages;

        [Header("공통 도형")]
        [SerializeField] private Image[] _squareImages;
        [SerializeField] private Image[] _rectangleImages;
        [SerializeField] private Image[] _rectangleOutlineImages;

        private readonly List<UISpriteController> _controllers = new();

        private void OnEnable()
        {
            if (_loadOnEnable)
                LoadAll();
        }

        public void LoadAll()
        {
            ReleaseAll();

            Load(_mainPanelImage, FindMoongchiSpriteKeys.PanelMain);
            Load(_mainImage, FindMoongchiSpriteKeys.MainImage);
            Load(_missionButtonImage, FindMoongchiSpriteKeys.ButtonMission);
            Load(_shopButtonImage, FindMoongchiSpriteKeys.ButtonShop);

            Load(_gameBoardBackgroundImage, FindMoongchiSpriteKeys.GameBoardBackground);
            Load(_minigamePanelImage, FindMoongchiSpriteKeys.PanelMinigame);
            Load(_timerIconImage, FindMoongchiSpriteKeys.IconTimer);
            Load(_helpButtonImage, FindMoongchiSpriteKeys.ButtonHelp);
            Load(_backButtonImage, FindMoongchiSpriteKeys.ButtonBack);
            Load(_infoPanelImage, FindMoongchiSpriteKeys.PanelInfo);
            Load(_targetTextPanelImage, FindMoongchiSpriteKeys.PanelTargetText);

            Load(_missionPanelImage, FindMoongchiSpriteKeys.PanelMission);

            Load(_eventCoinIconImage, FindMoongchiSpriteKeys.EventCoinIcon);
            Load(_toolPanelImage, FindMoongchiSpriteKeys.PanelItemList);

            LoadArray(_subPanelImages, FindMoongchiSpriteKeys.PanelSub);
            LoadArray(_squareImages, FindMoongchiSpriteKeys.ShapeSquare);
            LoadArray(_rectangleImages, FindMoongchiSpriteKeys.ShapeRectangle);
            LoadArray(_rectangleOutlineImages, FindMoongchiSpriteKeys.ShapeRectangleOutline);

            DebugTool.Log("[FindMoongchiStaticImageBinder] 고정 이미지 로드 요청 완료", DebugType.FindMoongchi, this);
        }

        private void LoadArray(Image[] images, string key)
        {
            if (images == null)
                return;

            for (int i = 0; i < images.Length; i++)
                Load(images[i], key);
        }

        private void Load(Image image, string key)
        {
            if (image == null)
                return;

            if (string.IsNullOrWhiteSpace(key))
            {
                DebugTool.Warning("[FindMoongchiStaticImageBinder] Sprite Key가 비어있습니다.", DebugType.FindMoongchi, image);
                return;
            }

            UISpriteController controller = new UISpriteController(image);
            _controllers.Add(controller);
            controller.ChangeSprite(key);
        }

        private void ReleaseAll()
        {
            for (int i = 0; i < _controllers.Count; i++)
                _controllers[i]?.Dispose();

            _controllers.Clear();
        }

        private void OnDisable()
        {
            ReleaseAll();
        }

        private void OnDestroy()
        {
            ReleaseAll();
        }
    }
}
