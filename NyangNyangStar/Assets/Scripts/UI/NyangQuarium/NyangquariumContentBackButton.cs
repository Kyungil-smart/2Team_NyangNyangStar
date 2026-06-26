using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium
{
    [RequireComponent(typeof(Button))]
    public sealed class NyangquariumContentBackButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                return;

            _button.onClick.RemoveListener(ReturnToMain);
            _button.onClick.AddListener(ReturnToMain);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(ReturnToMain);
        }

        public void ReturnToMain()
        {
            if (NyangquariumMainUIManager.Active != null)
            {
                NyangquariumMainUIManager.Active.ReturnToMain();
                return;
            }

            GameManager.UI.ClosePopupUI();
        }
    }
}
