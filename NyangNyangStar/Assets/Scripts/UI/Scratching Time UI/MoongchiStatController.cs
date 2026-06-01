using Data.ScriptableObjects.ScratchingTimeSO;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뭉치 스탯 정보를 UI에 출력하고 경험치 증가 요청을 전달합니다.
/// 경험치는 Filled 타입 Image의 fillAmount와 분리된 현재/최대 텍스트로 표시합니다.
/// </summary>
public class MoongchiStatController : UIBase
{
    [Header("Data")]
    [SerializeField] private MoongchiStatSo _moongchiStat;

    [Header("Stat Text")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _sharpnessText;
    [SerializeField] private TMP_Text _criticalChanceText;

    [Header("Exp Text")]
    [SerializeField] private TMP_Text _currentExpText;
    [SerializeField] private TMP_Text _expSlashText;
    [SerializeField] private TMP_Text _maxExpText;

    [Header("Exp Gauge Image")]
    [Tooltip("뭉치 경험치 게이지로 사용할 Filled 타입 Image입니다.")]
    [SerializeField] private Image _expAmountImage;

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

    /// <summary>
    /// 뭉치 스탯을 초기화하고 현재 스탯 정보를 출력합니다.
    /// </summary>
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

    /// <summary>
    /// 뭉치에게 경험치를 지급하고 UI를 갱신합니다.
    /// </summary>
    /// <param name="exp">지급할 경험치</param>
    public void IncreaseExp(int exp)
    {
        if (_moongchiStat == null)
            return;

        _moongchiStat.IncreaseExp(exp);
        PrintMoongchiStat();
        DebugTool.Log($"경험치 증가 : {exp}", DebugType.ScratchingTime);
    }

    /// <summary>
    /// 뭉치의 공격 데미지를 계산하여 반환합니다.
    /// </summary>
    /// <returns>뭉치의 공격 데미지</returns>
    public int MoongchiAttack()
    {
        if (_moongchiStat == null)
            return 0;

        return _moongchiStat.Attack();
    }

    /// <summary>
    /// 뭉치의 현재 레벨, 경험치 게이지, 공격력, 치명타 확률을 UI에 출력합니다.
    /// </summary>
    public void PrintMoongchiStat()
    {
        if (_moongchiStat == null)
            return;

        int currentExp = _moongchiStat.GetCurrentExp();
        int maxExp = _moongchiStat.MaxExp;

        SetText(_levelText, $"Lv. {_moongchiStat.Level} 뭉치");
        SetText(_currentExpText, currentExp.ToString());
        SetText(_expSlashText, "/");
        SetText(_maxExpText, maxExp.ToString());
        SetText(_sharpnessText, $"발톱 예리도 {_moongchiStat.GetSharpness()}");
        SetText(_criticalChanceText, $"힘껏 긁기 확률 {_moongchiStat.GetCriticalChance()}%");

        SetExpAmount(currentExp, maxExp);
    }

    /// <summary>
    /// 경험치 게이지 이미지를 현재/최대 경험치 비율로 표시합니다.
    /// </summary>
    /// <param name="currentExp">현재 경험치</param>
    /// <param name="maxExp">최대 경험치</param>
    public void SetExpAmount(int currentExp, int maxExp)
    {
        if (_expAmountImage == null)
            return;

        _expAmountImage.fillAmount = GetSafeRatio(currentExp, maxExp);
    }

    /// <summary>
    /// 텍스트 컴포넌트가 연결되어 있을 때만 문자열을 출력합니다.
    /// </summary>
    /// <param name="text">출력 대상 텍스트</param>
    /// <param name="value">출력할 문자열</param>
    private void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    /// <summary>
    /// 현재/최대 값으로 UI 게이지 비율을 계산합니다.
    /// </summary>
    /// <param name="current">현재 값</param>
    /// <param name="max">최대 값</param>
    /// <returns>0에서 1 사이로 제한된 비율 값</returns>
    private float GetSafeRatio(float current, float max)
    {
        if (max <= 0)
            return 0f;

        return Mathf.Clamp01(current / max);
    }

    public override void Init()
    {
        Bind<TMP_Text>(typeof(StatTexts));
        _expAmountImage = GetComponentInChildren<Image>();
        
        GetTexts();
        
    }

    private void GetTexts()
    {
        _levelText = Get<TMP_Text>((int)StatTexts.Level);
        _sharpnessText = Get<TMP_Text>((int)StatTexts.Sharpness);
        _criticalChanceText = Get<TMP_Text>((int)StatTexts.CriticalChance);
        _currentExpText = Get<TMP_Text>((int)StatTexts.CurrentExp);
        _expSlashText = Get<TMP_Text>((int)StatTexts.ExpSlash);
        _maxExpText = Get<TMP_Text>((int)StatTexts.MaxExp);
    }

    private enum StatTexts
    {
        Level,
        Sharpness,
        CriticalChance,
        CurrentExp,
        ExpSlash,
        MaxExp
    }
}
