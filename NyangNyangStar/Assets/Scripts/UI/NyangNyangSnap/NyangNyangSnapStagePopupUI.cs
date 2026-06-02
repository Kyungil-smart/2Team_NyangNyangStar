using Core.Managers;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class NyangNyangSnapStagePopupUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("스테이지 1")][SerializeField] private Button _stage1Button;
    [Tooltip("스테이지 2")][SerializeField] private Button _stage2Button;
    [Tooltip("닫기")][SerializeField] private Button _closeButton;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapStageButtons));

        _stage1Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage1Button);
        _stage2Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage2Button);
        _closeButton = Get<Button>((int)NyangNyangSnapStageButtons.CloseButton);

        // InitPopup(KeyContainer.Prefabs., _stage1Button);
        // InitPopup(KeyContainer.Prefabs., _stage2Button);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseNyangNyangSnapStagePopup);
    }

    private void OnDestroy()
    {
        RemovePopupButton(_stage1Button);
        RemovePopupButton(_stage2Button);
        RemovePopupButton(_closeButton);
    }

    private void InitPopup(string key, Button button)
    {
        GameManager.UI.ShowPopupUI<UIPopup>(key, onLoaded => AddPopupButton(button, onLoaded), false);
    }

    private void AddPopupButton(Button button, UIPopup popup)
    {
        if (button == null) return;
        button.onClick.AddListener(() =>
        {
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
        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapStageButtons
{
    Stage1Button,
    Stage2Button,
    CloseButton
}
