using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Shop
{
    public class ShopView : MonoBehaviour
    {
        public Button backButton;
        public Button homeButton;
        private void Awake()
        {
            backButton.onClick.AddListener(() => LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby);
            homeButton.onClick.AddListener(() => LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby);
        }
    }
}