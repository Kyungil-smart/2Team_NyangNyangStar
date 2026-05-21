using UnityEngine;
using UnityEngine.EventSystems;

public class BaseScene : MonoBehaviour
{
    void Awake() => Init();

    protected virtual void Init()
    {
        Object obj = FindFirstObjectByType(typeof(EventSystem));

        if (obj == null)
        {
            // TODO : 이벤트 시스템을 어드레서블로 불러오기
            // GameObject prefab = GameManager.Resource.Instantiate("EventSystem");
            // DontDestroyOnLoad(prefab);
        }
    }
}