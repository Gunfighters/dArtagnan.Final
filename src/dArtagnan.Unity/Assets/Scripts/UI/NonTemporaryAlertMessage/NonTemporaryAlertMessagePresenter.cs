using R3;

namespace UI.NonTemporaryAlertMessage
{
    public static class NonTemporaryAlertMessagePresenter
    {
        public static void Initialize(NonTemporaryAlertMessageModel model, NonTemporaryAlertMessageView view)
        {
            model.NonTemporaryAlertMessage
                .SubscribeToText(view.text);
            model.NonTemporaryAlertMessage
                .Select(msg => !string.IsNullOrEmpty(msg))
                .Subscribe(view.gameObject.SetActive);
        }
    }
}