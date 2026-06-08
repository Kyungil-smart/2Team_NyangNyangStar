using Core.Managers;
using UnityEngine;

public class UIButtonClickSfx : MonoBehaviour
{
    public void Play()
    {
        GameManager.Audio.PlaySfx("Main_SFX_Touch");
    }
}