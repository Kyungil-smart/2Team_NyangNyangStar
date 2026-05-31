using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "UsersSO", menuName = "ScriptableObjects/UsersSO")]

public class UsersSO : BaseFireStore
{
    [SerializeField] private string userId;
    [SerializeField] private string nickname;
    [SerializeField] private ResourcesSO resourcesSO;
    public ResourcesSO ResourcesSO => resourcesSO;

    public override void InitDataBase(FirebaseFirestore database)
    {
        this.db = database;
    }

    public override void InitDataBase(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.userId = userId;
        if (resourcesSO != null)
        {
            resourcesSO.InitDataBase(database, userId);
            Debug.Log($"UserInfoSO initialized with userId: {userId}", this);
        }
         else
        {
            Debug.LogError("ResourcesSO reference is not set in UserInfoSO!", this);
        }
    }

    public override Task DeleteDataAsync()
    {
        return null;
    }

    public override async Task<DocumentSnapshot> GetSnapshotAsync()
    {
        var data = await db.Collection("Users").Document(userId).GetSnapshotAsync();
        nickname = data.GetValue<string>("Name");
        userId = data.GetValue<string>("UserID");

        await resourcesSO.GetSnapshotAsync();
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

    public override async Task UpdateSubToServerAsync(string docName)
    {
        if (docName == "Resources")
        {
            await resourcesSO.UpdateDataAsync();
        }
        else
        {
            Debug.LogError($"Unknown document name for update: {docName}", this);
            return;
        }



    }
    public override Task<DocumentSnapshot> GetSnapshotAsync<T>()
    {
        return null;
    }
}
