using Audio;
using Cysharp.Threading.Tasks;
using Networking;
using R3;

namespace Lobby.LobbyScreen
{
    public static class RoomNameSetupPopupPresenter
    {
        public static void Initialize(RoomNameSetupPopupModel model, RoomNameSetupPopupView view)
        {
            view.roomNameInputField.OnValueChangedAsObservable().Subscribe(v => model.roomName.Value = v);
            view.isPrivateRoom.OnValueChangedAsObservable().Subscribe(isOn => model.isPrivateRoom.Value = isOn);
            view.closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                view.gameObject.SetActive(false);
            });
            view.confirmButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                view.confirmButton.interactable = false;
                view.confirmButtonText.text = "방 만드는 중...";
                WebsocketManager.Instance.CreateRoom(model.roomName.CurrentValue, view.isPrivateRoom.isOn).Forget();
            });
        }
    }
}