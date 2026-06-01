using TMPro;
using UnityEngine;

public class TouchBlinkAnim : MonoBehaviour
{
    [SerializeField] private float minAlpha = 0.1f;
    [SerializeField] private float maxAlpha = 0.5f;
    [SerializeField] private float blinkDuration = 1f;

    private TMP_Text touchText;
    private float time;

    private void Awake()
    {
        touchText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        Blink();

        if (Input.GetMouseButtonDown(0))
        {
            gameObject.SetActive(false);
        }
    }

    private void Blink()
    {
        time += Time.deltaTime;

        float t = Mathf.PingPong(time / blinkDuration, 1f);
        float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);

        Color color = touchText.color;
        color.a = alpha;
        touchText.color = color;
    }
}