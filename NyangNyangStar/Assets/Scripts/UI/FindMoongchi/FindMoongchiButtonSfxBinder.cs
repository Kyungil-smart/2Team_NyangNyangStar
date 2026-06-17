using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    [DisallowMultipleComponent]
    public sealed class FindMoongchiButtonSfxBinder : MonoBehaviour
    {
        private const string DefaultSfxKey = "Main_SFX_Touch";

        [SerializeField] private bool _bindOnEnable = true;
        [SerializeField] private bool _includeInactive = true;
        [SerializeField] private string _sfxKey = DefaultSfxKey;

        private string SfxKey => string.IsNullOrWhiteSpace(_sfxKey) ? DefaultSfxKey : _sfxKey;

        private void OnEnable()
        {
            if (_bindOnEnable)
                BindAllButtons();
        }

        public void BindAllButtons()
        {
            Button[] buttons = GetComponentsInChildren<Button>(_includeInactive);
            int addedCount = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button == null)
                    continue;

                FindMoongchiButtonClickSfx clickSfx = button.GetComponent<FindMoongchiButtonClickSfx>();

                if (clickSfx == null)
                {
                    clickSfx = button.gameObject.AddComponent<FindMoongchiButtonClickSfx>();
                    addedCount++;
                }

                clickSfx.SetSfxKey(SfxKey);
            }

            if (addedCount > 0)
            {
                DebugTool.Log(
                    $"[FindMoongchiButtonSfxBinder] 버튼 효과음 바인딩 완료: 전체={buttons.Length}, 신규={addedCount}, SfxKey={SfxKey}",
                    DebugType.FindMoongchi,
                    this);
            }
        }
    }
}
