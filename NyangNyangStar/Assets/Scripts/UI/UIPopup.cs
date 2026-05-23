using Core.Managers;

namespace UI
{
    public class UIPopup : UIBase
    {
        public override void Init() { }
        
        public virtual void ClosePopup()
        {
            GameManager.UI.ClosePopupUI();  
        }
    }
}