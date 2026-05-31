using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
[CreateAssetMenu(fileName = "ResourcesSO", menuName = "ScriptableObjects/ResourcesSO")]

public class ResourcesSO : BaseFireStore
{
    private string userId;
    [SerializeField] public int energy = 200;
    [SerializeField] public int coin = 100;
    [SerializeField] public int jewel = 100;

    [SerializeField] public int usedEnergy = 0;
    [SerializeField] public int totalGottenEnergy = 0;
    [SerializeField] public int totalGottenCoin = 0;
    [SerializeField] public int totalGottenJewel = 0;

    [SerializeField] public int cumulativeMerge = 0;

    public override async Task CreateNew(FirebaseFirestore database)
    {
        this.db = database;

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "Energy", energy },
            { "Coin", coin },
            { "Jewel", jewel }
        };
        await SetDataAsync(data);
    }

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        this.db = database;
        this.userId = userId;


        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "Energy", 200 },
            { "Coin", 100 },
            { "Jewel", 100 }
        };
        await SetDataAsync(data);
    }

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

    public override async Task<DocumentSnapshot> UpdateFromServerAsync<T>()
    {
        var data = await db.Collection("Users").Document(userId).Collection("Resources").Document(DocumentId).GetSnapshotAsync();
        energy = data.GetValue<int>("Energy");
        coin = data.GetValue<int>("Coin");
        jewel = data.GetValue<int>("Jewel");
        return data;
    }



    public override async Task SetDataAsync(object data)
    {
        await db.Collection("Users").Document(userId).Collection("Resources").Document(DocumentId).SetAsync(data);

    }

    public override async Task UpdateDataAsync()
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Energy", energy },
            { "Coin", coin },
            { "Jewel", jewel }
        };
        await db.Collection("Users").Document(userId).Collection("Resources").Document(DocumentId).UpdateAsync(updates);
        //throw new System.NotImplementedException();
    }

    

    public override async Task<DocumentSnapshot> UpdateFromServerAsync(bool updateAll)
    {
        var data = await db.Collection("Users").Document(userId).Collection("Resources").Document(DocumentId).GetSnapshotAsync();
        energy = data.GetValue<int>("Energy");
        coin = data.GetValue<int>("Coin");
        jewel = data.GetValue<int>("Jewel");
        return data;
    }

}
