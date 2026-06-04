using DG.Tweening;
using TMPro;
using UnityEngine;


// TOUCH! 안내 텍스트 알파 깜빡임을 재생합니다.
public class TouchBlinkAnim : MonoBehaviour
{
    [SerializeField] private float _minAlpha = 0.25f;
    [SerializeField] private float _maxAlpha = 1f;
    [SerializeField] private float _blinkHalfDuration = 0.45f;

    private TMP_Text _touchText;
    private Tween _blinkTween;

    private void Awake()
    {
        _touchText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StartBlink();
    }

    private void OnDisable()
    {
        StopBlink();
    }

    
    // 깜빡임을 다시 시작합니다. 이미 활성인 오브젝트를 다시 켤 때 호출
    public void StartBlink()
    {
        if (_touchText == null)
            return;

        StopBlink();
        _touchText.alpha = _maxAlpha;

        _blinkTween = _touchText
            .DOFade(_minAlpha, _blinkHalfDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    
    // 깜빡임 트윈을 중단
    public void StopBlink()
    {
        _blinkTween?.Kill();
        _blinkTween = null;
        _touchText?.DOKill();
    }
}
