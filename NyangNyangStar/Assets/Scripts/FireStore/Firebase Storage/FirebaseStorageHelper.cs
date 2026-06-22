using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Storage;
using UnityEngine;


public static class FirebaseStorageHelper
{
    /// <summary>계정 루트 prefix. Firestore 경로 규칙(Users/{uid}/...)과 맞춘다.</summary>
    private const string UserRoot = "Users";

    /// <summary>업로드 1건 결과.</summary>
    public readonly struct StorageUploadResult
    {
        public readonly bool Success;
        public readonly string StoragePath;   // 저장/재다운로드/삭제에 그대로 다시 넘기는 상대 경로(Users/{uid}/ 제외)
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

            // 저장값은 상대 경로(Users/{uid}/ 제외) — 다운로드/삭제에 그대로 다시 넘기면 된다.
            string storagePath = NormalizeRelative(relativePath);

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

    /// <summary>저장해 둔 relativePath로 표시용 download URL을 다시 얻는다(업로드와 같은 상대 경로).</summary>
    public static async Task<string> GetDownloadUrlAsync(string relativePath)
    {
        if (!TryGetUserId(out string uid, out _)) return string.Empty;

        string fullPath = BuildUserPath(uid, relativePath);
        if (string.IsNullOrEmpty(fullPath)) return string.Empty;

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(fullPath);
            Uri url = await reference.GetDownloadUrlAsync();
            return url != null ? url.ToString() : string.Empty;
        }
        catch (Exception ex)
        {
            DebugTool.Warning($"[FirebaseStorageHelper] GetDownloadUrl 실패: {ex.Message}", DebugType.Network);
            return string.Empty;
        }
    }

    /// <summary>
    /// relativePath의 이미지를 받아 Sprite로 만들어 반환한다(인게임 표시용). 실패 시 null.
    /// 업로드와 같은 상대 경로(Users/{uid}/ 제외)를 넘긴다.
    /// 주의: 반환된 Sprite의 texture는 호출부가 다 쓴 뒤 Destroy 해야 메모리 누수가 없다.
    /// maxBytes: 다운로드 허용 최대 크기(메모리 폭주 방지). 기본 5MB.
    /// </summary>
    public static async Task<Sprite> LoadUserSpriteAsync(string relativePath, long maxBytes = 5 * 1024 * 1024)
    {
        if (!TryGetUserId(out string uid, out _)) return null;

        string fullPath = BuildUserPath(uid, relativePath);
        if (string.IsNullOrEmpty(fullPath)) return null;

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(fullPath);
            byte[] bytes = await reference.GetBytesAsync(maxBytes);
            if (bytes == null || bytes.Length == 0) return null;

            Texture2D tex = new(2, 2);
            if (!tex.LoadImage(bytes)) // PNG/JPG 자동 디코드. 실패 시 정리 후 null
            {
                UnityEngine.Object.Destroy(tex);
                DebugTool.Warning($"[FirebaseStorageHelper] 이미지 디코드 실패: {fullPath}", DebugType.Network);
                return null;
            }

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        catch (Exception ex)
        {
            DebugTool.Warning($"[FirebaseStorageHelper] LoadUserSprite 실패: {ex.Message}", DebugType.Network);
            return null;
        }
    }

    /// <summary>relativePath의 이미지를 삭제한다(업로드와 같은 상대 경로). 성공 여부를 반환.</summary>
    public static async Task<bool> DeleteUserImageAsync(string relativePath)
    {
        if (!TryGetUserId(out string uid, out _)) return false;

        string fullPath = BuildUserPath(uid, relativePath);
        if (string.IsNullOrEmpty(fullPath)) return false;

        try
        {
            StorageReference reference = FirebaseStorage.DefaultInstance.GetReference(fullPath);
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

    /// <summary>relativePath 정규화: 역슬래시→슬래시, 앞뒤 슬래시 제거. 비면 빈 문자열.</summary>
    private static string NormalizeRelative(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
        return relativePath.Replace('\\', '/').Trim('/');
    }

    /// <summary>"Users/{uid}/{relativePath}" 전체 경로 조합.</summary>
    private static string BuildUserPath(string uid, string relativePath)
    {
        string trimmed = NormalizeRelative(relativePath);
        if (string.IsNullOrEmpty(trimmed)) return string.Empty;

        return $"{UserRoot}/{uid}/{trimmed}";
    }

    private static StorageUploadResult Fail(string relativePath, string message)
    {
        DebugTool.Warning($"[FirebaseStorageHelper] 업로드 실패 ({relativePath}): {message}", DebugType.Network);
        return new StorageUploadResult(false, string.Empty, string.Empty, message);
    }
}
