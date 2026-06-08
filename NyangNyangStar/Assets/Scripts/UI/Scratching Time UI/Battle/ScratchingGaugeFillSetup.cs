using UnityEngine;
using UnityEngine.UI;

// Interest/Durability 슬라이더 Fill을 Background(Shape_Rectangle) 안에만 보이게 맞춤
public static class ScratchingGaugeFillSetup
{
    public static void Apply(GameObject sliderRoot)
    {
        if (sliderRoot == null)
            return;

        Apply(sliderRoot.GetComponent<Slider>());
    }

    public static void Apply(Slider slider)
    {
        if (slider == null)
            return;

        Transform background = slider.transform.Find("Background");
        Transform fillArea = background != null ? background.Find("Fill Area") : null;
        if (fillArea == null)
            fillArea = slider.transform.Find("Fill Area");

        if (fillArea == null)
            return;

        Transform fillTransform = fillArea.Find("Fill");
        if (fillTransform == null)
            return;

        Image fillImage = fillTransform.GetComponent<Image>();
        if (fillImage == null)
            return;

        if (background != null)
            PlaceFillAreaInsideBackground(background, fillArea);

        RectTransform fillRect = fillTransform as RectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        fillImage.type = Image.Type.Sliced;
        fillImage.fillAmount = 1f;
        fillImage.preserveAspect = false;

        slider.fillRect = fillRect;

        float value = slider.value;
        slider.value = 0f;
        slider.value = value;
    }

    private static void PlaceFillAreaInsideBackground(Transform background, Transform fillArea)
    {
        if (fillArea.parent != background)
            fillArea.SetParent(background, false);

        RectTransform fillAreaRect = fillArea as RectTransform;
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        RectMask2D rectMask = fillArea.GetComponent<RectMask2D>();
        if (rectMask != null)
        {
            if (Application.isPlaying)
                Object.Destroy(rectMask);
            else
                Object.DestroyImmediate(rectMask);
        }

        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
            return;

        Mask mask = background.GetComponent<Mask>();
        if (mask == null)
            mask = background.gameObject.AddComponent<Mask>();

        mask.showMaskGraphic = true;
    }
}
