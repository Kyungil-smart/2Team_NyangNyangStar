using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangStargramPostSlotUI : UIBase
{
    [SerializeField] private Button _button;

    private NyangStargramPostSlotSprite _sprite;

    private NyangNyangSnapSavedPhotoData _photoData;
    private Action<NyangNyangSnapSavedPhotoData> _onClick;

    public override void Init()
    {
        Bind<Button>(typeof(NyangStargramPostSlotButtons));

        _button = GetButton((int)NyangStargramPostSlotButtons.PhotoImage);

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClickSlot);

        _sprite = GetComponent<NyangStargramPostSlotSprite>();
        _sprite.Init();
    }

    public void SetData(NyangNyangSnapSavedPhotoData photoData, Action<NyangNyangSnapSavedPhotoData> onClick)
    {
        _photoData = photoData;
        _onClick = onClick;

        _sprite.SetPhoto(photoData.storagePath);
    }

    private void OnClickSlot()
    {
        _onClick?.Invoke(_photoData);
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