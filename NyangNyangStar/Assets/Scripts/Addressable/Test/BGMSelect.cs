using Core.Managers;
using Services.AddressableKey;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class BGMSelect : UIBase
{
    [SerializeField] private Button titleButton;
    [SerializeField] private Button bgm01Button;
    [SerializeField] private Button bgm02Button;
    [SerializeField] private Button bgm03Button;
    
    [SerializeField] private BackgroundSelect _backgroundSelect;

    private void Awake()
        => Init();

    private void OnEnable()
    {
        titleButton.onClick.AddListener(GotoTitle);
        bgm01Button.onClick.AddListener(GotoHome);
        bgm02Button.onClick.AddListener(GotoCatCafe);
        bgm03Button.onClick.AddListener(GotoSchool);
        }

    private void OnDisable()
    {
        titleButton.onClick.RemoveAllListeners();
        bgm01Button.onClick.RemoveAllListeners();
        bgm02Button.onClick.RemoveAllListeners();
        bgm03Button.onClick.RemoveAllListeners();
    }

    private void GotoTitle()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.TitleBGM);
        _backgroundSelect.ChangeSprite(KeyContainer.Sprite.Title);
    }

    private void GotoHome()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM01);
        _backgroundSelect.ChangeSprite(KeyContainer.Sprite.Home);
    }

    private void GotoCatCafe()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM02);
        _backgroundSelect.ChangeSprite(KeyContainer.Sprite.CatCafe);
    }

    private void GotoSchool()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM03);
        _backgroundSelect.ChangeSprite(KeyContainer.Sprite.School);
    }

    public override void Init()
    {
        Bind<Button>(typeof(BGMSelectButtons));

        titleButton = Get<Button>((int)BGMSelectButtons.TitleButton);
        bgm01Button = Get<Button>((int)BGMSelectButtons.BGM01Button);
        bgm02Button = Get<Button>((int)BGMSelectButtons.BGM02Button);
        bgm03Button = Get<Button>((int)BGMSelectButtons.BGM03Button);
        
        _backgroundSelect = FindObjectOfType<BackgroundSelect>();
    }

    public enum BGMSelectButtons
    {
        TitleButton,
        BGM01Button,
        BGM02Button,
        BGM03Button
    }
}