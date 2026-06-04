using UnityEngine;
using UnityEngine.EventSystems;


// TouchArea 첫 터치를 감지해 TOUCH! 안내를 숨기고 흥미도 감소를 시작합니다.
// ScratchingDailyStageUI / ScratchingWeekStageUI 공통 사용.

public class ScratchingStageStartHandler : MonoBehaviour, IPointerDownHandler
{
    // 흥미도 감소를 제어하는 컨트롤러입니다.
    [SerializeField] private ScratchingInterestController _interestController;

    // 첫 터치 전까지 표시되는 TOUCH! 안내
    [SerializeField] private GameObject _touchGuideText;

    // 스테이지 시작 처리가 이미 실행되었는지 확인
    private bool _hasStarted;

    private void Awake()
    {
        // Inspector에 연결
        CacheReferences();
    }

    private void OnEnable()
    {
        // UI가 다시 활성화될 때마다 첫 터치 상태로 초기화
        _hasStarted = false;
        ShowTouchGuide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 첫 터치에 스테이지 시작
        if (_hasStarted)
            return;

        _hasStarted = true;

        HideTouchGuide();

        _interestController?.StartInterestDrain();
    }

    private void ShowTouchGuide()
    {
        if (_touchGuideText == null)
            return;

        TouchBlinkAnim blinkAnim = _touchGuideText.GetComponent<TouchBlinkAnim>();

        if (!_touchGuideText.activeSelf)
        {
            _touchGuideText.SetActive(true);
            return;
        }

        blinkAnim?.StartBlink();
    }

    private void HideTouchGuide()
    {
        if (_touchGuideText == null)
            return;

        _touchGuideText.GetComponent<TouchBlinkAnim>()?.StopBlink();
        _touchGuideText.SetActive(false);
    }

    private void CacheReferences()
    {
        
        Transform current = transform;
        while (current != null)
        {
            if (_interestController == null)
                _interestController = current.GetComponentInChildren<ScratchingInterestController>(true);

            if (_touchGuideText == null)
            {
                // TOUCH! 안내 텍스트는 이름으로 찾아 연결
                foreach (Transform child in current.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != "TouchGuideText")
                        continue;

                    _touchGuideText = child.gameObject;
                    break;
                }
            }

            if (_interestController != null && _touchGuideText != null)
                return;

            // 부모 계층에서 알아서 찾게 
            current = current.parent;
        }
    }
}
