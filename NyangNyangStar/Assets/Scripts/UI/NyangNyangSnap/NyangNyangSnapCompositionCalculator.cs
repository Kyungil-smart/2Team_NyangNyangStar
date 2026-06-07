using UnityEngine;
using Util;

public class NyangNyangSnapCompositionCalculator : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("RenderTexture 촬영에 사용하는 카메라")]
    [SerializeField] private Camera _captureCamera;

    [Header("구도 계산 대상")]
    [Tooltip("촬영 대상 고양이 RectTransform")]
    [SerializeField] private RectTransform _targetCatRect;

    [Header("구도 계산 설정")]
    [Tooltip("중앙에서 이 비율 거리까지는 만점 처리")]
    [Range(0f, 1f)]
    [SerializeField] private float _perfectRadiusRate = 0.12f;

    [Tooltip("중앙에서 이 비율 거리 이상 벗어나면 0점 처리")]
    [Range(0f, 1f)]
    [SerializeField] private float _failRadiusRate = 0.5f;

    public float CalculateCompositionRate()
    {
        if (_captureCamera == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] CaptureCamera가 없습니다.", DebugType.Game, this);
            return 0f;
        }

        if (_targetCatRect == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] TargetCatRect가 없습니다.", DebugType.Game, this);
            return 0f;
        }

        Vector3 catWorldCenter = GetRectWorldCenter(_targetCatRect);
        Vector3 viewportPosition = _captureCamera.WorldToViewportPoint(catWorldCenter);

        if (viewportPosition.z < 0f)
        {
            DebugTool.Log("[냥냥스냅 구도 계산] 고양이가 카메라 뒤에 있습니다. 구도 비율 0", DebugType.Game, this);
            return 0f;
        }

        bool isOutOfCameraView =
            viewportPosition.x < 0f ||
            viewportPosition.x > 1f ||
            viewportPosition.y < 0f ||
            viewportPosition.y > 1f;

        if (isOutOfCameraView)
        {
            DebugTool.Log("[냥냥스냅 구도 계산] 고양이가 CaptureCamera 화면 밖에 있습니다. 구도 비율 0", DebugType.Game, this);
            return 0f;
        }

        float normalizedX = (viewportPosition.x - 0.5f) * 2f;
        float normalizedY = (viewportPosition.y - 0.5f) * 2f;

        float distanceFromCenter = Mathf.Sqrt(
            normalizedX * normalizedX +
            normalizedY * normalizedY
        );

        float compositionRate = CalculateRateByDistance(distanceFromCenter);

        DebugTool.Log(
            $"[냥냥스냅 구도 계산] " +
            $"Viewport: ({viewportPosition.x:F3}, {viewportPosition.y:F3}), " +
            $"정규화 거리: {distanceFromCenter:F3}, " +
            $"구도 비율: {compositionRate:F2}",
            DebugType.Game,
            this
        );

        return compositionRate;
    }

    private Vector3 GetRectWorldCenter(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        return (corners[0] + corners[2]) * 0.5f;
    }

    private float CalculateRateByDistance(float distanceFromCenter)
    {
        if (distanceFromCenter <= _perfectRadiusRate)
            return 1f;

        if (distanceFromCenter >= _failRadiusRate)
            return 0f;

        float normalizedDistance = Mathf.InverseLerp(
            _perfectRadiusRate,
            _failRadiusRate,
            distanceFromCenter
        );

        return Mathf.Clamp01(1f - normalizedDistance);
    }
}