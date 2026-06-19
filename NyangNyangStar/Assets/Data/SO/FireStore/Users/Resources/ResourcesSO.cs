using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

[FirestorePath("Users/{userId}/Resources/{docId}")]
[CreateAssetMenu(fileName = "ResourcesSO", menuName = "ScriptableObjects/ResourcesSO")]
public class ResourcesSO : BaseFireStore
{
    private const int DefaultEnergy = 200;
    private const int DefaultCoin = 100;
    private const int DefaultJewel = 100;

    [SerializeField] public int energy = DefaultEnergy;
    [SerializeField] public int coin = DefaultCoin;
    [SerializeField] public int jewel = DefaultJewel;

    [SerializeField] public int usedEnergy = 0;
    [SerializeField] public int totalGottenEnergy = 0;
    [SerializeField] public int totalGottenCoin = 0;
    [SerializeField] public int totalGottenJewel = 0;

    [SerializeField] public int cumulativeMerge = 0;

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        ResetToDefault();
        await base.CreateNew(database, userId);
    }

    private void ResetToDefault()
    {
        energy = DefaultEnergy;
        coin = DefaultCoin;
        jewel = DefaultJewel;

        usedEnergy = 0;
        totalGottenEnergy = 0;
        totalGottenCoin = 0;
        totalGottenJewel = 0;
        cumulativeMerge = 0;
    }
}
