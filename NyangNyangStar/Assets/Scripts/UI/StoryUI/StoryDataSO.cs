using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "StoryDataSO", menuName = "ScriptableObjects/StoryDataSO")]
public class StoryDataSO : ScriptableObject
{

    public int storyId;


    public string title;


    [Tooltip("챕터 배경 Addressable 키 (비우면 배경 변경 안 함)")]
    public string backgroundKey;


    public List<DialogueCard> cards = new List<DialogueCard>();
}
