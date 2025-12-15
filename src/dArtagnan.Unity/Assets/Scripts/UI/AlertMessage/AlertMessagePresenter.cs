using R3;

namespace UI.AlertMessage
{
    public static class AlertMessagePresenter
    {
        public static void Initialize(AlertMessageModel model, AlertMessageView view)
        {
            model.message.Subscribe(msg => view.messageText.text = msg);
            model.color.Subscribe(color =>
            {
                view.messageText.color = color;
                view.decoImage.ForEach(deco => deco.color = color);
            });
            model.showMsg.Subscribe(view.gameObject.SetActive);
        }
    }
}