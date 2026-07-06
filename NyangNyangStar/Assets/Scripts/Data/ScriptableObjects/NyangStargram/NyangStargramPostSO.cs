using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[FirestorePath("Users/{userId}/NyangStargram/{docId}")]
[CreateAssetMenu(fileName = "NyangStargramPostSO", menuName = "ScriptableObjects/NyangStargramPostSO")]
public class NyangStargramPostSO : BaseFireStore
{
    [FirestoreMap]
    [SerializeField] private List<NyangStargramPostData> posts = new();

    public IReadOnlyList<NyangStargramPostData> Posts => posts;

    public void AddPost(NyangStargramPostData post)
    {
        posts.Add(post);
    }

    public void RemovePost(string photoId)
    {
        posts.RemoveAll(x => x.photoId == photoId);
    }

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        posts = new List<NyangStargramPostData>();

        await base.CreateNew(database, userId);
    }
}

[Serializable]
public struct NyangStargramPostData
{
    [FirestoreMapKey] public string postId;

    public string photoId;
    public Timestamp createdAt;
}