using Firebase.Firestore;
using System.Threading.Tasks;

public class FirestoreRequestContext
{
    private readonly BaseFireStore _targetStore;

    public FirestoreRequestContext(BaseFireStore targetStore)
    {
        _targetStore = targetStore;
    }


    public async Task SetAsync(object data)
    {

        await _targetStore.SetDataAsync(data);
    }


    public async Task<DocumentSnapshot> GetAsync<T>()
    {
        return await _targetStore.UpdateFromServerAsync<T>();
    }

    public async Task UpdateAsync(object data)
    {
        await _targetStore.UpdateDataAsync();
    }

    public async Task SaveAllToServerAsync()
    {
        await _targetStore.SaveAllToServerAsync();
    }

    public async Task DeleteAsync()
    {
        await _targetStore.DeleteDataAsync();
    }
}