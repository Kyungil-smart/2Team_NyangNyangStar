using Core.Managers;
using UnityEngine;

public class GameScene : BaseScene
{
    private void Start()
    {
        GameManager.Data.LoadSheets();
    }
}
