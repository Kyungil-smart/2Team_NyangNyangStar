using Core.Managers;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class CatPhotoSlotUI : UIBase
{
    [SerializeField] private Button _catImage;

    private CatPhotoSlotSprite _sprite;
    private NyangNyangSnapRuntimePhotoData _photoData;
    private PhotoDetailPopupUI _detailPopup;

    public int StarCount => _photoData.StarCount;

    public override void Init()
    {
        Bind<Button>(typeof(CatPhotoSlotButtons));

        _catImage = GetButton((int)CatPhotoSlotButtons.CatImage);
        _catImage.onClick.AddListener(OpenDetailPopup);

        _sprite = GetComponent<CatPhotoSlotSprite>();
        _sprite.Init();
    }

    public void SetDetailPopup(PhotoDetailPopupUI detailPopup)
    {
        _detailPopup = detailPopup;
    }

    public void SetData(NyangNyangSnapRuntimePhotoData photoData)
    {
        _photoData = photoData;
        Debug.Log($"[CatPhotoSlotUI] SetData 호출 / id:{photoData.PhotoId} / star:{photoData.StarCount}");
        _sprite.SetPhoto(photoData.Sprite);
        _sprite.SetStar(photoData.StarCount);
    }

    private void OpenDetailPopup()
    {
        if (_detailPopup == null) return;

        _detailPopup.SetData(_photoData);
        _detailPopup.gameObject.SetActive(true);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void OnDestroy()
    {
        if (_catImage != null)
            _catImage.onClick.RemoveAllListeners();
    }
}

public enum CatPhotoSlotButtons
{
    CatImage
}
