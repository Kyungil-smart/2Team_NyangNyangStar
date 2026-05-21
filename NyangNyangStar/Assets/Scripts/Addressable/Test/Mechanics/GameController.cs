using Platformer.Core;
using Platformer.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace Platformer.Mechanics
{
    /// <summary>
    /// This class exposes the the game model in the inspector, and ticks the
    /// simulation.
    /// </summary> 
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        //This model field is public and can be therefore be modified in the 
        //inspector.
        //The reference actually comes from the InstanceRegister, and is shared
        //through the simulation and events. Unity will deserialize over this
        //shared reference when the scene loads, allowing the model to be
        //conveniently configured inside the inspector.
        public PlatformerModel model = Simulation.GetModel<PlatformerModel>();
        public enum PlayerSelection
        {
            Player1,
            Player2
        }

        public PlayerSelection playerSelection = PlayerSelection.Player1;
        void OnEnable()
        {
            Instance = this;
            if(model != null && model.player == null)
            {
                var tmpobj = Addressables.LoadAssetAsync<GameObject>(playerSelection.ToString()).WaitForCompletion();
                var tmpPlayer = GameObject.Instantiate(tmpobj, Vector3.zero, Quaternion.identity);
                model.player = tmpPlayer.GetComponent<PlayerController>();
                // model.virtualCamera.Follow = model.player.transform;

            }
            
                
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Instance == this) Simulation.Tick();
        }
    }
}