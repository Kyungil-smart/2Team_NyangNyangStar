using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
[CreateAssetMenu(fileName = "ResourcesSO", menuName = "ScriptableObjects/ResourcesSO")]

public class ResourcesSO : BaseFireStore
{
    private string userId;
    [SerializeField] private int energy = 0;
    [SerializeField] private int coin = 0;
    [SerializeField] private int jewel = 0;

    public override void InitDataBase(FirebaseFirestore database)
    {
        this.db = database;
    }

    public override void InitDataBase(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.userId = userId;
        Debug.Log($"ResourcesSO initialized with userId: {userId}", this);
    }

    public override Task DeleteDataAsync()
    {
        return null;
    }

    public override async Task<DocumentSnapshot> GetSnapshotAsync()
    {
        var data = await db.Collection("Users").Document(userId).Collection("Resources").Document("Resources").GetSnapshotAsync();
        energy = data.GetValue<int>("Energy");
        coin = data.GetValue<int>("Coin");
        jewel = data.GetValue<int>("Jewel");
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
            { "Energy", energy },
            { "Coin", coin },
            { "Jewel", jewel }
        };
        await db.Collection("Users").Document(userId).Collection("Resources").Document("Resources").UpdateAsync(updates);
        //throw new System.NotImplementedException();
    }
    public override Task<DocumentSnapshot> GetSnapshotAsync<T>()
    {
        return null;
    }
}
