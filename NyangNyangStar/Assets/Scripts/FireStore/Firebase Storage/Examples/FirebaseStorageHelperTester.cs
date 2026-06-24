using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


public class FirebaseStorageHelperTester : MonoBehaviour
{
    [Header("테스트 설정")]
    [Tooltip("다중 업로드 시 생성할 이미지 장수")]
    [SerializeField] private int _imageCount = 3;

    [Tooltip("생성할 테스트 이미지 한 변의 픽셀 수")]
    [SerializeField] private int _textureSize = 256;

    [Tooltip("0이면 무제한 병렬, 1 이상이면 동시 업로드 수 제한")]
    [SerializeField] private int _maxConcurrency = 0;

    [Tooltip("업로드 후 storagePath로 download URL 재조회 / 삭제까지 테스트")]
    [SerializeField] private bool _testDownloadAndDelete = false;

    [Header("다운로드 / 표시 테스트")]
    [Tooltip("업로드 후 다시 받아 Sprite로 표시까지 테스트")]
    [SerializeField] private bool _testLoadAndShow = false;

    [Tooltip("다운로드한 Sprite를 띄울 Image (비워두면 콘솔 로그로만 확인)")]
    [SerializeField] private Image _previewTarget;

    [Tooltip("수동 다운로드 표시 테스트용 상대 경로 예: StorageTest/xxx.png (Users/{uid}/ 제외)")]
    [SerializeField] private string _manualRelativePath;

    // ───────────── 진입점 (버튼 OnClick / 컨텍스트 메뉴) ─────────────

    [ContextMenu("1) 단일 업로드 테스트")]
    public void RunSingleUploadTest() => _ = SingleUploadAsync();

    [ContextMenu("2) 다중 업로드 테스트")]
    public void RunMultiUploadTest() => _ = MultiUploadAsync();

    [ContextMenu("3) 수동 경로 다운로드 표시")]
    public void RunLoadManualPathTest() => _ = LoadManualPathAsync();

    // ───────────── 테스트 본체 ────────────

    private async Task SingleUploadAsync()
    {
        if (!IsReady()) return;

        string id = MakePhotoId(0);
        byte[] png = MakeTestPng(RandomColor());

        Debug.Log($"[Tester] 단일 업로드 시작: StorageTest/{id}.png ({png.Length} bytes)");

        var result = await FirebaseStorageHelper.UploadUserImageAsync($"StorageTest/{id}.png", png);

        LogResult(0, result);

        if (result.Success && _testLoadAndShow)
            await ShowOnPreviewAsync(result.StoragePath);

        if (result.Success && _testDownloadAndDelete)
            await DownloadAndDeleteAsync(result.StoragePath);
    }

    private async Task MultiUploadAsync()
    {
        if (!IsReady()) return;

        int count = Mathf.Max(1, _imageCount);

        // 1) 메인 스레드에서 바이트부터 모두 준비 (EncodeToPNG는 메인 스레드 필수)
        var items = new List<(string relativePath, byte[] data)>(count);
        var ids = new List<string>(count);

        for (int i = 0; i < count; i++)
        {
            string id = MakePhotoId(i);
            ids.Add(id);
            items.Add(($"StorageTest/{id}.png", MakeTestPng(RandomColor())));
        }

        Debug.Log($"[Tester] 다중 업로드 시작: {count}장 (maxConcurrency={_maxConcurrency})");

        // 2) 병렬 업로드 후 한 번에 대기
        var results = await FirebaseStorageHelper.UploadUserImagesAsync(items, _maxConcurrency);

        // 3) 결과 집계
        int success = 0;
        for (int i = 0; i < results.Length; i++)
        {
            LogResult(i, results[i]);
            if (results[i].Success) success++;
        }

        Debug.Log($"[Tester] 다중 업로드 완료: {success}/{results.Length} 성공");

        if (_testLoadAndShow)
        {
            // 첫 성공 1장만 미리보기에 표시
            for (int i = 0; i < results.Length; i++)
                if (results[i].Success) { await ShowOnPreviewAsync(results[i].StoragePath); break; }
        }

        if (_testDownloadAndDelete)
        {
            for (int i = 0; i < results.Length; i++)
                if (results[i].Success)
                    await DownloadAndDeleteAsync(results[i].StoragePath);
        }
    }

    private async Task DownloadAndDeleteAsync(string relativePath)
    {
        string url = await FirebaseStorageHelper.GetDownloadUrlAsync(relativePath);
        Debug.Log($"[Tester] 재조회 URL: {relativePath}\n  → {url}");

        bool deleted = await FirebaseStorageHelper.DeleteUserImageAsync(relativePath);
        Debug.Log($"[Tester] 삭제 {(deleted ? "성공" : "실패")}: {relativePath}");
    }

    private async Task LoadManualPathAsync()
    {
        if (!IsReady()) return;

        if (string.IsNullOrEmpty(_manualRelativePath))
        {
            Debug.LogWarning("[Tester] _manualRelativePath를 입력하세요. 예: StorageTest/xxx.png (Users/{uid}/ 제외)");
            return;
        }

        await ShowOnPreviewAsync(_manualRelativePath);
    }

    /// <summary>relativePath로 Sprite를 받아 _previewTarget에 표시. 교체 시 이전 텍스처는 Destroy.</summary>
    private async Task ShowOnPreviewAsync(string relativePath)
    {
        Sprite sprite = await FirebaseStorageHelper.LoadUserSpriteAsync(relativePath);
        if (sprite == null)
        {
            Debug.LogError($"[Tester] Sprite 로드 실패: {relativePath}");
            return;
        }

        if (_previewTarget != null)
        {
            // 이전 미리보기 텍스처 정리 (누수 방지)
            if (_previewTarget.sprite != null && _previewTarget.sprite.texture != null)
                Destroy(_previewTarget.sprite.texture);

            _previewTarget.sprite = sprite;
            _previewTarget.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("[Tester] _previewTarget(Image)가 비어 있어 콘솔 로그로만 확인합니다.");
        }

        Debug.Log($"[Tester] Sprite 로드 성공: {relativePath} ({sprite.texture.width}x{sprite.texture.height})");
    }

    // ───────────── 유틸 ─────────────

    private bool IsReady()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Tester] Play 모드에서 실행하세요.");
            return false;
        }

        if (AuthManager.Instance == null || string.IsNullOrEmpty(AuthManager.Instance.CurrentUserId))
        {
            Debug.LogWarning("[Tester] 로그인 후 실행하세요. (AuthManager.CurrentUserId가 비어 있음)");
            return false;
        }

        Debug.Log($"[Tester] 현재 UID: {AuthManager.Instance.CurrentUserId}");
        return true;
    }

    private string MakePhotoId(int index)
        => $"Test_{DateTime.Now:yyMMdd_HH.mm.ss.fff}_{index}";

    private byte[] MakeTestPng(Color color)
    {
        int size = Mathf.Clamp(_textureSize, 8, 2048);
        Texture2D tex = new(size, size, TextureFormat.RGBA32, false);

        Color32[] pixels = new Color32[size * size];
        Color32 c = color;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = c;

        tex.SetPixels32(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);
        return png;
    }

    private Color RandomColor()
        => new(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);

    private void LogResult(int index, FirebaseStorageHelper.StorageUploadResult r)
    {
        if (r.Success)
            Debug.Log($"[Tester] #{index} 성공\n  storagePath: {r.StoragePath}\n  downloadUrl: {r.DownloadUrl}");
        else
            Debug.LogError($"[Tester] #{index} 실패: {r.ErrorMessage}");
    }
}
