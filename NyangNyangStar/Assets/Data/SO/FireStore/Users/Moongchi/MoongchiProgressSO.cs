using Data.ScriptableObjects.ScratchingTimeSO;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine;

[FirestorePath("Users/{userId}/Moongchi/{docId}")]
[CreateAssetMenu(fileName = "MoongchiProgressSO", menuName = "ScriptableObjects/MoongchiProgressSO")]
public class MoongchiProgressSO : BaseFireStore
{
    [SerializeField] private int _level = 1;
    [SerializeField] private int _currentExp;

    public int Level => _level;
    public int CurrentExp => _currentExp;

    public override async Task CreateNew(FirebaseFirestore database, string userId)
    {
        ResetToDefault();
        await base.CreateNew(database, userId);
    }

    public void ApplyTo(MoongchiStatSo moongchiStat)
    {
        if (moongchiStat == null)
            return;

        moongchiStat.SetProgressData(_level, _currentExp);
    }

    public void CaptureFrom(MoongchiStatSo moongchiStat)
    {
        if (moongchiStat == null)
            return;

        _level = moongchiStat.Level;
        _currentExp = moongchiStat.GetCurrentExp();
    }

    private void ResetToDefault()
    {
        _level = 1;
        _currentExp = 0;
    }
}
