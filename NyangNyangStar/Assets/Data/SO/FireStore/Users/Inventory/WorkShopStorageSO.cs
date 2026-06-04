

using UnityEngine;

[FirestorePath("Users/{userId}/Inventory/{docId}")]
[CreateAssetMenu(fileName = "WorkShopStorageSO", menuName = "ScriptableObjects/WorkShopStorageSO")]
public class WorkShopStorageSO : BaseFireStore
{

    [SerializeField] public int amount = 0;    
    [SerializeField] public int maxAmount = 100;  
    [FirestoreIgnore][SerializeField] private int cachedValue;  

       
}
