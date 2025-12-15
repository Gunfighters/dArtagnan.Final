using Cysharp.Threading.Tasks;
using Game;
using R3;
using UnityEngine;

namespace UI.AlertMessage
{
    public class AlertMessageModel : MonoBehaviour
    {
        public SerializableReactiveProperty<string> message;
        public SerializableReactiveProperty<Color> color;
        public SerializableReactiveProperty<bool> showMsg;
        public SerializableReactiveProperty<float> duration;
        private int _messageCounter;
    }
}