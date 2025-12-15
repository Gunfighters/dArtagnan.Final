using R3;
using R3.Triggers;

namespace OAuth.LoginManager
{
    public static class LoginManagerPresenter
    {
        public static void Initialize(LoginManagerModel model, LoginManagerView view)
        {
            view.progressSlider.minValue = 0;
            view.progressSlider.maxValue = LoginManagerModel.MaxProgress;
            view.touchToStart.OnClickAsObservable().Subscribe(_ => model.ActivateLobbyScene());
            model.progress.Subscribe(val => view.progressSlider.value = val);
            model.statusMessage.SubscribeToText(view.statusMessage);
            model.showTouchToStart.Subscribe(show =>
            {
                view.progressSlider.gameObject.SetActive(!show);
                view.statusMessage.gameObject.SetActive(!show);
                view.touchToStart.gameObject.SetActive(show);
            });
            model.ErrorMessage.Subscribe(newError =>
            {
                view.progressSlider.gameObject.SetActive(false);
                view.statusMessage.gameObject.SetActive(false);
                view.touchToStart.gameObject.SetActive(false);
                view.errorMessage.gameObject.SetActive(true);
                view.errorMessage.text = $"오류가 발생했습니다: {newError}";
            });
        }
    }
}