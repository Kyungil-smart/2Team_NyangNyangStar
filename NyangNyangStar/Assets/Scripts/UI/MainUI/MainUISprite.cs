using System;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class MainUISprite : UIBase
{
    private const string NyangquariumTankSpritePrefix = "NQ_Object_Tank_";

    // 뭉치 버튼 일러 (지친 뭉치 / 기본 뭉치 - 냥쿠아리움 초입 퀘스트 완주 후)
    public const string MoongchiTiredSpriteKey = "Main_Img_TiredMoongchi";
    public const string MoongchiClearedSpriteKey = "NQ_Char_Moongchi";

    [SerializeField] private Color _workshopMergeBoardColor;
    [SerializeField] private Color _mainMergeBoardColor;
    private UISpriteController[] _spriteController;
    private Image _notebookAlert;
    private Image _moongchiAlert;
    private bool _isMoongchiCleared;

    public override void Init()
    {
        Bind<Image>(typeof(MainUIImages));

        _spriteController = new UISpriteController[Enum.GetValues(typeof(MainUIImages)).Length];

        for (int i = 0; i < Enum.GetValues(typeof(MainUIImages)).Length; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        _notebookAlert = GetImage((int)MainUIImages.NotebookAlert);
        _moongchiAlert = GetImage((int)MainUIImages.MoongchiAlert);

        RegisterMoongchiSpriteKeys();
        SetSprites();
    }

    // 뭉치 일러 키가 KeyContainer에 없으면 등록 (LoadSprite의 키 검증 통과용)
    private static void RegisterMoongchiSpriteKeys()
    {
        if (!KeyContainer.Sprites.Contains(MoongchiTiredSpriteKey))
            KeyContainer.Sprites.Add(MoongchiTiredSpriteKey);

        if (!KeyContainer.Sprites.Contains(MoongchiClearedSpriteKey))
            KeyContainer.Sprites.Add(MoongchiClearedSpriteKey);
    }

    private void SetSprites()
    {
        SetSprite(MainUIImages.Background, "Main_BG_NoMoongchi"); // 뭉치 있는 배경 버전은 "Main_BG"
        SetSprite(MainUIImages.ShopButton, "Main_Btn_Shop");
        SetSprite(MainUIImages.ScratchingTimeButton, "Main_Btn_Event_Scratching");
        SetSprite(MainUIImages.DailyCheckInButton, "Main_Btn_Attendance");
        SetSprite(MainUIImages.MailButton, "Main_Btn_Mail");
        SetSprite(MainUIImages.SettingsButton, "Main_Btn_Settings");
        SetSprite(MainUIImages.CollectionButton, "Main_Btn_Collection");
        SetSprite(MainUIImages.StoryBookButton, "Main_Btn_Storybook");
        SetSprite(MainUIImages.NotebookButton, "Main_Btn_FosterDiary");
        SetSprite(MainUIImages.RouletteButton, "Main_Btn_Roulette");
        SetSprite(MainUIImages.AffinityButton, "Main_Btn_Interact");
        SetSprite(MainUIImages.NyangNyangSnapButton, "Btn_Camera");
        SetSprite(MainUIImages.MeowMeowStarButton, "Btn_Nyangstagram");
        SetSprite(MainUIImages.WorkshopMergeBoardButton, "Main_Btn_Mergeboard", _workshopMergeBoardColor);
        SetSprite(MainUIImages.MainMergeBoardButton, "Main_Btn_Mergeboard", _mainMergeBoardColor);
        SetSprite(MainUIImages.CoinImage, "Main_Icon_Coin");
        SetSprite(MainUIImages.EnergyImage, "Main_Icon_Energy");
        SetSprite(MainUIImages.GemImage, "Main_Icon_Jewel");
        SetSprite(MainUIImages.LevelIcon, "Main_Panel_Level");
        SetSprite(MainUIImages.Energy, "Shape_Rectangle");
        SetSprite(MainUIImages.Coin, "Shape_Rectangle");
        SetSprite(MainUIImages.Gem, "Shape_Rectangle");
        SetSprite(MainUIImages.ProfileImage, "Profile_ProfileImages_0");
        SetSprite(MainUIImages.ProfileFrame, "Main_Profile_Frame");
        SetSprite(MainUIImages.FindMoongchiButton, "Main_Btn_Event_FindMoongchi");
        SetSprite(MainUIImages.NotebookAlert, "Main_Alert");
        SetSprite(MainUIImages.NyangquariumButton, "NQ_Object_Tank_01");
        SetSprite(MainUIImages.MoongchiButton, MoongchiTiredSpriteKey);
        SetSprite(MainUIImages.MoongchiAlert, "Main_Alert");
    }

    private void SetSprite(MainUIImages image, string key)
    {
        _spriteController[(int)image].ChangeSprite(key);
    }

    private void SetSprite(MainUIImages image, string key, Color color)
    {
        _spriteController[(int)image].ChangeColor(color);
        _spriteController[(int)image].ChangeSprite(key);
    }

    public void SetNotebookAlert(bool isOn)
    {
        _notebookAlert.gameObject.SetActive(isOn);
    }

    // 뭉치 클릭 대기 구간(첫 진입~1번 퀘스트 수락 전)에만 뭉치 버튼 알림 표시
    public void SetMoongchiAlert(bool isOn)
    {
        if (_moongchiAlert != null)
            _moongchiAlert.gameObject.SetActive(isOn);
    }

    public void SetNyangquariumTankLevel(int level)
    {
        SetSprite(MainUIImages.NyangquariumButton, ResolveNyangquariumTankSpriteKey(level));
    }

    // 냥쿠아리움 초입 퀘스트 완주 여부에 따라 뭉치 버튼 일러 교체
    public void SetMoongchiCleared(bool cleared)
    {
        if (_spriteController == null)
            return;

        if (_isMoongchiCleared == cleared)
            return;

        _isMoongchiCleared = cleared;
        SetSprite(MainUIImages.MoongchiButton, cleared ? MoongchiClearedSpriteKey : MoongchiTiredSpriteKey);
    }

    private static string ResolveNyangquariumTankSpriteKey(int level)
    {
        int tankIndex =
            level >= 20 ? 5 :
            level >= 15 ? 4 :
            level >= 10 ? 3 :
            level >= 5 ? 2 :
            1;

        return $"{NyangquariumTankSpritePrefix}{tankIndex:00}";
    }
}

public enum MainUIImages
{
    Background,                // 배경
    ShopButton,                // 상점
    ScratchingTimeButton,      // 시즌 이벤트
    DailyCheckInButton,        // 출석 체크
    MailButton,                // 우편함
    SettingsButton,            // 설정
    CollectionButton,          // 아이템 도감
    StoryBookButton,           // 스토리북
    NotebookButton,            // 임시보호 수첩
    RouletteButton,            // 룰렛
    AffinityButton,            // 교감
    NyangNyangSnapButton,      // 냥냥스냅
    MeowMeowStarButton,        // 냥스타그램
    WorkshopMergeBoardButton,  // 공방 머지 보드판
    MainMergeBoardButton,      // 기본 머지 보드판
    CoinImage,                 // 코인 아이콘
    EnergyImage,               // 에너지 아이콘
    GemImage,                  // 보석 아이콘
    LevelIcon,                 // 레벨 패널
    Energy,                    // 에너지 패널
    Coin,                      // 코인 패널
    Gem,                       // 보석 패널
    ProfileImage,              // 프로필 이미지
    ProfileFrame,              // 프로필 이미지 테두리
    FindMoongchiButton,        // 뭉치를 찾아라 이벤트
    NotebookAlert,             // 임시보호 수업 알림
    NyangquariumButton,        // 냥쿠아리움
    MoongchiButton,            // 뭉치 (냥쿠아리움 스토리 진행 상태)
    MoongchiAlert,             // 뭉치 버튼 알림 (뭉치 클릭 대기)
}
