using Core.Managers;
using UnityEngine;

public class GameScene : BaseScene
{
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("D");
            GameManager.Data.LoadSheets();
        }
    }
}
