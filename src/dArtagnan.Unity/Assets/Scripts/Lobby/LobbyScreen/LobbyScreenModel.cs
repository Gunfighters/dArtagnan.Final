using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Assets.HeroEditor4D.InventorySystem.Scripts.Data;
using Costume;
using dArtagnan.Shared;
using Networking;
using ObservableCollections;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lobby.LobbyScreen
{
    public class LobbyScreenModel : MonoBehaviour
    {
        public readonly Subject<bool> ShowSettingsPopup = new();
        public SerializableReactiveProperty<RoomInfo[]> roomList;
        public ObservableDictionary<EquipmentPart, ItemSprite> CurrentEquipment { get; private set; }
        public ObservableDictionary<BodyPart, ItemSprite> CurrentBodyParts { get; private set; }
        public ObservableDictionary<Paint, Color> CurrentPaints { get; private set; }

        private void Awake()
        {
            if (!WebsocketManager.Instance)
            {
                SceneManager.LoadScene("Login");
                return;
            }
            WebsocketManager.Instance.roomList.Subscribe(newList  => roomList.Value = newList);
            CurrentEquipment = WebsocketManager.Instance.CurrentEquipments;
            CurrentBodyParts = WebsocketManager.Instance.CurrentBodyParts;
            CurrentPaints = WebsocketManager.Instance.CurrentPaints;
        }
    }
}