using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class PlayerResourceDebugButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private PlayerResourceType _resourceType = PlayerResourceType.Energy;
        [SerializeField] private int _amount = 100;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                return;

            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        public async void HandleClicked()
        {
            int safeAmount = Mathf.Max(0, _amount);

            if (safeAmount <= 0)
                return;

            bool added = await PlayerResourceManager.Instance.AddAsync(_resourceType, safeAmount);

            if (!added)
                DebugTool.Warning($"[PlayerResourceDebugButton] 자원 추가 실패: Type={_resourceType}, Amount={safeAmount}", DebugType.UI, this);
        }
    }
}
