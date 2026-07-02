using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NyangNyangSnapRuntimePhoto", menuName = "SO/Data/NyangNyangSnapRuntimePhotoSO", order = 0)]
public class NyangNyangSnapRuntimePhotoSO : ScriptableObject
{
    [SerializeField] private List<NyangNyangSnapRuntimePhotoData> _runtimePhotos = new();
    public IReadOnlyList<NyangNyangSnapRuntimePhotoData> RuntimePhotos => _runtimePhotos;

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

[Serializable]
public class NyangNyangSnapRuntimePhotoData
{
    [SerializeField] private string _photoId;
    [SerializeField] private Sprite _sprite;
    [SerializeField] private string _storagePath;
    [SerializeField] private int _starCount;
    [SerializeField] private Timestamp _createdAt;

    public string PhotoId => _photoId;
    public Sprite Sprite => _sprite;
    public string StoragePath => _storagePath;
    public int StarCount => _starCount;
    public Timestamp CreatedAt => _createdAt;

    public NyangNyangSnapRuntimePhotoData(
        string photoId,
        Sprite sprite,
        string storagePath,
        int starCount,
        Timestamp createdAt)
    {
        _photoId = photoId;
        _sprite = sprite;
        _storagePath = storagePath;
        _starCount = starCount;
        _createdAt = createdAt;
    }
}