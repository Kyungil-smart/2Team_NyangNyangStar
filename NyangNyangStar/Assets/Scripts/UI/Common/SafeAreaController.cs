using UnityEngine;

/// <summary>
/// 모바일 기기의 노치, 펀치홀, 홈 인디케이터 영역을 피해
/// UI 영역을 자동으로 조정합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaController : MonoBehaviour
{
    private RectTransform _rectTransform;

    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private ScreenOrientation _lastOrientation;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (!HasScreenChanged())
            return;

        ApplySafeArea();
    }

    /// <summary>
    /// 현재 기기의 Safe Area를 RectTransform 앵커에 적용합니다.
    /// </summary>
    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            Debug.LogWarning(
                "[SafeAreaController] 화면 크기를 가져올 수 없습니다.",
                this);

            return;
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;

        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;

        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;

        Debug.Log(
            $"[SafeAreaController] Safe Area 적용 완료 " +
            $"Screen: {Screen.width}x{Screen.height}, " +
            $"SafeArea: {safeArea}",
            this);
    }

    private bool HasScreenChanged()
    {
        return _lastSafeArea != Screen.safeArea ||
               _lastScreenSize.x != Screen.width ||
               _lastScreenSize.y != Screen.height ||
               _lastOrientation != Screen.orientation;
    }
}