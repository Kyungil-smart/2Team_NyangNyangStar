using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    [Header("로딩 바 이미지")]
    [SerializeField] private Image _loadingBar;
    [Space(3)] [Header("로딩 텍스트")]
    [SerializeField] private TMP_Text _loadingText;
    [Header("로딩 텍스트 배경")]
    [SerializeField] private Image _loadingTextBackground;

    [NonSerialized] public int TotalProgress;
    private int _loadingProgress = 0;
    
    private void Awake()
        => Init();

    private void Start()
        => ComponentInit();

    public void OnProceedLoading()
    {
        _loadingProgress++;
        _loadingBar.fillAmount = (float)_loadingProgress / TotalProgress;
        _loadingText.text = "불러오는 중 ..." + $"{_loadingProgress} / {TotalProgress}";
        
        if (_loadingProgress >= TotalProgress)
            _loadingText.text = "불러오기 완료.";
    }

    private void Init()
    {
        if(_loadingBar == null)
            DebugTool.Warning("로딩 바 이미지 컴포넌트가 없습니다.", DebugType.Missing);
        
        if(_loadingText == null)
            DebugTool.Warning("로딩 텍스트 컴포넌트가 없습니다.", DebugType.Missing);
    }

    private void ComponentInit()
    {
        _loadingBar.fillAmount = 0f;
    }
}