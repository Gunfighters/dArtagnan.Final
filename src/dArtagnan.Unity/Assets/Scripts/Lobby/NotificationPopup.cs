using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    public class NotificationPopup : MonoBehaviour
    {
        public SerializableReactiveProperty<string> title;
        public SerializableReactiveProperty<string> message;
        public TextMeshProUGUI titleTMP;
        public TextMeshProUGUI messageTMP;
        public Button confirmButton;

        private void Awake()
        {
            title.SubscribeToText(titleTMP);
            message.SubscribeToText(messageTMP);
            confirmButton.OnClickAsObservable().Subscribe(_ => Destroy(gameObject));
        }
    }
}