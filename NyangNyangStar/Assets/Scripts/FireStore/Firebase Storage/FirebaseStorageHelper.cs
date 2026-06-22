using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Storage;


public static class FirebaseStorageHelper
{
    /// <summary>계정 루트 prefix. Firestore 경로 규칙(Users/{uid}/...)과 맞춘다.</summary>
    private const string UserRoot = "Users";

    /// <summary>업로드 1건 결과.</summary>
    public readonly struct StorageUploadResult
    {
        public readonly bool Success;
        public readonly string StoragePath;   // 진실의 기준: 쿼리/삭제/재다운로드에 사용
        public readonly string DownloadUrl;   // 표시용 https URL(캐시 용도). 실패 시 빈 문자열
        public readonly string ErrorMessage;  // 실패 사유(성공 시 빈 문자열)

        public StorageUploadResult(bool success, string storagePath, string downloadUrl, string errorMessage)
        {
            Success = success;
            StoragePath = storagePath ?? string.Empty;
            DownloadUrl = downloadUrl ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    // ───────────────────────── 업로드 ─────────────────────────

    /// <summary>
    /// 이미지 1장 업로드. relativePath 예: "NyangNyangSnap/NNSnap_xxx.png"
    /// 실제 저장 경로는 "Users/{uid}/NyangNyangSnap/NNSnap_xxx.png" 가 된다.
    /// </summary>
    public static async Task<StorageUploadResult> UploadUserImageAsync(
        string relativePath, byte[] data, string contentType = "image/png")
    {
        if (data == null || data.Length == 0)
            return Fail(relativePath, "업로드할 데이터가 비어 있습니다.");

        if (!TryGetUserId(out string uid, out string reason))
            return Fail(relativePath, reason);

        string fullPath = BuildUserPath(uid, relativePath);
        if (string.IsNullOrEmpty(fullPath))
            return Fail(relativePath, "relativePath가 비어 있습니다.");

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(fullPath);

            MetadataChange metadata = new() { ContentType = contentType };
            await reference.PutBytesAsync(data, metadata);

            string storagePath = reference.Path;

            string downloadUrl = string.Empty;
            try
            {
                Uri url = await reference.GetDownloadUrlAsync();
                downloadUrl = url != null ? url.ToString() : string.Empty;
            }
            catch (Exception urlEx)
            {
                // 업로드는 성공했지만 URL 조회만 실패한 경우 — storagePath는 유효하므로 성공 처리.
                DebugTool.Warning(
                    $"[FirebaseStorageHelper] download URL 조회 실패(업로드는 성공): {urlEx.Message}",
                    DebugType.Network);
            }

            return new StorageUploadResult(true, storagePath, downloadUrl, string.Empty);
        }
        catch (Exception ex)
        {
            return Fail(relativePath, ex.Message);
        }
    }

    /// <summary>
    /// 여러 장을 한 번에 업로드. 기본은 무제한 병렬(Task.WhenAll),
    /// maxConcurrency &gt; 0 이면 동시 업로드 수를 제한한다.
    /// 반환 배열은 입력 순서와 1:1로 대응한다(부분 실패는 각 항목 Success로 판별).
    /// </summary>
    public static async Task<StorageUploadResult[]> UploadUserImagesAsync(
        IReadOnlyList<(string relativePath, byte[] data)> items,
        int maxConcurrency = 0)
    {
        if (items == null || items.Count == 0)
            return Array.Empty<StorageUploadResult>();

        // 무제한 병렬
        if (maxConcurrency <= 0)
        {
            Task<StorageUploadResult>[] tasks = new Task<StorageUploadResult>[items.Count];
            for (int i = 0; i < items.Count; i++)
                tasks[i] = UploadUserImageAsync(items[i].relativePath, items[i].data);

            return await Task.WhenAll(tasks);
        }

        // 동시 업로드 수 제한
        StorageUploadResult[] results = new StorageUploadResult[items.Count];
        using SemaphoreSlim sem = new(maxConcurrency);

        async Task RunOne(int idx)
        {
            await sem.WaitAsync();
            try { results[idx] = await UploadUserImageAsync(items[idx].relativePath, items[idx].data); }
            finally { sem.Release(); }
        }

        Task[] running = new Task[items.Count];
        for (int i = 0; i < items.Count; i++)
            running[i] = RunOne(i);

        await Task.WhenAll(running);
        return results;
    }

    // ───────────────────────── 조회 / 삭제 ─────────────────────────

    /// <summary>저장해 둔 storagePath로 표시용 download URL을 다시 얻는다.</summary>
    public static async Task<string> GetDownloadUrlAsync(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath)) return string.Empty;

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(storagePath);
            Uri url = await reference.GetDownloadUrlAsync();
            return url != null ? url.ToString() : string.Empty;
        }
        catch (Exception ex)
        {
            DebugTool.Warning($"[FirebaseStorageHelper] GetDownloadUrl 실패: {ex.Message}", DebugType.Network);
            return string.Empty;
        }
    }

    /// <summary>storagePath의 이미지를 삭제한다. 성공 여부를 반환.</summary>
    public static async Task<bool> DeleteUserImageAsync(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath)) return false;

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(storagePath);
            await reference.DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            DebugTool.Warning($"[FirebaseStorageHelper] Delete 실패: {ex.Message}", DebugType.Network);
            return false;
        }
    }

    // ───────────────────────── 내부 ─────────────────────────

    private static bool TryGetUserId(out string uid, out string reason)
    {
        uid = AuthManager.Instance != null ? AuthManager.Instance.CurrentUserId : null;

        if (string.IsNullOrEmpty(uid))
        {
            reason = "로그인 상태가 아닙니다(CurrentUserId 비어 있음).";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>"Users/{uid}/{relativePath}" 조합. 앞뒤 슬래시를 정리한다.</summary>
    private static string BuildUserPath(string uid, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;

        string trimmed = relativePath.Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(trimmed)) return string.Empty;

        return $"{UserRoot}/{uid}/{trimmed}";
    }

    private static StorageUploadResult Fail(string relativePath, string message)
    {
        DebugTool.Warning($"[FirebaseStorageHelper] 업로드 실패 ({relativePath}): {message}", DebugType.Network);
        return new StorageUploadResult(false, string.Empty, string.Empty, message);
    }
}
