using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using UnityEngine;

namespace Lobby.UsernameSetupPopup
{
    public class UsernameSetupPopupModel : MonoBehaviour
    {
        public SerializableReactiveProperty<string> nicknameInput;
        public SerializableReactiveProperty<bool> privacyPolicyAgreed;
        public SerializableReactiveProperty<bool> canSubmitNickname;
        public Subject<string> NicknameSetFailureReason => WebsocketManager.Instance.NicknameSetFailureReason;
        public readonly ReactiveProperty<int> ToastCount = new();
        public string privacyPolicyLink;
        private readonly Regex _nicknameValidator = new("^(?=.*[a-zA-Z0-9가-힣])[a-zA-Z0-9가-힣]{2,8}$"); // 공백 없이, 한글/영문/숫자로만, 2-8자. 자모는 허용되지 않음.

        private void Awake()
        {
            WebsocketManager.Instance.NicknameSetSuccessful.Subscribe(success =>
            {
                gameObject.SetActive(!success);
                if (success)
                    LobbySceneScreenManager.Instance.currentScreen.Value = LobbyCanvasScreenType.Lobby;
            });
        }

        public async UniTask SubmitNickname()
        {
            await WebsocketManager.Instance.SubmitNickname(nicknameInput.CurrentValue);
        }

        public bool ValidateNickname(string nickname) => true;
    }
}