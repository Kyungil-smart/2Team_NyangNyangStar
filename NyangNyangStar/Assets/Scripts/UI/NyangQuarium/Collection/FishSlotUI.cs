using Core.Managers;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class FishSlotUI : UIBase
{
    [SerializeField] private Button _fishSlotBackground;
    [SerializeField] private GameObject _lockedOverlay;
    [SerializeField] private TMP_Text _fishNameText;

    private FishSlotSprite _sprite;
    private NyangQuariumFishData _fishData;
    private NyangQuariumCollectionPopupUI _collectionPopup;

    public FishType FishType => _fishData.FishType;
    public int FishId => _fishData.FishId;

    public override void Init()
    {
        Bind<Button>(typeof(FishSlotButtons));
        Bind<GameObject>(typeof(FishSlotObjects));
        Bind<TMP_Text>(typeof(FishSlotTexts));

        _fishSlotBackground = GetButton((int)FishSlotButtons.FishSlotBackground);
        _fishSlotBackground.onClick.AddListener(OpenInfoPopup);

        _lockedOverlay = GetObject((int)FishSlotObjects.LockedOverlay);

        _fishNameText = GetText((int)FishSlotTexts.FishNameText);

        _sprite = GetComponent<FishSlotSprite>();
        _sprite.Init();
    }

    public void SetCollectionPopup(NyangQuariumCollectionPopupUI collectionPopup)
    {
        _collectionPopup = collectionPopup;
    }

    public void SetData(NyangQuariumFishData fishData, bool isUnlocked)
    {
        _fishData = fishData;

        _sprite.SetFishImage(fishData.FishKey);
        RefreshUnlockState(isUnlocked);
    }

    public void RefreshUnlockState(bool isUnlocked)
    {
        _sprite.SetFishImageColor(isUnlocked ? Color.white : Color.black);
        _lockedOverlay.SetActive(!isUnlocked);

        _fishNameText.text = isUnlocked
            ? $"Lv.{_fishData.Level}\n{_fishData.FishName}"
            : $"Lv.{_fishData.Level}\n???";
    }

    private void OpenInfoPopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        _collectionPopup.OnClickFishSlot(_fishData);
    }

    private void OnDestroy()
    {
        if (_fishSlotBackground != null)
            _fishSlotBackground.onClick.RemoveAllListeners();
    }
}

public enum FishSlotButtons
{
    FishSlotBackground
}

public enum FishSlotObjects
{
    LockedOverlay
}

public enum FishSlotTexts
{
    FishNameText
}