using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScratchEffectItem : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float fadeOutTime = 0.4f;

    private Image image;
    private Coroutine playCoroutine;
    private Action<ScratchEffectItem> returnAction;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Play(Action<ScratchEffectItem> onFinished)
    {
        returnAction = onFinished;

        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }

        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(0f, 1f, fadeInTime);
        yield return Fade(1f, 0f, fadeOutTime);

        playCoroutine = null;
        returnAction?.Invoke(this);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        Color color = image.color;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            image.color = color;

            yield return null;
        }

        color.a = endAlpha;
        image.color = color;
    }

    public void ResetEffect()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        Color color = image.color;
        color.a = 0f;
        image.color = color;
    }
}