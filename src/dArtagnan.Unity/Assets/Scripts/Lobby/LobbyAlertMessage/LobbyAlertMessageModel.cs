using R3;
using UnityEngine;

namespace Lobby.LobbyAlertMessage
{
    public class LobbyAlertMessageModel : MonoBehaviour
    {
        public static LobbyAlertMessageModel Instance;
        public readonly Subject<string> Text = new();
        public float duration;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance.gameObject);
            Instance = this;
        }
    }
}