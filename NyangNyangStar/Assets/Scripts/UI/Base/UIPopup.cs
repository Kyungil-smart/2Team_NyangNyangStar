using Core.Managers;

namespace UI.Base
{
    public enum UIPopupCloseMode
    {
        Auto,
        Release,
        Hide
    }

    public class UIPopup : UIBase
    {
        public string AddressableKey { get; private set; }
        public UIPopupCloseMode CloseMode { get; private set; } = UIPopupCloseMode.Release;

        public void SetAddressableKey(string key)
            => AddressableKey = key;

        public void SetCloseMode(UIPopupCloseMode closeMode)
        {
            CloseMode = closeMode == UIPopupCloseMode.Auto
                ? UIPopupCloseMode.Release
                : closeMode;
        }

        public override void Init() { }

        public virtual void PlayOpenAnimation() { }

        public virtual void ClosePopup()
        {
            if (CloseMode == UIPopupCloseMode.Hide)
            {
                gameObject.SetActive(false);
                return;
            }

            GameManager.UI.ClosePopupUI(this);
        }
    }
}
