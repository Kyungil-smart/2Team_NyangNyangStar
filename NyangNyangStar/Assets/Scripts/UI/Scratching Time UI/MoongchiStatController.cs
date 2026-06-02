using Data.ScriptableObjects.ScratchingTimeSO;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뭉치 스탯 정보를 UI에 출력하고 경험치 증가 요청을 전달합니다.
/// </summary>
public class MoongchiStatController : UIBase
{
    [Header("Data")] [SerializeField] private MoongchiStatSo _moongchiStat;

    [Header("Stat Text")] [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _sharpnessText;
    [SerializeField] private TMP_Text _criticalChanceText;

    [Header("Exp Text")] [SerializeField] private TMP_Text _expCountText; // ExpPanel Slider 통합 텍스트 연결용임
    [SerializeField] private TMP_Text _currentExpText;
    [SerializeField] private TMP_Text _expSlashText;
    [SerializeField] private TMP_Text _maxExpText;

    [Header("Exp Gauge")] [SerializeField] private Slider _expSlider; // ExpPanel Slider value 연동용임

    [Tooltip("뭉치 경험치 게이지로 사용할 Filled 타입 Image입니다.")] [SerializeField]
    private Image _expAmountImage;

    [SerializeField] private bool _isFirstTime = true;

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

        if (_isFirstTime)
        {
            _moongchiStat.Init();
            _isFirstTime = false;
        }

        PrintMoongchiStat();
    }

    public void IncreaseExp(int exp)
    {
        if (_moongchiStat == null)
            return;

        _moongchiStat.IncreaseExp(exp);
        PrintMoongchiStat();
        DebugTool.Log($"경험치 증가 : {exp}", DebugType.ScratchingTime);
    }

    public int MoongchiAttack()
    {
        if (_moongchiStat == null)
            return 0;

        return _moongchiStat.Attack();
    }

    public void PrintMoongchiStat()
    {
        if (_moongchiStat == null)
            return;

        int currentExp = _moongchiStat.GetCurrentExp();
        int maxExp = _moongchiStat.MaxExp;

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

        SetExpAmount(currentExp, maxExp);
    }

    public void SetExpAmount(int currentExp, int maxExp)
    {
        float ratio = GetSafeRatio(currentExp, maxExp);

        // Slider와 Fill Image 모두 갱신함
        if (_expSlider != null)
            _expSlider.value = ratio;

        if (_expAmountImage != null)
            _expAmountImage.fillAmount = ratio;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    private static float GetSafeRatio(float current, float max)
    {
        if (max <= 0)
            return 0f;

        return Mathf.Clamp01(current / max);
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