using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Firebase.Analytics;
using Networking;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lobby
{
    public class LobbySceneScreenManager : MonoBehaviour
    {
        public static LobbySceneScreenManager Instance;
        [Header("Screens")] public SerializableReactiveProperty<LobbyCanvasScreenType> currentScreen;
        public List<LobbyCanvasScreenMetadata> screenList;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance.gameObject);
            Instance = this;
            currentScreen.Subscribe(Switch);
            SubscribeToWebsocket();
            currentScreen.Subscribe(screen =>
            {
                FirebaseAnalytics.LogEvent("LobbyScreen", "Screen", screen.ToString());
            });
        }

        private void SubscribeToWebsocket()
        {
            WebsocketManager.Instance.isNewUser.Subscribe(yes =>
            {
                if (yes)
                    currentScreen.Value = LobbyCanvasScreenType.UsernameSetup;
            }).AddTo(this);
            WebsocketManager.Instance.CreateRoomSuccess.Subscribe(result =>
            {
                LoadGameSceneWithParameters(result.ip, result.port);
            }).AddTo(this);
            WebsocketManager.Instance.JoinRoomSuccess.Where(result => result.ok).Subscribe(result =>
            {
                LoadGameSceneWithParameters(result.ip, result.port);
            }).AddTo(this);
            WebsocketManager.Instance.ConnectionClosed += OnConnectionClosed;
        }

        private static void LoadGameSceneWithParameters(string ip, int port)
        {
            GameLobbyParameter.GameServerIP = ip;
            GameLobbyParameter.GameServerPort = port;
            SceneManager.LoadScene("Game");
        }

        private void Switch(LobbyCanvasScreenType type)
        {
            Debug.Log(type);
            screenList.ForEach(s => s.canvas.gameObject.SetActive(s.screenType == type));
            screenList.Select(s => s.background.gameObject)
                .Where(o => o.activeSelf)
                .ToList()
                .ForEach(o => o.SetActive(false));
            screenList.First(s => s.screenType == type).background.gameObject.SetActive(true);
        }

        private void OnConnectionClosed()
        {
            currentScreen.Value = LobbyCanvasScreenType.ConnectionClosed;
        }

        private void OnDestroy()
        {
            if (WebsocketManager.Instance)
                WebsocketManager.Instance.ConnectionClosed -= OnConnectionClosed;
        }
    }
}