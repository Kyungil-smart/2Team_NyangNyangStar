using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneMoveButton : MonoBehaviour
{
    private void Start()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                continue;
            }

            string sceneName = SceneNameFromLabel(label.text);
            if (string.IsNullOrEmpty(sceneName))
            {
                continue;
            }

            button.onClick.AddListener(() => MoveScene(sceneName));
        }
    }

    public void MoveScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private string SceneNameFromLabel(string label)
    {
        return label switch
        {
            "A Button" => "A Scene",
            "B Button" => "B Scene 1",
            "C Button" => "C Scene 2",
            _ => string.Empty
        };
    }
}
