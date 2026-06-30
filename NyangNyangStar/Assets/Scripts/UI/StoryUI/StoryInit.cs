using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryInit : MonoBehaviour
{
    [SerializeField] private StoryDataSO _story;

    void Start()
    {
        StoryUIController.ShowOnce(_story);
    }


}
