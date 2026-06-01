using System.Collections.Generic;
using System;
using UnityEngine;

public class ScratchEffectPool : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private ScratchEffectItem effectPrefab;
    [SerializeField] private RectTransform effectRoot;
    [SerializeField] private RectTransform touchArea;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<ScratchEffectItem> pool = new Queue<ScratchEffectItem>();

    public event Action OnScratchClicked;

    private void Awake()
    {
        CreatePool();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInsideTouchArea(Input.mousePosition))
            {
                return;
            }

            SpawnEffect(Input.mousePosition);
            OnScratchClicked?.Invoke();
        }
    }

    private bool IsInsideTouchArea(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            touchArea,
            screenPosition,
            null
        );
    }

    private void CreatePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            ScratchEffectItem item = Instantiate(effectPrefab, effectRoot);
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
    }

    private void SpawnEffect(Vector2 screenPosition)
    {
        ScratchEffectItem item = GetEffect();

        RectTransform itemRect = item.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectRoot,
            screenPosition,
            null,
            out Vector2 localPosition
        );

        itemRect.anchoredPosition = localPosition;
        itemRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-20f, 20f));
        itemRect.localScale = Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f);

        item.gameObject.SetActive(true);
        item.ResetEffect();
        item.Play(ReturnEffect);
    }

    private ScratchEffectItem GetEffect()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        ScratchEffectItem item = Instantiate(effectPrefab, effectRoot);
        item.gameObject.SetActive(false);
        return item;
    }

    private void ReturnEffect(ScratchEffectItem item)
    {
        item.ResetEffect();
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }
}
