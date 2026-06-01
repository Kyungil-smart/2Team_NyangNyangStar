using UnityEngine;

[FirestorePath("Users/{userId}/Resources/{docId}")]
[CreateAssetMenu(fileName = "ResourcesSO", menuName = "ScriptableObjects/ResourcesSO")]
public class ResourcesSO : BaseFireStore
{
    [SerializeField] public int energy = 200;
    [SerializeField] public int coin = 100;
    [SerializeField] public int jewel = 100;

    [SerializeField] public int usedEnergy = 0;
    [SerializeField] public int totalGottenEnergy = 0;
    [SerializeField] public int totalGottenCoin = 0;
    [SerializeField] public int totalGottenJewel = 0;

    [SerializeField] public int cumulativeMerge = 0;
}
