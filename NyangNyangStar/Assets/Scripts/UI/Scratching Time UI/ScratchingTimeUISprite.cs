using System.Collections.Generic;
using Core.Managers;
using Data.LibrarySystem;
using Data.ScriptableObjects.KeyContainerSO;
using Services.Enums;
using UI;
using UI.Base;
using Util;
using UnityEngine;
using UnityEngine.UI;

// ScratchingTimeUI 선택 화면에 Addressables 스프라이트를 적용
public class ScratchingTimeUISprite : UIBase
{
    // 스프라이트 키 이름
    private const string ShapeRectangle = "Shape_Rectangle";
    private const string ShapeRectangleOutline = "Shape_Rectangle_Outline";
    private const string ShapeSquare = "Shape_Square";
    private const string CloseButton = "Btn_Close";
    private const string NysContent = "NYS_Content";
    private const string HighlightCurved = "ST_Highlight_Curved";
    private const string CloudFace = "ST_BG_Cloud_Face";
    private const string CloudLeft = "ST_BG_Cloud_Left";
    private const string TileCats = "ST_Tile_Cats";

    // ScratchingTimeUI_Sprite 프리팹 기준 경로별 Addressable 매핑
    private static readonly Dictionary<string, string> SpriteKeysByPath = new()
    {
        ["Background"] = ShapeRectangle,
        ["Background/Outline"] = ShapeRectangleOutline,
        ["Background/Image"] = TileCats,
        ["Background/LeftCloudImage"] = CloudLeft,
        ["EventPopupPanel"] = NysContent,
        ["EventPopupPanel/HeaderPanel/ExitButton"] = CloseButton,
        ["EventPopupPanel/LevelSection/LevelRow/Panel/Background"] = ShapeRectangle,
        ["EventPopupPanel/LevelSection/LevelRow/Panel/Background/Image"] = CloudFace,
        ["EventPopupPanel/LevelSection/LevelRow/Panel/Background/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/LevelSection/Panel/InfoPanel/Background"] = ShapeRectangle,
        ["EventPopupPanel/LevelSection/Panel/InfoPanel/Background/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/LevelSection/Panel/Panel (1)/Slider/Fill Area/Fill"] = ShapeRectangle,
        ["EventPopupPanel/LevelSection/Panel/Panel (1)/Slider/Fill Area/Image"] = ShapeRectangle,
        ["EventPopupPanel/LevelSection/Panel/Panel (1)/Slider/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/DailyStageCard"] = ShapeSquare,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button/Highlight"] = HighlightCurved,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button (1)"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button (1)/Highlight"] = HighlightCurved,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/Button (1)/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/CenterPanel/Background"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/CenterPanel/CountPanel/Background"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/CenterPanel/Image"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/DailyStageCard/CardRow/CenterPanel/PortraitPanel/Image (1)"] = CloudFace,
        ["EventPopupPanel/Stage Panel/WeekStageCard"] = ShapeSquare,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/Button"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/Button/Highlight"] = HighlightCurved,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/Button/Outline"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/CenterPanel/Background"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/CenterPanel/CountPanel/Background"] = ShapeRectangle,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/CenterPanel/Image"] = ShapeRectangleOutline,
        ["EventPopupPanel/Stage Panel/WeekStageCard/CardRow/CenterPanel/PortraitPanel/Image (1)"] = CloudFace,
        ["EventPopupPanel/StageTabSectionPanel/StageTabSection/StageTab_1"] = ShapeRectangle,
        ["EventPopupPanel/StageTabSectionPanel/StageTabSection/StageTab_2"] = ShapeRectangle,
        ["EventPopupPanel/StageTabSectionPanel/StageTabSection/StageTab_3"] = ShapeRectangle,
        ["EventPopupPanel/StageTabSectionPanel/StageTabSection/StageTab_4"] = ShapeRectangle,
    }; // 나니모 나갓타

    private readonly List<UISpriteController> _spriteControllers = new();
    private bool _initialized;

    public override void Init()
    {
        // 중복 초기화 방지임
        if (_initialized)
            return;

        EnsureRequiredSpriteKeys();
        _initialized = true;
        ApplySprites();
    }

    private static void EnsureRequiredSpriteKeys()
    {
        // 필요한 스프라이트 키 등록
        RegisterSpriteKeyIfMissing(ShapeRectangle, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(ShapeRectangleOutline, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(ShapeSquare, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(CloseButton, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(NysContent, AddressableGroupType.Nyangstagram);
        RegisterSpriteKeyIfMissing(HighlightCurved, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(CloudFace, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(CloudLeft, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(TileCats, AddressableGroupType.Scratching);
    }

    private static void RegisterSpriteKeyIfMissing(string key, AddressableGroupType groupType)
    {
        // 이미 등록된 키는 제외
        if (KeyContainer.Sprites.Contains(key))
            return;

        KeyContainer.Register(new KeyData
        {
            Key = key,
            FileName = key,
            Usage = "ScratchingTimeUI",
            GroupType = groupType,
            LabelType = LabelType.Sprite,
            BuildType = BuildType.Local
        });
    }

    private void ApplySprites()
    {
        // 하위 Image를 경로 기준으로 순회하며 스프라이트 적용
        _spriteControllers.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            string relativePath = GetRelativePath(image.transform);
            if (string.IsNullOrEmpty(relativePath))
                continue;

            if (!SpriteKeysByPath.TryGetValue(relativePath, out string spriteKey))
                continue;

            UISpriteController controller = new(image);
            controller.ChangeSprite(spriteKey);
            _spriteControllers.Add(controller);
        }
    }

    private string GetRelativePath(Transform target)
    {
        // 루트 기준 상대 경로 문자열 생성
        if (target == null || target == transform)
            return string.Empty;

        var parts = new List<string>();
        Transform current = target;
        while (current != null && current != transform)
        {
            parts.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }

    private void OnDestroy()
    {
        // 로드한 스프라이트 해제
        foreach (UISpriteController controller in _spriteControllers)
            controller?.ReleaseSprite();
    }
}
