using Core.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;
using Util;

public class BGMSelect : UIBase
{
    [SerializeField] private List<Button> _buttons = new(); 
    
    [SerializeField] private BackgroundSelect _backgroundSelect;

    private void Awake()
        => Init();

    private void OnEnable()
    {
        _buttons[(int)BGMSelectButtons.TitleButton].onClick.AddListener(GotoTitle);
        _buttons[(int)BGMSelectButtons.BGM01Button].onClick.AddListener(GotoHome);
        _buttons[(int)BGMSelectButtons.BGM02Button].onClick.AddListener(GotoCatCafe);
        _buttons[(int)BGMSelectButtons.BGM03Button].onClick.AddListener(GotoSchool);
    }

    private void OnDisable()
    {
        foreach (var button in _buttons)
            button.onClick.RemoveAllListeners();
    }

    private void GotoTitle()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.TitleBGM);
        _backgroundSelect.BackgroundController.ChangeSprite(KeyContainer.Sprite.Title);
    }

    private void GotoHome()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM01);
        _backgroundSelect.BackgroundController.ChangeSprite(KeyContainer.Sprite.Home);
    }

    private void GotoCatCafe()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM02);
        _backgroundSelect.BackgroundController.ChangeSprite(KeyContainer.Sprite.CatCafe);
    }

    private void GotoSchool()
    {
        GameManager.Audio.PlayBgm(KeyContainer.Audio.BGM03);
        _backgroundSelect.BackgroundController.ChangeSprite(KeyContainer.Sprite.School);
    }

    public override void Init()
    {
        Bind<Button>(typeof(BGMSelectButtons));
        
        _buttons.Clear();
        
        int buttonCount = Enum.GetValues(typeof(BGMSelectButtons)).Length;

        for (int i = 0; i < buttonCount; i++)
            _buttons.Add(Get<Button>(i));
        
        _backgroundSelect = FindObjectOfType<BackgroundSelect>();
    }

    public enum BGMSelectButtons
    {
        TitleButton,
        BGM01Button,
        BGM02Button,
        BGM03Button,
    }
}