using Core.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Login
{
    public class LogInUIController : MonoBehaviour
    {
        [SerializeField] private Image _logoImage;
        [SerializeField] private Image _upDownImage;

        private void Awake()
        {
            if(_logoImage != null)
                _logoImage.SetNativeSize();
        }

        public void DownImage()
        {
            if (_upDownImage == null)
                return;
            
            RectTransform rectTransform = _upDownImage.GetComponent<RectTransform>();
            
            rectTransform.anchoredPosition = new Vector2(0, 1920f);
            rectTransform.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutQuad)
                .OnComplete(GameManager.Scene.LoadNextScene);
        }
    }
}