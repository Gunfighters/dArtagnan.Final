using System.Collections.Generic;
using System.Linq;
using Costume;
using dArtagnan.Shared;
using Lobby.RoomInfoItem;
using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.LobbyScreen
{
    public class LobbyScreenView : MonoBehaviour
    {
        private LobbyScreenModel _model;
        [Header("Settings")]
        public Button openSettingsButton;
        public RectTransform settingsScreen;
        [Header("Room List")]
        public Transform roomListContainer;
        public RoomInfoItemModel roomInfoItemModelPrefab;
        public TextMeshProUGUI roomListInfoText;
        [Header("Player Preview")]
        public RenderCostumeToTexture costumeToTexture;
        [Header("Bottom Buttons")]
        public Transform gameJoinButtonsContainer;
        public Button dressRoomButton;
        public Button openShopButton;
        public Button createRoomButton;
        public Button quickMatchButton;
        [Header("Popups")]
        public RoomNameSetupPopupView roomNameSetupPopupView;
        public PrivateRoomPopupView privateRoomPopupView;

        [Header("Misc")]
        public Button seeTutorialButton;
        public RectTransform creditScreen;

        private readonly Stack<RoomInfoItemModel> _roomInfoItemPool = new();
        private readonly Stack<RoomInfoItemModel> _roomInfoItems = new();

        private void Awake()
        {
            foreach (var c in roomListContainer.GetComponentsInChildren<RoomInfoItemModel>())
                ReturnToPool(c);
            _model = GetComponent<LobbyScreenModel>();
            // WebsocketManager.Instance.showTutorial.Subscribe(yes =>
            // {
            //     seeTutorialButton.gameObject.SetActive(yes);
            //     gameJoinButtonsContainer.gameObject.SetActive(!yes);
            //     roomListContainer.gameObject.SetActive(!yes);
            // });
        }

        private void Start()
        {
            LobbyScreenPresenter.Initialize(_model, this);
            WebsocketManager.Instance.showTutorial.Subscribe(yes =>
            {
                if (yes)
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Tutorial;
            }).AddTo(this);
        }

        private RoomInfoItemModel GetNewRoomInfoItemModel()
        {
            var i = _roomInfoItemPool.Any()
                ? _roomInfoItemPool.Pop()
                : Instantiate(roomInfoItemModelPrefab, roomListContainer);
            i.transform.SetParent(roomListContainer);
            _roomInfoItems.Push(i);
            i.gameObject.SetActive(true);
            return i;
        }

        private void ReturnToPool(RoomInfoItemModel model)
        {
            model.gameObject.SetActive(false);
            _roomInfoItemPool.Push(model);
        }

        public void CreateRoom(RoomInfo info)
        {
            GetNewRoomInfoItemModel().roomInfo.Value = info;
        }

        public void RemoveRoom(RoomInfo info)
        {
            var found = _roomInfoItems.FirstOrDefault(i => i.roomInfo.CurrentValue.roomId == info.roomId);
            if (found) ReturnToPool(found);
        }

        public void UpdateRoom(RoomInfo info)
        {
            var found = _roomInfoItems.FirstOrDefault(i => i.roomInfo.CurrentValue.roomId == info.roomId);
            if (found) found.roomInfo.Value = info;
        }

        public void ClearRoom()
        {
            while (_roomInfoItems.Any()) ReturnToPool(_roomInfoItems.Pop());
        }
    }
}