using R3;
using UnityEngine;

namespace Lobby.LobbyScreen
{
    public class RoomNameSetupPopupModel : MonoBehaviour
    {
        public SerializableReactiveProperty<string> roomName;
        public SerializableReactiveProperty<bool> isPrivateRoom;
    }
}