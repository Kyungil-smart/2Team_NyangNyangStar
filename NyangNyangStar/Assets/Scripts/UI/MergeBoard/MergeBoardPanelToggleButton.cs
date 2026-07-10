using UnityEngine;

namespace UI.MergeBoard
{
    public sealed class MergeBoardPanelToggleButton : MonoBehaviour
    {
        [Header("대상 패널")]
        [SerializeField] private GameObject _targetPanel;

        public void ToggleTarget()
        {
            if (_targetPanel == null)
                return;

            _targetPanel.SetActive(!_targetPanel.activeSelf);
        }

        public void ShowTarget()
        {
            if (_targetPanel != null)
                _targetPanel.SetActive(true);
        }

        public void HideTarget()
        {
            if (_targetPanel != null)
                _targetPanel.SetActive(false);
        }
    }
}
