using System;
using DG.Tweening;
using UnityEngine;


// UI 패널 닫기 시 스케일 축소와 페이드 아웃을 재사용
public static class UIPanelCloseTween
{
    
    // 패널 닫기 트윈을 재생. content·fade 대상이 모두 없으면 즉시 완료
    public static Sequence Play(
        Transform contentPanel,
        CanvasGroup fadeTarget,
        float contentScaleTo,
        float duration,
        Action onComplete)
    {
        contentPanel?.DOKill();
        fadeTarget?.DOKill();

        if (contentPanel == null && fadeTarget == null)
        {
            onComplete?.Invoke();
            return null;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (contentPanel != null)
            sequence.Join(contentPanel.DOScale(Vector3.one * contentScaleTo, duration).SetEase(Ease.InSine));

        if (fadeTarget != null)
            sequence.Join(fadeTarget.DOFade(0f, duration).SetEase(Ease.InSine));

        sequence.OnComplete(() => onComplete?.Invoke());
        return sequence;
    }

    
    // 패널을 다시 열기 전 기본 표시 상태로 되돌리기
    public static void PrepareShow(Transform contentPanel, CanvasGroup fadeTarget, float dimAlpha = 1f)
    {
        if (contentPanel != null)
            contentPanel.localScale = Vector3.one;

        if (fadeTarget != null)
            fadeTarget.alpha = dimAlpha;
    }


    // 페이드 대상에 CanvasGroup을 보장
    public static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = target.AddComponent<CanvasGroup>();

        return canvasGroup;
    }


    // 실행 중인 닫기 트윈을 중단
    public static void Kill(Transform contentPanel, CanvasGroup fadeTarget, Sequence sequence)
    {
        sequence?.Kill();
        contentPanel?.DOKill();
        fadeTarget?.DOKill();
    }
}
