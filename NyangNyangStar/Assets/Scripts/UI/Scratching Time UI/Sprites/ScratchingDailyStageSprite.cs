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

public class ScratchingDailyStageSprite : UIBase
{
    // 스프라이트 키 이름
    private const string ShapeRectangle = "Shape_Rectangle";
    private const string ShapeRectangleOutline = "Shape_Rectangle_Outline";
    private const string CloseButton = "Btn_Close";
    private const string BossDay = "ST_Boss_Day";
    private const string TileCats = "BG_Tile_Cats";

    // 교체할 이미지 컨트롤러 목록
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
        // 중복 초기화 방지임
        if (_initialized)
            return;

        _initialized = true;
        BindImages();
        SetSprites();
    }

    private static void EnsureLocalDataAccess()
    {
        // 로컬 데이터 접근 객체 보장임
        if (LocalDataAccess.Instance != null)
            return;

        new GameObject("@LocalDataAccess").AddComponent<LocalDataAccess>();
    }

    private static void EnsureRequiredSpriteKeys()
    {
        // 필요한 스프라이트 키 등록임
        RegisterSpriteKeyIfMissing(ShapeRectangle, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(ShapeRectangleOutline, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(CloseButton, AddressableGroupType.Common);
        RegisterSpriteKeyIfMissing(BossDay, AddressableGroupType.Scratching);
        RegisterSpriteKeyIfMissing(TileCats, AddressableGroupType.Scratching);
    }

    
    private static void RegisterSpriteKeyIfMissing(string key, AddressableGroupType groupType)
    {
        // 이미 등록된 키는 제외임
        if (KeyContainer.Sprites.Contains(key))
            return;

        KeyContainer.Register(new KeyData
        {
            Key = key,
            FileName = key,
            Usage = "ScratchingDailyStageUI",
            GroupType = groupType,
            LabelType = LabelType.Sprite,
            BuildType = BuildType.Local
        });
    }

    private void BindImages()
    {
        // UI 이미지들을 순서대로 연결함
        _spriteControllers.Clear();

        Register(FindChildImage("BattlePopupPanel"));
        Register(FindChildImageUnder("BattlePopupPanel", "Outline"));
        Register(FindChildImage("Tile"));
        Register(FindChildImage("ScratcherImage"));
        // ExitButton 우선, 없으면 Image 이름으로 닫기 버튼 찾기임
        Register(FindCloseButtonImage());

        GameObject durabilitySlider = FindChild(gameObject, "DurabilitySlider", true);
        Register(FindChild<Image>(durabilitySlider, "Background", false));
        Register(FindChild<Image>(durabilitySlider, "Outline", false));
        Register(FindSliderFillImage(durabilitySlider));

        GameObject interestSlider = FindChild(gameObject, "InterestSlider", true);
        Register(FindChild<Image>(interestSlider, "Background", false));
        Register(FindSliderFillImage(interestSlider));
    }

    private void SetSprites()
    {
        // 연결된 이미지 순서대로 스프라이트 지정함
        int index = 0;

        ChangeSprite(index++, ShapeRectangle);
        ChangeSprite(index++, ShapeRectangleOutline);
        ChangeSprite(index++, TileCats);
        ChangeSprite(index++, BossDay);
        ChangeSprite(index++, CloseButton);
        ChangeSprite(index++, ShapeRectangle);
        ChangeSprite(index++, ShapeRectangleOutline);
        ChangeSprite(index++, ShapeRectangle);
        ChangeSprite(index++, ShapeRectangle);
        ChangeSprite(index++, ShapeRectangle);
    }

    private void Register(Image image)
    {
        // 비어있는 이미지는 제외임
        if (image == null)
            return;

        _spriteControllers.Add(new UISpriteController(image));
    }

    private void ChangeSprite(int index, string key)
    {
        // 범위를 벗어나면 무시함
        if (index < 0 || index >= _spriteControllers.Count)
            return;

        _spriteControllers[index].ChangeSprite(key);
    }

    private Image FindChildImage(string objectName)
    {
        // 현재 UI 아래 이미지 찾기임
        return FindChild<Image>(gameObject, objectName, true);
    }

    private Image FindCloseButtonImage()
    {
        // Screen 프리팹은 ExitButton 이름을 사용함
        Image exitButtonImage = FindChild<Image>(gameObject, "ExitButton", true);
        if (exitButtonImage != null)
            return exitButtonImage;

        // standalone 프리팹 호환용 Image 이름 폴백임
        return FindChildImage("Image");
    }

    private Image FindChildImageUnder(string parentName, string childName)
    {
        // 부모 아래 특정 이미지 찾기임
        GameObject parent = FindChild(gameObject, parentName, true);
        if (parent == null)
            return null;

        return FindChild<Image>(parent, childName, false);
    }

    private static Image FindSliderFillImage(GameObject sliderRoot)
    {
        // 슬라이더 Fill 이미지 찾기임
        if (sliderRoot == null)
            return null;

        Transform fillArea = sliderRoot.transform.Find("Fill Area");
        if (fillArea == null)
            return null;

        Transform fill = fillArea.Find("Fill");
        return fill != null ? fill.GetComponent<Image>() : null;
    }

    private void OnDestroy()
    {
        // 로드한 스프라이트 해제임
        foreach (UISpriteController controller in _spriteControllers)
            controller?.ReleaseSprite();
    }
}
