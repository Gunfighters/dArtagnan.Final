using R3;

namespace UI.HUD.PopupOnDeath
{
    public static class PopupOnDeathPresenter
    {
        public static void Initialize(PopupOnDeathModel model, PopupOnDeathView view)
        {
            model.mainText.SubscribeToText(view.mainText);
            model.balanceText.SubscribeToText(view.balanceText);
            model.revivalText.SubscribeToText(view.revivalText);
            model.isBankrupt.Subscribe(yes => view.containerImage.sprite = yes ? view.bankruptDeath : view.normalDeath);
        }
    }
}