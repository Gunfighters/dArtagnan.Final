using System.Linq;
using Audio;
using ObservableCollections;
using R3;
using Utils;

namespace Lobby.LobbyScreen
{
    public static class LobbyScreenPresenter
    {
        public static void Initialize(LobbyScreenModel model, LobbyScreenView view)
        {
            model.ShowSettingsPopup.Subscribe(view.settingsScreen.gameObject.SetActive).AddTo(view);
            model.roomList.Subscribe(list =>
            {
                view.ClearRoom();
                foreach (var info in list.Where(r => r.playerCount > 0))
                    view.CreateRoom(info);
            }).AddTo(view);
            model.roomList.Select(l => !l.Any(r => r.playerCount > 0)).Subscribe(view.roomListInfoText.gameObject.SetActive);
            view.costumeToTexture.character.UpdateCostumeByData(
                model.CurrentBodyParts,
                model.CurrentPaints,
                model.CurrentEquipment);
            model.CurrentEquipment.ObserveChanged()
                .CombineLatest(model.CurrentBodyParts.ObserveChanged(), (a, _) => a)
                .CombineLatest(model.CurrentPaints.ObserveChanged(), (_, b) => b)
                .Subscribe(_ => view.costumeToTexture.character.UpdateCostumeByData(
                    model.CurrentBodyParts,
                    model.CurrentPaints,
                    model.CurrentEquipment));
            view.openSettingsButton.OnClickAsObservable().Subscribe(_ =>
            {
                model.ShowSettingsPopup.OnNext(true);
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            }).AddTo(model);
            view.openShopButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Shop;
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                });
            view.dressRoomButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.DressRoom;
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                }).AddTo(model);
            view.createRoomButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    view.roomNameSetupPopupView.gameObject.SetActive(true);
                    AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                });
            view.seeTutorialButton.OnClickAsObservable().Subscribe(_ =>
            {
                LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Tutorial;
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            }).AddTo(model);
        }
    }
}