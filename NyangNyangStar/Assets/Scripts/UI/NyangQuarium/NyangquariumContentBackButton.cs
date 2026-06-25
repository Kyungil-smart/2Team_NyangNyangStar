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
            _button ??= GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                return;

            _button.onClick.RemoveListener(ReturnToHub);
            _button.onClick.AddListener(ReturnToHub);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(ReturnToHub);
        }

        public void ReturnToHub()
        {
            if (NyangquariumHubUI.Active != null)
            {
                NyangquariumHubUI.Active.ReturnToHub();
                return;
            }

            GameManager.UI.ClosePopupUI();
        }
    }
}
