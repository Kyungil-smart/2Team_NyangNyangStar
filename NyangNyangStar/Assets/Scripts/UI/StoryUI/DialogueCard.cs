using UnityEngine;

/// <summary>
/// 대사 카드 한 줄의 데이터 (데이터시트의 한 행에 대응).
/// StoryDataSO·StoryUIController 등 여러 곳에서 공용으로 쓴다.
/// </summary>
[System.Serializable]
public struct DialogueCard
{
    public string ID;            // 예: S2_001 (문자열이라 string)
    public string char_name;     // 표시 이름
    public string frame;         // 포트레잇 프레임 키
    public string char_portrait; // 초상화 키
    [TextArea] public string dialogue; // 대사 (여러 줄 입력 가능)
    public string card_bg;       // 카드 배경 키
    public bool end;             // 중단점 (true면 이 카드 출력 후 멈춤)
}
