using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostSlotUI : UIBase
{
    [SerializeField] private Button _button;

    private NyangStargramPostSlotSprite _sprite;

    private Sprite _photoSprite;
    private Action<Sprite> _onClick;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramPostSlotButtons));

        _button = GetButton((int)NyangStargramPostSlotButtons.PhotoImage);

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClickSlot);

        _sprite = GetComponent<NyangStargramPostSlotSprite>();
        _sprite.Init();
    }

    public void SetData(Sprite sprite, Action<Sprite> onClick)
    {
        _photoSprite = sprite;
        _onClick = onClick;

        _sprite.SetPhoto(sprite);
    }

    private void OnClickSlot()
    {
        _onClick?.Invoke(_photoSprite);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }
}

public enum NyangStargramPostSlotButtons
{
    PhotoImage
}