using Core.Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SceneChangeController : MonoBehaviour
{
    public static SceneChangeController Instance { get; private set; }
    
    [Space(3)] [Header("화면 전환 이미지 컴포넌트")] [SerializeField]
    private Image _changeSceneImage;

    [Space(3)] [Header("씬 진입 전환 시간")]
    [SerializeField] [Range(0.1f, 10.0f)]
    private float _enterDuration = 1f;

    [Space(3)] [Header("씬 전환 시간")] [SerializeField] [Range(0.1f, 10.0f)]
    private float _exitDuration = 2f;

    [Space(3)] [Header("이미지 색상")] [SerializeField]
    private Color color;

    public UnityAction OnChangeScene;

    private void Awake()
    {
        SingletonInit();
        if (_changeSceneImage == null)
            DebugTool.Log("이미지 컴포넌트를 등록해야 합니다.", DebugType.Missing);
    }

    private void OnEnable()
    {
        _changeSceneImage.color = new Color(color.r, color.g, color.b, 1f);
    }

    public void OnEnterScene()
        => StartCoroutine(EnterScene());

    public void OnExitScene(bool nextScene = false)
        => StartCoroutine(ExitScene(nextScene));

    private IEnumerator EnterScene()
    {
        SetActivateImage(true);
        DebugTool.Log("씬 전환 (Enter)", DebugType.Game);
        float time = 0f;

        while (time < _enterDuration)
        {
            time += Time.deltaTime;
            float t = time / _enterDuration;

            color.a = Mathf.Lerp(1f, 0f, t);

            _changeSceneImage.color = color;

            yield return null;
        }

        _changeSceneImage.gameObject.SetActive(false);
        
        OnChangeScene?.Invoke();
    }

    private IEnumerator ExitScene(bool nextScene)
    {
        DebugTool.Log("현재 전환 (Out) ", DebugType.Game);
        
        _changeSceneImage.gameObject.SetActive(true);
        
        float time = 0f;
        
        while (time < _exitDuration)
        {
            time += Time.deltaTime;
            float t = time / _exitDuration;

            color.a = Mathf.Lerp(0f, 1f, t);

            _changeSceneImage.color = color;

            yield return null;
        }
        
        if(nextScene)
            GameManager.Scene.LoadNextStage();
    }

    public void SetActivateImage(bool value)
    {
        _changeSceneImage.gameObject.SetActive(value);
    }
    
    private void SingletonInit()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
