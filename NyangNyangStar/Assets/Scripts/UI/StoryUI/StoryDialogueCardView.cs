using TMPro;
using UnityEngine;

/// <summary>
/// 대사 카드 한 장의 표시 담당.
/// 카드 프리팹에 붙이고, 인스펙터에서 이름/대사 TMP를 연결한다.
/// (나중에 초상화·배경 Image 슬롯도 여기 추가하면 됨)
/// </summary>
public class StoryDialogueCardView : MonoBehaviour
{
    [Header("텍스트 (인스펙터에서 연결)")]
    [Tooltip("캐릭터 이름 TMP")]
    [SerializeField] private TMP_Text _nameText;

    [Tooltip("대사 TMP")]
    [SerializeField] private TMP_Text _dialogueText;

    /// <summary>이 카드에 이름과 대사를 채운다.</summary>
    public void Setup(string charName, string dialogue)
    {
        if (_nameText != null)
            _nameText.text = charName;

        if (_dialogueText != null)
            _dialogueText.text = dialogue;
    }
}
