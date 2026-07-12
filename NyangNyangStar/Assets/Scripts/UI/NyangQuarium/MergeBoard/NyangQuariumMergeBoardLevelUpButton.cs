using UI.NyangQuarium.Quest;
using UnityEngine;
using UnityEngine.UI;

namespace UI.NyangQuarium.MergeBoard
{
    public sealed class NyangQuariumMergeBoardLevelUpButton : MonoBehaviour
    {
        private Button _button;
        private bool _isLevelingUp;

        public void Init()
        {
            Button button = GetComponent<Button>();
            if (button == null)
            {
                DebugTool.Warning(
                    "[NyangQuariumMergeBoardLevelUpButton] Button 컴포넌트를 찾을 수 없습니다",
                    DebugType.UI,
                    this);
                return;
            }

            if (_button == button)
                return;

            Unbind();

            _button = button;
            _button.onClick.AddListener(OnClicked);
        }

        private async void OnClicked()
        {
            if (_isLevelingUp)
                return;

            _isLevelingUp = true;

            try
            {
                NyangQuariumFirestoreSO firestoreSO =
                    await NyangQuariumFirestoreSO.WaitForReadyAsync();

                if (firestoreSO == null)
                {
                    DebugTool.Warning(
                        "[NyangQuariumMergeBoardLevelUpButton] NyangQuariumFirestoreSO 준비가 완료되지 않았습니다",
                        DebugType.Data,
                        this);
                    return;
                }

                NyangQuariumAquariumLevelSO aquariumLevelSO =
                    NyangQuariumQuestSOLocator.ResolveAquariumLevelSO();

                bool saved = await firestoreSO.AddAquariumLevelAsync(aquariumLevelSO);

                if (!saved)
                {
                    DebugTool.Warning(
                        "[NyangQuariumMergeBoardLevelUpButton] 수조 레벨업에 실패했습니다",
                        DebugType.Data,
                        this);
                }
            }
            finally
            {
                _isLevelingUp = false;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }
    }
}
