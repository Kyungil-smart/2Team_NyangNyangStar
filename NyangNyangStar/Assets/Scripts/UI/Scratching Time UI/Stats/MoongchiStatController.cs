using System;
using Data.ScriptableObjects.ScratchingTimeSO;
using TMPro;
using UI;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

// 뭉치 스탯 정보를 UI에 출력하고 경험치 증가 요청을 전달
public class MoongchiStatController : UIBase
{
    [Header("Data")][SerializeField] private MoongchiStatSo _moongchiStat;
    [SerializeField] private MoongchiProgressSO _moongchiProgress;

    [Header("Stat Text")][SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _sharpnessText;
    [SerializeField] private TMP_Text _criticalChanceText;

    [Header("Exp Text")][SerializeField] private TMP_Text _expCountText; // ExpPanel Slider 통합 텍스트 연결용임
    [SerializeField] private TMP_Text _currentExpText;
    [SerializeField] private TMP_Text _expSlashText;
    [SerializeField] private TMP_Text _maxExpText;

    [Header("Exp Gauge")][SerializeField] private Slider _expSlider; // ExpPanel Slider value 연동용임

    [Tooltip("뭉치 경험치 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField]
    private Image _expAmountImage;

    private int _moongchiProgressLoadVersion;
    private bool _isProgressReady;

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        if (_moongchiStat == null)
            return;

        _moongchiStat.OnLevelChanged += PrintMoongchiStat;
        _moongchiStat.OnExpChanged += PrintMoongchiStat;
    }

    private void OnDisable()
    {
        if (_moongchiStat == null)
            return;

        _moongchiStat.OnLevelChanged -= PrintMoongchiStat;
        _moongchiStat.OnExpChanged -= PrintMoongchiStat;
    }

    private void Start()
    {
        if (_moongchiStat == null)
            return;

        ReloadMoongchiProgressForSession();
    }

    public void ReloadMoongchiProgressForSession()
    {
        if (_moongchiStat == null)
            return;

        _isProgressReady = false;
        _moongchiStat.Init();
        PrintMoongchiStat();
        LoadMoongchiProgressFromServer(++_moongchiProgressLoadVersion);
    }

    public void IncreaseExp(int exp)
    {
        if (_moongchiStat == null)
            return;

        if (!_isProgressReady)
        {
            DebugTool.Warning("뭉치 진행도 로드 전에는 경험치를 지급할 수 없습니다.", DebugType.ScratchingTime);
            return;
        }

        _moongchiStat.IncreaseExp(exp);
        SaveMoongchiProgressToServer();
        PrintMoongchiStat();
        DebugTool.Log($"경험치 증가 : {exp}", DebugType.ScratchingTime);
    }


    private async void LoadMoongchiProgressFromServer(int loadVersion)
    {
        if (_moongchiStat == null)
            return;

        if (!ResolveMoongchiProgress())
            return;

        try
        {
            await _moongchiProgress.UpdateFromServerAsync(false);

            if (loadVersion != _moongchiProgressLoadVersion)
                return;

            _moongchiProgress.ApplyTo(_moongchiStat);
            _isProgressReady = true;
            PrintMoongchiStat();
        }
        catch (Exception e)
        {
            DebugTool.Warning($"MoongchiProgressSO 불러오기 실패: {e.Message}", DebugType.ScratchingTime);
        }
    }

    private async void SaveMoongchiProgressToServer()
    {
        if (_moongchiStat == null || !_isProgressReady)
            return;

        if (!ResolveMoongchiProgress())
            return;

        _moongchiProgress.CaptureFrom(_moongchiStat);

        try
        {
            await _moongchiProgress.SetDataAsync(_moongchiProgress.ToFirestoreDictionary());
        }
        catch (Exception e)
        {
            DebugTool.Warning($"MoongchiProgressSO 저장 실패: {e.Message}", DebugType.ScratchingTime);
        }
    }


    private bool ResolveMoongchiProgress()
    {
        if (FireStoreManager.Instance == null)
        {
            DebugTool.Warning("FireStoreManager.Instance가 없어 뭉치 진행도를 불러올 수 없습니다.", DebugType.ScratchingTime, this);
            return false;
        }

        if (!FireStoreManager.Instance.IsInitialized)
        {
            DebugTool.Warning("Firestore 초기화 완료 전에는 뭉치 진행도를 불러올 수 없습니다.", DebugType.ScratchingTime, this);
            return false;
        }

        MoongchiProgressSO resolvedProgress =
            FireStoreManager.Instance.GetData<MoongchiProgressSO>(DataType.MoongchiProgess);

        if (resolvedProgress == null)
        {
            DebugTool.Warning("FireStoreManager에서 MoongchiProgressSO를 찾을 수 없습니다.", DebugType.ScratchingTime, this);
            return false;
        }

        if (!ReferenceEquals(_moongchiProgress, resolvedProgress))
        {
            _moongchiProgress = resolvedProgress;
            DebugTool.Log("MoongchiProgressSO 연결 완료", DebugType.ScratchingTime, this);
        }

        return true;
    }

    public int MoongchiAttack()
        => MoongchiAttack(out _);

    public int MoongchiAttack(out bool isCritical)
    {
        isCritical = false;

        if (_moongchiStat == null)
            return 0;

        return _moongchiStat.Attack(out isCritical);
    }

    public void PrintMoongchiStat()
    {
        if (_moongchiStat == null)
            return;

        int currentExp = Mathf.Min(_moongchiStat.GetCurrentExp(), _moongchiStat.MaxExp);
        int maxExp = _moongchiStat.MaxExp;
        float fillRatio = _moongchiStat.GetExpFillRatio();

        // MoongchiStat SO 값을 LvPanel, InfoPanel 텍스트에 반영함
        SetText(_levelText, $"Lv. {_moongchiStat.Level} 뭉치");
        SetText(_sharpnessText, $"발톱 예리도 {_moongchiStat.GetSharpness()}");
        SetText(_criticalChanceText, $"힘껏 긁기 확률 {_moongchiStat.GetCriticalChance()}%");

        // ExpPanel 통합 텍스트 우선, 없으면 분리 텍스트 폴백임
        if (_expCountText != null)
            SetText(_expCountText, $"{currentExp} / {maxExp}");
        else
        {
            SetText(_currentExpText, currentExp.ToString());
            SetText(_expSlashText, "/");
            SetText(_maxExpText, maxExp.ToString());
        }

        SetExpAmount(fillRatio);
    }

    public void SetExpAmount(float fillRatio)
    {
        float ratio = Mathf.Clamp01(fillRatio);

        // Slider Fill은 anchorMax.x로 너비를 맞춤 (스프라이트 없는 Image는 fillAmount가 동작하지 않음)
        if (_expSlider != null)
            _expSlider.SetValueWithoutNotify(ratio);
        else
            ApplyExpFillRect(ratio);
    }

    private void ApplyExpFillRect(float ratio)
    {
        if (_expAmountImage == null)
            return;

        RectTransform fillRect = _expAmountImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(ratio, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.pivot = new Vector2(0f, 0.5f);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    public override void Init()
    {
        // LevelSection 패널 경로 기준으로 UI 참조 연결함
        BindUiReferences();
    }

    private void BindUiReferences()
    {
        GameObject levelSection = FindChild(gameObject, "LevelSection", true);
        if (levelSection == null)
            return;

        // Screen(MoongChiStat)과 standalone(Panel) 구조 모두 지원함
        GameObject statRoot = FindChild(levelSection, "MoongChiStat", false)
                              ?? FindChild(levelSection, "Panel", false)
                              ?? levelSection;

        _levelText ??= FindPanelText(statRoot, "LvPanel")
                       ?? FindPanelText(statRoot, "Panel");

        _expCountText ??= FindSliderText(statRoot, "ExpPanel")
                          ?? FindSliderText(statRoot, "Panel (1)");

        _expSlider ??= FindExpSlider(statRoot, "ExpPanel")
                       ?? FindExpSlider(statRoot, "Panel (1)");

        _expAmountImage ??= FindSliderFillImage(statRoot, "ExpPanel")
                            ?? FindSliderFillImage(statRoot, "Panel (1)");

        BindInfoPanelTexts(statRoot);
        ConfigureExpSlider();
    }

    private void ConfigureExpSlider()
    {
        SetupExpFillRect();

        if (_expSlider == null)
            return;

        _expSlider.minValue = 0f;
        _expSlider.maxValue = 1f;
        _expSlider.wholeNumbers = false;

        if (_expAmountImage != null)
            _expSlider.fillRect = _expAmountImage.rectTransform;
    }

    private void SetupExpFillRect()
    {
        if (_expAmountImage == null)
            return;

        RectTransform fillRect = _expAmountImage.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        // Filled + 전체 stretch는 스프라이트 없을 때 항상 100%로 보임
        if (_expAmountImage.type == Image.Type.Filled)
            _expAmountImage.type = Image.Type.Sliced;
    }

    private void BindInfoPanelTexts(GameObject statRoot)
    {
        GameObject infoPanel = FindChild(statRoot, "InfoPanel", true);
        if (infoPanel == null)
            return;

        // Screen 프리팹은 Claw sharpness, Continuous scratching 이름 사용함
        _sharpnessText ??= FindPanelText(infoPanel, "Claw sharpness");
        _criticalChanceText ??= FindPanelText(infoPanel, "Continuous scratching");

        if (_sharpnessText != null && _criticalChanceText != null)
            return;

        // standalone InfoPanel은 Background 하위 텍스트 순서로 폴백함
        TMP_Text[] texts = infoPanel.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length == 0)
            return;

        if (_sharpnessText == null)
            _sharpnessText = texts[0];

        if (_criticalChanceText == null && texts.Length > 1)
            _criticalChanceText = texts[1];
    }

    private static TMP_Text FindPanelText(GameObject parent, string panelName)
    {
        if (parent == null)
            return null;

        GameObject panel = FindChild(parent, panelName, false);
        if (panel == null)
            return null;

        TMP_Text text = FindChild<TMP_Text>(panel, "Text (TMP)", false);
        return text != null ? text : panel.GetComponentInChildren<TMP_Text>(true);
    }

    private static TMP_Text FindSliderText(GameObject parent, string panelName)
    {
        GameObject slider = FindSliderObject(parent, panelName);
        if (slider == null)
            return null;

        TMP_Text text = FindChild<TMP_Text>(slider, "Text (TMP)", false);
        return text != null ? text : slider.GetComponentInChildren<TMP_Text>(true);
    }

    private static Slider FindExpSlider(GameObject parent, string panelName)
    {
        GameObject slider = FindSliderObject(parent, panelName);
        return slider != null ? slider.GetComponent<Slider>() : null;
    }

    private static Image FindSliderFillImage(GameObject parent, string panelName)
    {
        GameObject slider = FindSliderObject(parent, panelName);
        if (slider == null)
            return null;

        Transform fillArea = slider.transform.Find("Fill Area");
        if (fillArea == null)
            return null;

        Transform fill = fillArea.Find("Fill");
        return fill != null ? fill.GetComponent<Image>() : null;
    }

    private static GameObject FindSliderObject(GameObject parent, string panelName)
    {
        if (parent == null)
            return null;

        GameObject panel = FindChild(parent, panelName, false);
        if (panel == null)
            return null;

        return FindChild(panel, "Slider", false);
    }
}
