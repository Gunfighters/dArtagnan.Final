using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.UserInfoBar
{
    public class UserInfoView : MonoBehaviour
    {
        public TextMeshProUGUI nicknameText;
        public TextMeshProUGUI levelText;
        public Slider expSlider;
        public TextMeshProUGUI expText;

        private void Awake()
        {
            WebsocketManager.Instance.nickname.SubscribeToText(nicknameText);
            WebsocketManager.Instance.level.SubscribeToText(levelText);
            WebsocketManager.Instance.currentExp.Subscribe(c => expSlider.value = c);
            WebsocketManager.Instance.expToNextLevel.Subscribe(f => expSlider.maxValue = f);
            WebsocketManager.Instance.currentExp
                .CombineLatest(WebsocketManager.Instance.expToNextLevel, (c, f) => $"{c}/{f}")
                .SubscribeToText(expText);
        }
    }
}