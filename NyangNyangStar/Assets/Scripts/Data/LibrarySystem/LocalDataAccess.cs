using UnityEngine;


public class LocalDataAccess : MonoBehaviour
{
    public static LocalDataAccess Instance { get; private set; }


    public GameDataModule Game { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DebugTool.Warning(
                "[LocalDataAccess] 중복 인스턴스 감지 - 새 인스턴스 파괴",
                DebugType.Data, this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Game = new GameDataModule();

        DebugTool.Log("[LocalDataAccess] 초기화 완료", DebugType.Data, this);
    }

    private void OnDestroy()
    {
        Instance.Game.ClearEvent(); 
        if (Instance == this) Instance = null;
    }
}
