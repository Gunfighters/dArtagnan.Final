using Cysharp.Threading.Tasks;
using R3;

namespace Lobby.LobbyAlertMessage
{
    public static class LobbyAlertMessagePresenter
    {
        public static void Initialize(LobbyAlertMessageModel model, LobbyAlertMessageView view)
        {
            model.Text.Subscribe(t =>
            {
                view.text.text = t;
                view.gameObject.SetActive(true);
                UniTask.WaitForSeconds(model.duration).ContinueWith(() => view.gameObject.SetActive(false));
            });
        }
    }
}