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

// ScratchingTimeScreen의 Week 스테이지 UI에 Addressables 스프라이트를 적용함
public class ScratchingWeekStageSprite : UIBase
{
    // 스프라이트 키 이름
    private const string ShapeRectangle = "Shape_Rectangle";
    private const string ShapeRectangleOutline = "Shape_Rectangle_Outline";
    private const string CloseButton = "Btn_Close";
    private const string BossWeek = "ST_Boss_Week";
    private const string TileCats = "ST_Tile_Cats";

    // UI 경로별 Addressable 스프라이트 매핑
    private static readonly Dictionary<string, string> SpriteKeysByPath = new()
    {
        ["ExitButton"] = CloseButton,
        ["Image"] = CloseButton, // standalone 프리팹 닫기 버튼 호환용
        ["BattlePopupPanel"] = ShapeRectangle,
        ["BattlePopupPanel/Outline"] = ShapeRectangleOutline,
        ["BattlePopupPanel/Tile"] = TileCats,
        ["BattlePopupPanel/ContentPanel/ScratcherImage"] = BossWeek,
        ["BattlePopupPanel/GaugePanel/DurabilitySlider (1)/Fill Area/Fill"] = ShapeRectangle,
        ["BattlePopupPanel/GaugePanel/DurabilitySlider (1)/Outline"] = ShapeRectangleOutline,
        ["BattlePopupPanel/GaugePanel/DurabilitySlider/Background"] = ShapeRectangle,
        ["BattlePopupPanel/GaugePanel/DurabilitySlider/Fill Area/Fill"] = ShapeRectangle,
        ["BattlePopupPanel/GaugePanel/DurabilitySlider/Outline"] = ShapeRectangleOutline,
        ["BattlePopupPanel/GaugePanel/TouchArea/Background"] = ShapeRectangle,
        ["BattlePopupPanel/GaugePanel/TouchArea/Outline"] = ShapeRectangleOutline,
        ["BattlePopupPanel/GaugePanel/TouchArea/Fill Area/Fill"] = ShapeRectangle,
    };

    private readonly List<UISpriteController> _spriteControllers = new();
    private bool _initialized;

    private void Start()
    {
        // 스프라이트 적용 전 기본 데이터 준비
        GameManager.Init();
        EnsureLocalDataAccess();
        EnsureRequiredSpriteKeys();
        Init();

        if (LocalDataAccess.Instance != null && !LocalDataAccess.Instance.Game.IsReady)
            GameManager.Data.LoadSheets();
    }

    public override void Init()
    {
        // 중복 초기화 방지
        if (_initialized)
            return;

        _initialized = true;
        ApplySprites();
    }

    private static void EnsureLocalDataAccess()
    {
        // 로컬 데이터 접근 객체 보장
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }

    private static void EnsureRequiredSpriteKeys()
    {
        // 필요한 스프라이트 키 등록
        RegisterSpriteKeyIfMissing(ShapeRectangle, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(ShapeRectangleOutline, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(CloseButton, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(BossWeek, AddressableGroupType.Scratching);
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
            Usage = "ScratchingWeekStageUI",
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
