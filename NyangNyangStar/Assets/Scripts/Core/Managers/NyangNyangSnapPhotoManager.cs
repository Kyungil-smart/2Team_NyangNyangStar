using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NyangNyangSnapPhotoManager : MonoBehaviour
{
    public static NyangNyangSnapPhotoManager Instance { get; private set; }

    private List<NyangNyangSnapRuntimePhotoData> _runtimePhotos = new();
    public IReadOnlyList<NyangNyangSnapRuntimePhotoData> RuntimePhotos => _runtimePhotos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddPhoto(NyangNyangSnapRuntimePhotoData photo)
    {
        _runtimePhotos.Add(photo);
    }

    public void ClearPhotos()
    {
        _runtimePhotos.Clear();
    }

    public void RemovePhoto(string photoId)
    {
        _runtimePhotos.RemoveAll(photo => photo.PhotoId == photoId);
    }
}

public class NyangNyangSnapRuntimePhotoData
{
    public string PhotoId { get; }
    public Sprite Sprite { get; }
    public string StoragePath { get; }
    public int StarCount { get; }
    public Timestamp CreatedAt { get; }

    public NyangNyangSnapRuntimePhotoData(
        string photoId,
        Sprite sprite,
        string storagePath,
        int starCount,
        Timestamp createdAt)
    {
        PhotoId = photoId;
        Sprite = sprite;
        StoragePath = storagePath;
        StarCount = starCount;
        CreatedAt = createdAt;
    }
}