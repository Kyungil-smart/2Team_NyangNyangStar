using UI;
using UnityEngine;
using UnityEngine.UI;

public class MainUISprite : UIBase
{
    [SerializeField] private Color _workshopMergeBoardColor;
    [SerializeField] private Color _mainMergeBoardColor;
    private UISpriteController[] _spriteController;

    public override void Init()
    {
        Bind<Image>(typeof(MainUIImages));

        _spriteController = new UISpriteController[(int)MainUIImages.Count];

        for (int i = 0; i < (int)MainUIImages.Count; i++)
        {
            _spriteController[i] = new UISpriteController(GetImage(i));
        }

        SetSprites();
    }

    private void SetSprites()
    {
        SetSprite(MainUIImages.ShopButton, "Main_Btn_Shop");
        SetSprite(MainUIImages.EventButton, "Main_Btn_Event_Scratching");
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
}

public enum MainUIImages
{
    ShopButton,                // 상점
    EventButton,               // 시즌 이벤트
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

    Count
}