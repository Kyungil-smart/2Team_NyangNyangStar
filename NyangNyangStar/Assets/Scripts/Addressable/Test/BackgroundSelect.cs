using Services.AddressableKey;
using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundSelect : UIBase
{
    [SerializeField] private Image _backgroundImage;

    private UISpriteController _backgroundController;
    public UISpriteController BackgroundController => _backgroundController;

    private void Awake()
        => Init();

    private void Start()
    {
        _backgroundController.ChangeSprite(KeyContainer.Sprite.Title);
    }

    private void OnDestroy()
    {
        _backgroundController?.Dispose();
    }
    public override void Init()
    {
        if(_backgroundImage == null)
            _backgroundImage = GetComponentInChildren<Image>();
        
        _backgroundController = new(_backgroundImage);
    }
}