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

            bool nextActive = !_targetPanel.activeSelf;
            _targetPanel.SetActive(nextActive);

            if (nextActive)
                BringTargetToFront();
        }

        public void ShowTarget()
        {
            if (_targetPanel == null)
                return;

            _targetPanel.SetActive(true);
            BringTargetToFront();
        }

        public void HideTarget()
        {
            if (_targetPanel != null)
                _targetPanel.SetActive(false);
        }

        private void BringTargetToFront()
        {
            if (_targetPanel != null)
                _targetPanel.transform.SetAsLastSibling();
        }
    }
}
