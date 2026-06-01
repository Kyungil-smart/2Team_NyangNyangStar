using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyingDataTEST : MonoBehaviour
{
    [SerializeField] ResourcesSO resourcesSO;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    public async void CoinUp()
    {
        await resourcesSO.UpdateFromServerAsync(false);
        resourcesSO.coin += 10;
        await resourcesSO.UpdateDataAsync();
    }
}
