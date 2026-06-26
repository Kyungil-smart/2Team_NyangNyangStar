using Core.Managers;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class FishSlotUI : UIBase
{
    [SerializeField] private Button _fishSlot;
    [SerializeField] private GameObject _lockedOverlay;

    private FishSlotSprite _sprite;
    private NyangQuariumFishData _fishData;
    private NyangQuariumFishInfoPopupUI _infoPopup;

    public FishType FishType => _fishData.FishType;

    public override void Init()
    {
        Bind<Button>(typeof(FishSlotButtons));
        Bind<GameObject>(typeof(FishSlotObjects));

        _fishSlot = GetButton((int)FishSlotButtons.FishSlot);
        _fishSlot.onClick.AddListener(OpenInfoPopup);

        _lockedOverlay = GetObject((int)FishSlotObjects.LockedOverlay);

        _sprite = GetComponent<FishSlotSprite>();
        _sprite.Init();
    }

    public void SetInfoPopup(NyangQuariumFishInfoPopupUI infoPopup)
    {
        _infoPopup = infoPopup;
    }

    public void SetData(NyangQuariumFishData fishData)
    {
        _fishData = fishData;
        _sprite.SetFishImage(fishData.FishKey);
        // TODO : 해금여부에 따라서 이미지 컬러 White/Black, LockedOverlay on/off
    }

    private void OpenInfoPopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        // TODO : 미해금이면 LockedPopup열림
        //NyangQuariumFishInfoPopup열기
        _infoPopup.SetData(_fishData);
        _infoPopup.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (_fishSlot != null)
            _fishSlot.onClick.RemoveAllListeners();
    }
}

public enum FishSlotButtons
{
    FishSlot
}

public enum FishSlotObjects
{
    LockedOverlay
}