using System.Collections.Generic;
using UnityEngine;
using Util;

public class NyangNyangSnapCaptureRecorder : MonoBehaviour
{
    [Header("촬영 설정")]
    [Tooltip("최대 촬영 횟수")]
    [SerializeField] private int _maxCaptureCount = 10;

    private readonly List<NyangNyangSnapCaptureRecord> _records = new();

    private NyangNyangSnapCaptureRecord _bestRecord;

    public int CurrentCaptureCount => _records.Count;
    public int MaxCaptureCount => _maxCaptureCount;
    public bool IsCaptureComplete => CurrentCaptureCount >= _maxCaptureCount;
    public NyangNyangSnapCaptureRecord BestRecord => _bestRecord;
    public IReadOnlyList<NyangNyangSnapCaptureRecord> Records => _records;

    public void ClearRecords()
    {
        foreach (NyangNyangSnapCaptureRecord record in _records)
        {
            ReleaseSprite(record.CapturedSprite);
        }

        _records.Clear();
        _bestRecord = null;

        DebugTool.Log("[NyangNyangSnapCaptureRecorder] 촬영 기록 초기화 완료", DebugType.Game, this);
    }

    public void AddRecord(
        Sprite capturedSprite,
        NyangNyangSnapPoseData poseData,
        NyangNyangSnapScoreResult scoreResult)
    {
        if (capturedSprite == null)
        {
            DebugTool.Warning("[NyangNyangSnapCaptureRecorder] 저장할 캡처 Sprite가 없습니다.", DebugType.Game, this);
            return;
        }

        if (scoreResult == null)
        {
            DebugTool.Warning("[NyangNyangSnapCaptureRecorder] 저장할 점수 결과가 없습니다.", DebugType.Game, this);
            return;
        }

        if (IsCaptureComplete)
        {
            DebugTool.Log("[NyangNyangSnapCaptureRecorder] 이미 최대 촬영 횟수에 도달했습니다.", DebugType.Game, this);
            return;
        }

        Sprite copiedSprite = CopySprite(capturedSprite);

        NyangNyangSnapCaptureRecord record = new(
            copiedSprite,
            poseData,
            scoreResult
        );

        _records.Add(record);
        UpdateBestRecord(record);

        DebugTool.Log(
            $"[NyangNyangSnapCaptureRecorder] 촬영 결과 저장 완료 " +
            $"({CurrentCaptureCount}/{MaxCaptureCount}) / " +
            $"현재 점수: {record.TotalScore} / " +
            $"최고 점수: {_bestRecord.TotalScore}",
            DebugType.Game,
            this
        );
    }

    private void UpdateBestRecord(NyangNyangSnapCaptureRecord newRecord)
    {
        if (_bestRecord == null)
        {
            _bestRecord = newRecord;
            return;
        }

        if (newRecord.TotalScore > _bestRecord.TotalScore)
            _bestRecord = newRecord;
    }

    private Sprite CopySprite(Sprite sourceSprite)
    {
        Texture2D sourceTexture = sourceSprite.texture;

        Texture2D copiedTexture = new Texture2D(
            sourceTexture.width,
            sourceTexture.height,
            TextureFormat.RGBA32,
            false
        );

        copiedTexture.SetPixels(sourceTexture.GetPixels());
        copiedTexture.Apply();

        Sprite copiedSprite = Sprite.Create(
            copiedTexture,
            new Rect(0, 0, copiedTexture.width, copiedTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        copiedSprite.name = $"NyangNyangSnap_Captured_{CurrentCaptureCount + 1}";

        return copiedSprite;
    }

    private void ReleaseSprite(Sprite sprite)
    {
        if (sprite == null) return;

        Texture2D texture = sprite.texture;

        Destroy(sprite);
        Destroy(texture);
    }

    private void OnDestroy()
    {
        ClearRecords();
    }
}