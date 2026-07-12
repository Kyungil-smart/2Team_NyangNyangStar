using Core.Managers;
using UI;
using UI.Base;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Util;

public class NyangNyangSnapStagePopupUI : UIPopup
{
    [Header("버튼")]
    [Tooltip("스테이지 1")][SerializeField] private Button _stage1Button;
    [Tooltip("스테이지 2")][SerializeField] private Button _stage2Button;
    [Tooltip("뒤로가기")][SerializeField] private Button _backButton;

    [Header("플레이 방법 안내")]
    [Tooltip("플레이 방법 버튼")]
    [SerializeField] private Button _howToPlayButton;

    [Tooltip("플레이 방법 전체 패널")]
    [SerializeField] private GameObject _guidePanel;

    [Tooltip("플레이 방법 페이지 오브젝트")]
    [SerializeField] private GameObject[] _guidePages;

    [Tooltip("이전 페이지 버튼")]
    [SerializeField] private Button _previousButton;

    [Tooltip("다음 페이지 버튼")]
    [SerializeField] private Button _nextButton;

    [Tooltip("플레이 방법 닫기 버튼")]
    [SerializeField] private Button _guideCloseButton;

    [Tooltip("현재 페이지 표시 Text")]
    [SerializeField] private TMP_Text _pageText;

    private int _currentGuidePage;
    private NyangNyangSnapStagePopupSprite _sprite;

    public override void Init()
    {
        Bind<Button>(typeof(NyangNyangSnapStageButtons));

        _stage1Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage1Button);
        _stage2Button = Get<Button>((int)NyangNyangSnapStageButtons.Stage2Button);
        _backButton = Get<Button>((int)NyangNyangSnapStageButtons.BackButton);

        InitNyangNyangSnap();
        BindGuideButtons();

        if (_backButton != null)
            _backButton.onClick.AddListener(CloseNyangNyangSnapStagePopup);

        CloseGuideImmediately();

        _sprite = GetComponent<NyangNyangSnapStagePopupSprite>();
        _sprite.Init();
    }

    private void OnDestroy()
    {
        RemovePopupButton(_stage1Button);
        RemovePopupButton(_stage2Button);
        RemovePopupButton(_backButton);
        RemovePopupButton(_howToPlayButton);
        RemovePopupButton(_previousButton);
        RemovePopupButton(_nextButton);
        RemovePopupButton(_guideCloseButton);
    }

    private void BindGuideButtons()
    {
        if (_howToPlayButton != null)
        {
            _howToPlayButton.onClick.RemoveListener(OpenGuide);
            _howToPlayButton.onClick.AddListener(OpenGuide);
        }

        if (_previousButton != null)
        {
            _previousButton.onClick.RemoveListener(ShowPreviousGuidePage);
            _previousButton.onClick.AddListener(ShowPreviousGuidePage);
        }

        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveListener(ShowNextGuidePage);
            _nextButton.onClick.AddListener(ShowNextGuidePage);
        }

        if (_guideCloseButton != null)
        {
            _guideCloseButton.onClick.RemoveListener(CloseGuide);
            _guideCloseButton.onClick.AddListener(CloseGuide);
        }
    }

    private void OpenGuide()
    {
        if (_guidePanel == null)
        {
            DebugTool.Warning(
                "[NyangNyangSnapStagePopupUI] GuidePanel이 연결되지 않았습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        _currentGuidePage = 0;
        _guidePanel.SetActive(true);
        _guidePanel.transform.SetAsLastSibling();

        RefreshGuidePage();
    }

    private void ShowPreviousGuidePage()
    {
        if (_currentGuidePage <= 0)
            return;

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        _currentGuidePage--;
        RefreshGuidePage();
    }

    private void ShowNextGuidePage()
    {
        if (_guidePages == null ||
            _guidePages.Length == 0 ||
            _currentGuidePage >= _guidePages.Length - 1)
        {
            return;
        }

        GameManager.Audio.PlaySfx("Main_SFX_Touch");

        _currentGuidePage++;
        RefreshGuidePage();
    }

    private void RefreshGuidePage()
    {
        if (_guidePages == null || _guidePages.Length == 0)
        {
            DebugTool.Warning(
                "[NyangNyangSnapStagePopupUI] GuidePages가 연결되지 않았습니다.",
                DebugType.UI,
                this
            );
            return;
        }

        _currentGuidePage = Mathf.Clamp(
            _currentGuidePage,
            0,
            _guidePages.Length - 1
        );

        for (int i = 0; i < _guidePages.Length; i++)
        {
            if (_guidePages[i] != null)
                _guidePages[i].SetActive(i == _currentGuidePage);
        }

        if (_pageText != null)
        {
            _pageText.text =
                $"{_currentGuidePage + 1} / {_guidePages.Length}";
        }

        if (_previousButton != null)
            _previousButton.interactable = _currentGuidePage > 0;

        if (_nextButton != null)
        {
            _nextButton.interactable =
                _currentGuidePage < _guidePages.Length - 1;
        }
    }

    private void CloseGuide()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
        CloseGuideImmediately();
    }

    private void CloseGuideImmediately()
    {
        if (_guidePanel != null)
            _guidePanel.SetActive(false);
    }

    private void InitNyangNyangSnap()
    {
        GameManager.UI.ShowPopupUI<NyangNyangSnapUI>(KeyContainer.Prefabs.NyangNyangSnapPopupUI,
            onLoaded =>
            {
                onLoaded.SetStagePopup(this);
                AddNyangNyangSnapPopupButton(_stage1Button, onLoaded, 1);
                AddNyangNyangSnapPopupButton(_stage2Button, onLoaded, 2);
            }, false);
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
        CloseGuideImmediately();
        gameObject.SetActive(false);
    }
}

public enum NyangNyangSnapStageButtons
{
    Stage1Button,
    Stage2Button,
    BackButton
}
