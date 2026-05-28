using Core.Managers;
using UnityEngine;

public class GameScene : BaseScene
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            GameManager.Data.LoadSheets();
        }
    }
}
