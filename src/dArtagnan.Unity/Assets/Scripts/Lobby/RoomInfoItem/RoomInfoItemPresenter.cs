using Audio;
using Cysharp.Threading.Tasks;
using Networking;
using R3;

namespace Lobby.RoomInfoItem
{
    public static class RoomInfoItemPresenter
    {
        public static void Initialize(RoomInfoItemModel model, RoomInfoItemView view)
        {
            model.roomInfo
                .Subscribe(info =>
                {
                    view.roomName.text = info.roomName;
                    view.headcount.text = $"({info.playerCount} / {info.maxPlayers})";
                    view.roomId = info.roomId;
                    // view.btn.interactable = info.joinable;
                    view.leftIcon.enabled = info.hasPassword;
                    foreach (var g in view.graphics)
                    {
                        var color = g.color;
                        color.a = info.joinable ? 1f: 0.5f;
                        g.color = color;
                    }
                });
            view.btn.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                if (model.roomInfo.CurrentValue.hasPassword)
                {
                    PrivateRoomPopupView.Instance.roomInfo = model.roomInfo.CurrentValue;
                    PrivateRoomPopupView.Instance.gameObject.SetActive(true);
                }
                else
                    WebsocketManager.Instance.JoinRoom(view.roomId).Forget();
            });
        }
    }
}