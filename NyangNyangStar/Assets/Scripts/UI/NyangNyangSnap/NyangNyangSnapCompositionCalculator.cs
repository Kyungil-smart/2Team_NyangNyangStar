using UnityEngine;
using Util;
using UnityEngine.UI;

public class NyangNyangSnapCompositionCalculator : MonoBehaviour
{
    private const int MaxCompositionScore = 25;

    private const float PerfectDistanceRate = 0.08f;
    private const float NearDistanceRate = 0.15f;
    private const float MidDistanceRate = 0.25f;

    [Header("Capture")]
    [Tooltip("RenderTexture 촬영에 사용하는 카메라")]
    [SerializeField] private Camera _captureCamera;

    [Header("구도 계산 대상")]
    [Tooltip("고양이와 목표 마커가 함께 배치된 촬영 영역")]
    [SerializeField] private RectTransform _photoRoot;

    [Tooltip("촬영 대상 고양이 RectTransform")]
    [SerializeField] private RectTransform _targetCatRect;

    [Tooltip("랜덤 촬영 목표를 표시할 RectTransform")]
    [SerializeField] private RectTransform _targetPointRect;

    [Tooltip("촬영 순간에만 숨길 구도 목표 Image")]
    [SerializeField] private Image _targetPointImage;

    [Header("목표 생성 설정")]
    [Tooltip("촬영 영역 좌우 가장자리에서 제외할 비율")]
    [Range(0f, 0.4f)]
    [SerializeField] private float _horizontalPaddingRate = 0.1f;

    [Tooltip("촬영 영역 상하 가장자리에서 제외할 비율")]
    [Range(0f, 0.4f)]
    [SerializeField] private float _verticalPaddingRate = 0.1f;

    /// <summary>
    /// 촬영 영역 안에 새로운 랜덤 목표 지점을 생성합니다.
    /// </summary>
    public void RandomizeTargetPoint()
    {
        if (!ValidateTargetSetup())
            return;

        Rect rootRect = _photoRoot.rect;
        Rect targetRect = _targetPointRect.rect;

        float horizontalPadding = rootRect.width * _horizontalPaddingRate;
        float verticalPadding = rootRect.height * _verticalPaddingRate;

        float halfTargetWidth = targetRect.width * 0.5f;
        float halfTargetHeight = targetRect.height * 0.5f;

        float minX = rootRect.xMin + horizontalPadding + halfTargetWidth;
        float maxX = rootRect.xMax - horizontalPadding - halfTargetWidth;
        float minY = rootRect.yMin + verticalPadding + halfTargetHeight;
        float maxY = rootRect.yMax - verticalPadding - halfTargetHeight;

        if (minX > maxX || minY > maxY)
        {
            DebugTool.Warning(
                "[냥냥스냅 구도 계산] PhotoRoot보다 목표 마커 또는 여백이 너무 큽니다.",
                DebugType.Game,
                this
            );
            return;
        }

        Vector2 randomLocalPosition = new(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );

        _targetPointRect.localPosition = new Vector3(
            randomLocalPosition.x,
            randomLocalPosition.y,
            _targetPointRect.localPosition.z
        );

        SetTargetVisible(true);

        DebugTool.Log(
            $"[냥냥스냅 구도 계산] 랜덤 목표 생성 완료 / " +
            $"LocalPosition: ({randomLocalPosition.x:F1}, {randomLocalPosition.y:F1})",
            DebugType.Game,
            this
        );
    }

    public void SetTargetVisible(bool isVisible)
    {
        if (_targetPointRect == null)
            return;

        _targetPointRect.gameObject.SetActive(isVisible);
    }

    public void SetTargetImageVisible(bool isVisible)
    {
        if (_targetPointImage == null && _targetPointRect != null)
        {
            _targetPointImage = _targetPointRect.GetComponent<Image>();
        }

        if (_targetPointImage == null)
        {
            DebugTool.Warning(
                "[냥냥스냅 구도 계산] TargetPointImage를 찾지 못했습니다.",
                DebugType.Game,
                this
            );

            return;
        }

        _targetPointImage.enabled = isVisible;
    }
    /// <summary>
    /// 기존 점수 계산 코드와 연결하기 위한 0~1 구도 비율입니다.
    /// 내부 판정은 기획 기준인 0~25점으로 처리합니다.
    /// </summary>
    public float CalculateCompositionRate()
    {
        int score = CalculateCompositionScore();
        return score / (float)MaxCompositionScore;
    }

    /// <summary>
    /// 랜덤 목표와 고양이 사이의 거리를 기준으로 0~25점을 계산합니다.
    /// 거리 비율은 RenderTexture 촬영 화면의 가로 길이를 기준으로 합니다.
    /// </summary>
    public int CalculateCompositionScore()
    {
        if (!ValidateScoreSetup())
            return 0;

        Vector3 catViewportPosition = GetViewportPosition(_targetCatRect);
        Vector3 targetViewportPosition = GetViewportPosition(_targetPointRect);

        if (!IsInsideCaptureView(catViewportPosition, "고양이"))
            return 0;

        if (!IsInsideCaptureView(targetViewportPosition, "목표 마커"))
            return 0;

        float distanceRate = CalculateDistanceRateByScreenWidth(
            catViewportPosition,
            targetViewportPosition
        );

        int compositionScore = CalculateScoreByDistanceRate(distanceRate);

        DebugTool.Log(
            $"[냥냥스냅 구도 계산] " +
            $"CatViewport: ({catViewportPosition.x:F3}, {catViewportPosition.y:F3}), " +
            $"TargetViewport: ({targetViewportPosition.x:F3}, {targetViewportPosition.y:F3}), " +
            $"DistanceRate: {distanceRate:F3}, " +
            $"Score: {compositionScore}/{MaxCompositionScore}",
            DebugType.Game,
            this
        );

        return compositionScore;
    }

    private Vector3 GetViewportPosition(RectTransform targetRect)
    {
        Vector3 worldCenter = GetRectWorldCenter(targetRect);
        return _captureCamera.WorldToViewportPoint(worldCenter);
    }

    private Vector3 GetRectWorldCenter(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        return (corners[0] + corners[2]) * 0.5f;
    }

    private bool IsInsideCaptureView(Vector3 viewportPosition, string targetName)
    {
        if (viewportPosition.z < 0f)
        {
            DebugTool.Log(
                $"[냥냥스냅 구도 계산] {targetName}이 CaptureCamera 뒤에 있습니다. 0점 처리합니다.",
                DebugType.Game,
                this
            );
            return false;
        }

        bool isOutside =
            viewportPosition.x < 0f ||
            viewportPosition.x > 1f ||
            viewportPosition.y < 0f ||
            viewportPosition.y > 1f;

        if (isOutside)
        {
            DebugTool.Log(
                $"[냥냥스냅 구도 계산] {targetName}이 CaptureCamera 화면 밖에 있습니다. 0점 처리합니다.",
                DebugType.Game,
                this
            );
            return false;
        }

        return true;
    }

    private float CalculateDistanceRateByScreenWidth(
        Vector3 catViewportPosition,
        Vector3 targetViewportPosition)
    {
        float width = GetCaptureWidth();
        float height = GetCaptureHeight();

        float horizontalDistanceRate =
            Mathf.Abs(catViewportPosition.x - targetViewportPosition.x);

        float verticalViewportDistance =
            Mathf.Abs(catViewportPosition.y - targetViewportPosition.y);

        // 세로 거리를 촬영 화면의 가로 길이 기준 비율로 변환합니다.
        float verticalDistanceRate = verticalViewportDistance * (height / width);

        return Mathf.Sqrt(
            horizontalDistanceRate * horizontalDistanceRate +
            verticalDistanceRate * verticalDistanceRate
        );
    }

    private float GetCaptureWidth()
    {
        if (_captureCamera.targetTexture != null)
            return Mathf.Max(1f, _captureCamera.targetTexture.width);

        return Mathf.Max(1f, _captureCamera.pixelWidth);
    }

    private float GetCaptureHeight()
    {
        if (_captureCamera.targetTexture != null)
            return Mathf.Max(1f, _captureCamera.targetTexture.height);

        return Mathf.Max(1f, _captureCamera.pixelHeight);
    }

    private int CalculateScoreByDistanceRate(float distanceRate)
    {
        if (distanceRate <= PerfectDistanceRate)
            return 25;

        if (distanceRate <= NearDistanceRate)
            return 15;

        if (distanceRate <= MidDistanceRate)
        {
            float normalizedDistance = Mathf.InverseLerp(
                NearDistanceRate,
                MidDistanceRate,
                distanceRate
            );

            return Mathf.RoundToInt(
                Mathf.Lerp(15f, 5f, normalizedDistance)
            );
        }

        return 0;
    }

    private bool ValidateTargetSetup()
    {
        if (_photoRoot == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] PhotoRoot가 없습니다.", DebugType.Game, this);
            return false;
        }

        if (_targetPointRect == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] TargetPointRect가 없습니다.", DebugType.Game, this);
            return false;
        }

        if (_targetPointRect.parent != _photoRoot)
        {
            DebugTool.Warning(
                "[냥냥스냅 구도 계산] TargetPointRect는 PhotoRoot의 직접 자식이어야 합니다.",
                DebugType.Game,
                this
            );
            return false;
        }

        return true;
    }

    private bool ValidateScoreSetup()
    {
        if (_captureCamera == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] CaptureCamera가 없습니다.", DebugType.Game, this);
            return false;
        }

        if (_targetCatRect == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] TargetCatRect가 없습니다.", DebugType.Game, this);
            return false;
        }

        if (_targetPointRect == null)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] TargetPointRect가 없습니다.", DebugType.Game, this);
            return false;
        }

        if (!_targetPointRect.gameObject.activeInHierarchy)
        {
            DebugTool.Warning("[냥냥스냅 구도 계산] 목표 마커가 비활성화 상태입니다.", DebugType.Game, this);
            return false;
        }

        return true;
    }
}
