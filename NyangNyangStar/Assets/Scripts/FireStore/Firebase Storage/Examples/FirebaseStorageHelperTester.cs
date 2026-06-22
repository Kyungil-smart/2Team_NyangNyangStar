using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


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

    // ───────────── 진입점 (버튼 OnClick / 컨텍스트 메뉴) ─────────────

    [ContextMenu("1) 단일 업로드 테스트")]
    public void RunSingleUploadTest() => _ = SingleUploadAsync();

    [ContextMenu("2) 다중 업로드 테스트")]
    public void RunMultiUploadTest() => _ = MultiUploadAsync();

    // ───────────── 테스트 본체 ────────────

    private async Task SingleUploadAsync()
    {
        if (!IsReady()) return;

        string id = MakePhotoId(0);
        byte[] png = MakeTestPng(RandomColor());

        Debug.Log($"[Tester] 단일 업로드 시작: StorageTest/{id}.png ({png.Length} bytes)");

        var result = await FirebaseStorageHelper.UploadUserImageAsync($"StorageTest/{id}.png", png);

        LogResult(0, result);

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

        if (_testDownloadAndDelete)
        {
            for (int i = 0; i < results.Length; i++)
                if (results[i].Success)
                    await DownloadAndDeleteAsync(results[i].StoragePath);
        }
    }

    private async Task DownloadAndDeleteAsync(string storagePath)
    {
        string url = await FirebaseStorageHelper.GetDownloadUrlAsync(storagePath);
        Debug.Log($"[Tester] 재조회 URL: {storagePath}\n  → {url}");

        bool deleted = await FirebaseStorageHelper.DeleteUserImageAsync(storagePath);
        Debug.Log($"[Tester] 삭제 {(deleted ? "성공" : "실패")}: {storagePath}");
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
