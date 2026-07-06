using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class NyangQuariumFishInfoPopupUI : UIPopup
{
    [Header("닫기 버튼")]
    [Tooltip("배경")][SerializeField] private Button _background;
    [Tooltip("뒤로가기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("닫기 버튼")][SerializeField] private Button _closeButton;

    [Header("화살표 버튼")]
    [Tooltip("이전 물고기 버튼")][SerializeField] private Button _previousButton;
    [Tooltip("다음 물고기 버튼")][SerializeField] private Button _nextButton;

    [Header("물고기 정보")]
    [Tooltip("분류")][SerializeField] private TMP_Text _fishTypeText;
    [Tooltip("레벨")][SerializeField] private TMP_Text _fishLevelText;
    [Tooltip("이름")][SerializeField] private TMP_Text _fishNameText;
    [Tooltip("설명")][SerializeField] private TMP_Text _fishDescriptionText;

    private NyangQuariumFishInfoPopupSprite _sprite;
    private NyangQuariumCollectionPopupUI _collectionPopup;

    private List<NyangQuariumFishData> _fishList = new();
    private int _currentIndex;

    public override void Init()
    {
        Bind<Button>(typeof(NyangQuariumFishInfoPopupButtons));
        Bind<TMP_Text>(typeof(NyangQuariumFishInfoPopupTexts));

        _background = GetButton((int)NyangQuariumFishInfoPopupButtons.Background);
        _backButton = GetButton((int)NyangQuariumFishInfoPopupButtons.BackButton);
        _closeButton = GetButton((int)NyangQuariumFishInfoPopupButtons.CloseButton);
        _previousButton = GetButton((int)NyangQuariumFishInfoPopupButtons.PreviousButton);
        _nextButton = GetButton((int)NyangQuariumFishInfoPopupButtons.NextButton);

        _fishTypeText = GetText((int)NyangQuariumFishInfoPopupTexts.FishTypeText);
        _fishLevelText = GetText((int)NyangQuariumFishInfoPopupTexts.FishLevelText);
        _fishNameText = GetText((int)NyangQuariumFishInfoPopupTexts.FishNameText);
        _fishDescriptionText = GetText((int)NyangQuariumFishInfoPopupTexts.FishDescriptionText);

        BindButtons();

        _sprite = GetComponent<NyangQuariumFishInfoPopupSprite>();
        _sprite.Init();
    }

    public void SetCollectionPopup(NyangQuariumCollectionPopupUI collectionPopup)
    {
        _collectionPopup = collectionPopup;
    }

    public void SetFishList(List<NyangQuariumFishData> fishList, int targetIndex)
    {
        _fishList = fishList;
        _currentIndex = Mathf.Clamp(targetIndex, 0, _fishList.Count - 1);

        RefreshCurrentFish();
    }

    private void RefreshCurrentFish()
    {
        NyangQuariumFishData fishData = _fishList[_currentIndex];
        bool isUnlocked = _collectionPopup.IsFishUnlocked(fishData.FishId);

        _sprite.SetFishImage(fishData.FishKey, isUnlocked);

        _fishTypeText.text = $"[ {GetFishTypeText(fishData.FishType)} ]";
        _fishLevelText.text = isUnlocked ? $"[ Lv {fishData.Level} ]" : "[ Lv ??? ]";
        _fishNameText.text = isUnlocked ? $"이름 : {fishData.FishName}" : "이름 : ???";
        _fishDescriptionText.text = isUnlocked ? $"설명 : {fishData.FishDescription}" : "설명 : ???";
    }

    private void ShowPreviousFish()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_currentIndex <= 0) return;

        _currentIndex--;

        RefreshCurrentFish();
    }

    private void ShowNextFish()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        if (_currentIndex >= _fishList.Count - 1) return;

        _currentIndex++;

        RefreshCurrentFish();
    }

    private string GetFishTypeText(FishType fishType)
    {
        switch (fishType)
        {
            case FishType.Freshwater:
                return "담수";
            case FishType.Saltwater:
                return "해수";
            case FishType.BrackishWater:
                return "기수";
            case FishType.Environments:
                return "자연 요소";
            default:
                return string.Empty;
        }
    }

    private void BindButtons()
    {
        if (_background != null) _background.onClick.AddListener(CloseNyangQuariumFishInfoPopup);
        if (_backButton != null) _backButton.onClick.AddListener(CloseNyangQuariumFishInfoPopup);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseAllPopup);
        if (_previousButton != null) _previousButton.onClick.AddListener(ShowPreviousFish);
        if (_nextButton != null) _nextButton.onClick.AddListener(ShowNextFish);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_background);
        RemovePopupButton(_backButton);
        RemovePopupButton(_closeButton);
        RemovePopupButton(_previousButton);
        RemovePopupButton(_nextButton);
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
    }

    private void CloseNyangQuariumFishInfoPopup()
    {
        gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }

    private void CloseAllPopup()
    {
        gameObject.SetActive(false);

        if (_collectionPopup != null)
            _collectionPopup.gameObject.SetActive(false);

        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }
}

public enum NyangQuariumFishInfoPopupButtons
{
    Background,
    BackButton,
    CloseButton,
    PreviousButton,
    NextButton,
}

public enum NyangQuariumFishInfoPopupTexts
{
    FishTypeText,
    FishLevelText,
    FishNameText,
    FishDescriptionText
}
