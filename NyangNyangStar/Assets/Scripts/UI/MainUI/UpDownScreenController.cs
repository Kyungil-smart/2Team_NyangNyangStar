using Core.Managers;
using DG.Tweening;
using UI.MergeBoard;
using UnityEngine;

namespace UI.MainUI
{
    public class UpDownScreenController : MonoBehaviour
    {
        [SerializeField] private MergeBoardController _mergeBoardCon;
        [SerializeField] private RectTransform _rectTransfom;
        [SerializeField] private Canvas _upDownScreenCanvas;
        
        private Canvas _mainUICanvas;

        private void Awake()
        {
            _upDownScreenCanvas = GetComponentInParent<Canvas>();
            _rectTransfom = GetComponent<RectTransform>();
        }

        private void Start()
        {
            EnterGameScene();
            _upDownScreenCanvas.overrideSorting = true;
            _upDownScreenCanvas.sortingOrder = 3;
        }

        public void DownAnimation(Canvas mainCanvas = null)
        {
            if(mainCanvas != null)
                _mainUICanvas = mainCanvas;
            
            _rectTransfom.anchoredPosition = new Vector2(0, 1920f);
            _rectTransfom.DOAnchorPos(Vector2.zero, 0.5f).
                SetEase(Ease.OutQuad).OnComplete(UpAnimation);
        }

        private void UpAnimation()
        {
            if(_mergeBoardCon == null)
                _mergeBoardCon = FindObjectOfType<MergeBoardController>();
                
            _mergeBoardCon.IsOpen = !_mergeBoardCon.IsOpen;
            
            if(_mergeBoardCon.IsOpen)
                _mainUICanvas.sortingOrder = 0;
            else
                _mainUICanvas.sortingOrder = 2;
            
            _rectTransfom.DOAnchorPos(new Vector2(0, 1920f), 0.15f).SetDelay(0.3f).SetEase(Ease.OutSine);
        }

        public void EnterGameScene()
        {
            _rectTransfom.anchoredPosition = new Vector2(0, 0);
            _rectTransfom.DOAnchorPos(new Vector2(0, 1920f), 0.25f).SetDelay(0.5f).SetEase(Ease.OutSine);
        }

        public void ExitGameScene()
        {
            _rectTransfom.anchoredPosition = new Vector2(0, 1920f);
            _rectTransfom.DOAnchorPos(Vector2.zero, 0.5f).
                SetEase(Ease.OutQuad).OnComplete(() => GameManager.Scene.LoadPreviousScene());
        }
    }
}