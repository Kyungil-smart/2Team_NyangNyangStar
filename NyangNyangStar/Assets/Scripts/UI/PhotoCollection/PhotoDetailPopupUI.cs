using UI.Base;
using UnityEngine;

public class PhotoDetailPopupUI : UIPopup
{
    private NyangNyangSnapSavedPhotoData _photoData;

    public void SetData(NyangNyangSnapSavedPhotoData photoData)
    {
        _photoData = photoData;
    }
}
