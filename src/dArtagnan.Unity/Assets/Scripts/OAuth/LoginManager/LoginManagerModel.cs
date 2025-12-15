using System;
using Networking;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OAuth.LoginManager
{
    public class LoginManagerModel : MonoBehaviour
    {
        public const int MaxProgress = 4;
        public string devProviderId;
        public string devEndpoint;
        public string productionEndpoint;
        public SerializableReactiveProperty<float> progress;
        public SerializableReactiveProperty<string> statusMessage;
        public SerializableReactiveProperty<bool> showTouchToStart;
        public readonly Subject<string> ErrorMessage = new();
        private AsyncOperation _lobbySceneLoader;

        private async void Start()
        {
            _lobbySceneLoader = SceneManager.LoadSceneAsync("Lobby")!;
            _lobbySceneLoader.allowSceneActivation = false;
            WebsocketManager.Instance.AuthSuccess.Subscribe(success =>
            {
                if (success)
                {
                    progress.Value = 3;
                    statusMessage.Value = "로비 서버 연결 성공! 로비를 불러오는 중...";
                    this.UpdateAsObservable()
                        .TakeUntil(_ => _lobbySceneLoader.progress >= 0.9f)
                        .Subscribe(_ => progress.Value = 3 + _lobbySceneLoader.progress,
                            _ =>
                            {
                                progress.Value = MaxProgress;
                                showTouchToStart.Value = true;
                            });
                }
                else
                {
                    ErrorMessage.OnNext("로비 서버와 연결하지 못했습니다. 나중에 다시 시도해주세요.");
                }
            }).AddTo(this);
            progress.Value = 1;
            statusMessage.Value = "소셜 로그인 정보를 검증하는 중...";
            OAuthLoginResponse response;
            try
            {
#if UNITY_EDITOR
                response = await DevOAuthLoginManager.Login(devEndpoint, devProviderId);
#elif UNITY_ANDROID
                response = await GoogleOAuthManager.Login(productionEndpoint);
#elif UNITY_IOS
                response = await AppleOAuthManager.Login(productionEndpoint);
#endif
                if (!response.success)
                    throw new Exception($"소셜 로그인 정보 검증 실패. {response}");
            }
            catch (Exception e)
            {
                ErrorMessage.OnNext(e.Message);
                return;
            }
            if (!response.isNewUser)
                WebsocketManager.Instance.nickname.Value = response.nickname;
            Debug.Log($"Login successful. Nickname: {response.nickname}");
            progress.Value = 2;
            statusMessage.Value = "검증 성공. 로비 서버에 연결하는 중...";
            #if UNITY_EDITOR
            var chosenEndpoint = devEndpoint;
            #else
            var chosenEndpoint = productionEndpoint;
            #endif
            await WebsocketManager.Instance.Connect(chosenEndpoint, response.sessionId);
        }

        public void ActivateLobbyScene()
        {
            _lobbySceneLoader.allowSceneActivation = true;
        }
    }
}