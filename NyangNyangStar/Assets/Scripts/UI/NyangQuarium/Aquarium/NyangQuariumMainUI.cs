using Core.Managers;
using System.Collections.Generic;
using TMPro;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangQuariumMainUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("뒤로가기 버튼")][SerializeField] private Button _backButton;
    [Tooltip("유리 버튼")][SerializeField] private Button _windowButton;
    [Tooltip("물멍 버튼")][SerializeField] private Button _mulmeongButton;
    [Tooltip("도감 버튼")][SerializeField] private Button _fishCollectionButton;
    [Tooltip("보드 버튼")][SerializeField] private Button _mergeBoardButton;
    [Tooltip("냥쿠라이움 버튼")][SerializeField] private Button _quariumLayOutButton;


    //private NyangstagramMainUISprite _nyangstagramMainUISprite;
    //private NyangstagramTab _currentMainTab = NyangstagramTab.Profile;

    public override void Init()
    {
        Bind<Button>(typeof(NyangQuariumMainUIButton));

        _backButton = Get<Button>((int)NyangQuariumMainUIButton.BackButton);
        _windowButton = Get<Button>((int)NyangQuariumMainUIButton.WindowButton);
        _mulmeongButton = Get<Button>((int)NyangQuariumMainUIButton.mulmeongButton);
        _fishCollectionButton = Get<Button>((int)NyangQuariumMainUIButton.FishCollectionButton);
        _mergeBoardButton = Get<Button>((int)NyangQuariumMainUIButton.MergeBoardButton);
        _quariumLayOutButton = Get<Button>((int)NyangQuariumMainUIButton.QuariumLayOutButton);

        AddBackButton();
        InitPopups();
    }

    private void AddBackButton()
    {
        if (_backButton == null)
        {
            DebugTool.Warning(
                "[NyangQuariumMainUI] BackButton을 찾을 수 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        _backButton.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            gameObject.SetActive(false);
        });
    }

    private void InitPopups()
    {
        // 자기 자신(NyangStargramHomeProfile)은 여기서 다시 로드하면 안 된다.
        // 이 스크립트가 붙은 메인 UI는 이미 열려있는 1개로 취급한다.
        InitPopup(KeyContainer.Prefabs.NyangQuariumMulMeongUI, _mulmeongButton);
        InitPopup(KeyContainer.Prefabs.NyangQuariumFreshLayoutUI, _quariumLayOutButton);

        //머지보드
        //InitPopup(KeyContainer.Prefabs.NyangQuariumLayoutUI, _quariumLayOutButton);

        //도감
        //InitPopup(KeyContainer.Prefabs.NyangQuariumCollectionPopup, _quariumLayOutButton);
    }
    private void InitPopup(string key, Button button)
    {
        if (button == null)
        {
            DebugTool.Warning(
                $"[NyangQuariumMainUI] {key}와 연결할 버튼이 없습니다.",
                DebugType.UI,
                this);

            return;
        }

        GameManager.UI.ShowPopupUI<UIPopup>(
            key,
            popup => AddPopupButton(button, popup),
            false);
    }


    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null || popup == null)
        {
            DebugTool.Warning(
                "[NyangQuariumMainUI] 버튼 또는 팝업 연결에 실패했습니다.",
                DebugType.UI,
                this);

            return;
        }

        button.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");

            popup.gameObject.SetActive(true);
            popup.PlayOpenAnimation();
        });
    }

}

public enum NyangQuariumMainUIButton
{
    BackButton,
    WindowButton,
    mulmeongButton,
    FishCollectionButton,
    MergeBoardButton,
    QuariumLayOutButton
}
