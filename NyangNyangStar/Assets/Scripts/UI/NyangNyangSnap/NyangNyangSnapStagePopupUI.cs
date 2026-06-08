using Core.Managers;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapStagePopupUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("스테이지 1")][SerializeField] private Button _stage1Button;
    [Tooltip("스테이지 2")][SerializeField] private Button _stage2Button;
    [Tooltip("뒤로가기")][SerializeField] private Button _backButton;

    private NyangNyangSnapStagePopupSprite _sprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapStageButtons));

        _stage1Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage1Button);
        _stage2Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage2Button);
        _backButton = Get<Button>((int)NyangNyangSnapStageButtons.BackButton);

        InitNyangNyangSnap();

        if (_backButton != null) _backButton.onClick.AddListener(CloseNyangNyangSnapStagePopup);

        _sprite = GetComponent<NyangNyangSnapStagePopupSprite>();
        _sprite.Init();
    }

    private void OnDestroy()
    {
        RemovePopupButton(_stage1Button);
        RemovePopupButton(_stage2Button);
        RemovePopupButton(_backButton);
    }

    private void InitNyangNyangSnap()
    {
        GameManager.UI.ShowPopupUI<NyangNyangSnapUI>(KeyContainer.Prefabs.NyangNyangSnapPopupUI,
            onLoaded =>
            {
                AddNyangNyangSnapPopupButton(_stage1Button, onLoaded, 1);
                AddNyangNyangSnapPopupButton(_stage2Button, onLoaded, 2);
            }, false, false);
    }

    private void AddNyangNyangSnapPopupButton(Button button, NyangNyangSnapUI popup, int stage)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
            GameManager.Audio.PlaySfx("Main_SFX_Touch");
            popup.OpenPopup(stage);
            popup.gameObject.SetActive(true);
            PlayPopupOpenAnimation(popup);
        });
    }

    private void PlayPopupOpenAnimation(UIPopup popup)
    {
        if (popup == null) return;
        popup.PlayOpenAnimation();
    }

    private void RemovePopupButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
    }

    private void CloseNyangNyangSnapStagePopup()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapStageButtons
{
    Stage1Button,
    Stage2Button,
    BackButton
}
