using Core.Managers;

namespace UI
{
    public class UIPopup : UIBase
    {
        public string AddressableKey { get; private set; }

        public void SetAddressableKey(string key)
            => AddressableKey = key;
        public override void Init() { }
        
        public virtual void ClosePopup()
        {
            GameManager.UI.ClosePopupUI();  
        }
    }
}