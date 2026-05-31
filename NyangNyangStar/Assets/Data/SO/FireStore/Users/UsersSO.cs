using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "UsersSO", menuName = "ScriptableObjects/UsersSO")]

public class UsersSO : BaseFireStore
{

    [SerializeField] private string userId;
    [SerializeField] private string nickname;

    [SerializeField] public int userLevel = 1;
    [SerializeField] public int userExp = 0;
    [SerializeField] public int catLikeLevel = 1;
    [SerializeField] public int catLikeExp = 0;

    [SerializeField] public int uploadPost = 0;
    [SerializeField] public int follwerNumber = 0;
    [SerializeField] public int followingNumber = 0;
    [SerializeField] public int lastLogin = 0;
    [SerializeField] public int lastLogout = 0;


    [SerializeField] private List<BaseFireStore> subCollections;


    public override Task CreateNew(FirebaseFirestore database)
    {
        throw new System.NotImplementedException();
    }
    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.userId = userId;
        this.nickname = "New User";


        foreach (var sub in subCollections)
        {
            await sub.CreateNew(database, userId);
        }

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "UserID", userId },
            { "Name", nickname },
            { "UserLevel", 1 },
            { "UserExp", 0 },
            { "CatLikeLevel", 1 },
            { "CatLikeExp", 0 },
            { "UploadPost", 0 },
            { "FollowerNumber", 0 },
            { "FollowingNumber", 0 },
            { "LastLogin", 0 },
            { "LastLogout", 0 }

        };

        await SetDataAsync(updates);


    }
    public override void InitDataBase(FirebaseFirestore database)
    {
        this.db = database;
    }

    public override void InitDataBase(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.userId = userId;
        foreach (var sub in subCollections)
        {
           sub.InitDataBase(database, userId);
        }

    }

    public override Task DeleteDataAsync()
    {
        return null;
    }

    public override async Task<DocumentSnapshot> UpdateFromServerAsync(bool updateAll)
    {
        var data = await db.Collection("Users").Document(userId).GetSnapshotAsync();
        nickname = data.GetValue<string>("Name");
        userId = data.GetValue<string>("UserID");

        if(updateAll)
        {
            foreach (var sub in subCollections)
            {
                await sub.UpdateFromServerAsync(true);
            }
        }
        return data;
    }



    public override async Task SetDataAsync(object data)
    {
        await db.Collection("Users").Document(userId).SetAsync(data);

    }

    public override async Task UpdateDataAsync()
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", nickname },
            { "UserID", userId }
        };
        await db.Collection("Users").Document(userId).UpdateAsync(updates);

    }

    public override async Task UpdateSubToServerAsync<T>()
    {
        bool found = false;
        foreach (var sub in subCollections.OfType<T>())
        {
            await sub.UpdateDataAsync();
            found = true;
        }
        if (!found)
            Debug.LogError($"No sub-collection of type {typeof(T).Name} found.", this);
    }

    public override async Task UpdateSubToServerAsync<T>(string documentId)
    {
        bool found = false;
        foreach (var sub in subCollections.OfType<T>().Where(s => s.DocumentId == documentId))
        {
            await sub.UpdateDataAsync();
            found = true;
        }
        if (!found)
            Debug.LogError($"No '{typeof(T).Name}' with id '{documentId}'.", this);
    }


    public override Task<DocumentSnapshot> UpdateFromServerAsync<T>()
    {
        return null;
    }

}
