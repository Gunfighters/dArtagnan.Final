using dArtagnan.Shared;
using R3;
using TMPro;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public class ChatInput : MonoBehaviour
    {
        public TMP_InputField InputField { get; private set; }

        private void Awake()
        {
            InputField = GetComponent<TMP_InputField>();
            InputField
                .onSubmit
                .AsObservable()
                .Subscribe(value =>
                {
                    PacketChannel.Raise(new ChatFromClient { Message = value });
                    InputField.text = "";
                });
        }
    }
}