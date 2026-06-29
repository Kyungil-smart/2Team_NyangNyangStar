using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "StoryDataSO", menuName = "ScriptableObjects/StoryDataSO")]
public class StoryDataSO : ScriptableObject
{

    public int storyId;


    public string title;


    public List<DialogueCard> cards = new List<DialogueCard>();
}
