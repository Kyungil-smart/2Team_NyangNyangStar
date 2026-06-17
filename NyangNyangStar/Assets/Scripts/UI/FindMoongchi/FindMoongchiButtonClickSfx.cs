using Core.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.FindMoongchi
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class FindMoongchiButtonClickSfx : MonoBehaviour
    {
        private const string DefaultSfxKey = "Main_SFX_Touch";

        [SerializeField] private string _sfxKey = DefaultSfxKey;

        private Button _button;
        private bool _bound;

        public void SetSfxKey(string sfxKey)
        {
            _sfxKey = string.IsNullOrWhiteSpace(sfxKey) ? DefaultSfxKey : sfxKey;
        }

        private void Awake()
        {
            ResolveButton();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void ResolveButton()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }

        private void Bind()
        {
            ResolveButton();

            if (_button == null || _bound)
                return;

            _button.onClick.AddListener(PlayClickSfx);
            _bound = true;
        }

        private void Unbind()
        {
            if (_button == null || !_bound)
                return;

            _button.onClick.RemoveListener(PlayClickSfx);
            _bound = false;
        }

        private void PlayClickSfx()
        {
            if (string.IsNullOrWhiteSpace(_sfxKey))
                return;

            GameManager.Audio.PlaySfx(_sfxKey);
        }
    }
}
