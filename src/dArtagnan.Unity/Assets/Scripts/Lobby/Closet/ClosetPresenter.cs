using Audio;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Lobby.Closet
{
    public static class ClosetPresenter
    {
        public static void Initialize(ClosetModel model, ClosetView view)
        {
            view.closeClosetButon.OnClickAsObservable().Subscribe(_ =>
            {
                LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            });
            view.homeButton.OnClickAsObservable().Subscribe(_ =>
            {
                LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            });
        }
    }
}