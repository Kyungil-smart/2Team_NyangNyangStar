using Firebase.Firestore;
using FireStoreTest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[FirestorePath("Users/{userId}/NyangNyangSnap/{docId}")]
[CreateAssetMenu(fileName = "NyangNyangSnapPhotoAlbumSO", menuName = "ScriptableObjects/NyangNyangSnapPhotoAlbumSO")]
public class NyangNyangSnapPhotoAlbumSO : BaseFireStore
{
    [FirestoreMap]
    [SerializeField] private List<NyangNyangSnapSavedPhotoData> photos = new();

    public IReadOnlyList<NyangNyangSnapSavedPhotoData> Photos => photos;

    public void AddPhoto(NyangNyangSnapSavedPhotoData photo)
    {
        photos.Add(photo);
    }

    public void RemovePhoto(string photoId)
    {
        photos.RemoveAll(x => x.photoId == photoId);
    }

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        photos = new List<NyangNyangSnapSavedPhotoData>();

        await base.CreateNew(database, userId);
    }
}

[Serializable]
public struct NyangNyangSnapSavedPhotoData
{
    [FirestoreMapKey] public string photoId;

    public string imageUrl;
    public string storagePath;
    public int poseScore; // 포즈 점수
    public int compositionScore; // 구도 점수
    public int timingScore;  // 타이밍 점수
    public int backGroundScore; // 배경 점수
    public int totalScore;
    public int starCount;
    public Timestamp createdAt;
}