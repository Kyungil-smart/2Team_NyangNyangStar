using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[FirestorePath("Users/{userId}")]
[CreateAssetMenu(fileName = "UsersSO", menuName = "ScriptableObjects/UsersSO")]
public class UsersSO : BaseFireStore
{
    [FirestoreField("UserID")] [SerializeField] private string userId;
    [FirestoreField("Name")]   [SerializeField] private string nickname;

    [SerializeField] public int userLevel = 1;
    [SerializeField] public int userExp = 0;
    [SerializeField] public int catLikeLevel = 1;
    [SerializeField] public int catLikeExp = 0;

    [SerializeField] public int uploadPost = 0;
    [SerializeField] public int followerNumber = 0; 
    [SerializeField] public int followingNumber = 0;
    [SerializeField] public int lastLogin = 0;
    [SerializeField] public int lastLogout = 0;


    // subCollections 는 이제 BaseFireStore 가 기본 제공 (기존 에셋 데이터는 이름 매칭으로 유지됨)


    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        this.userId = userId;
        nickname = "New User";
        await base.CreateNew(database, userId);
    }
}
