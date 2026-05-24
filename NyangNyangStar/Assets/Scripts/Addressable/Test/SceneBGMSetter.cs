using Core.Managers;
using UnityEngine;

public class SceneBGMSetter : MonoBehaviour
{
    [SerializeField] private string _bgmKey;

    private void Start()
    {
        GameManager.Audio.PlayBgm(_bgmKey);
    }
}
