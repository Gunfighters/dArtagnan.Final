using Assets.HeroEditor4D.Common.Scripts.Common;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    public class PrivateRoomPopupView : MonoBehaviour
    {
        public static PrivateRoomPopupView Instance { get; private set; }
        public TMP_InputField passwordInputField;
        public Button confirmButton;
        public SerializableReactiveProperty<string> errorMessage;
        public TextMeshProUGUI confirmText;
        public TextMeshProUGUI errorText;
        [SerializeReference] public RoomInfo roomInfo;
        public Button closeButton;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance.gameObject);
            Instance = this;
            confirmButton.OnClickAsObservable().Subscribe(_ =>
            {
                confirmButton.interactable = false;
                confirmText.text = "검증 중...";
                errorMessage.Value = "";
                
                WebsocketManager.Instance.JoinRoom(roomInfo.roomId, passwordInputField.text).Forget();
            }).AddTo(this);
            passwordInputField.OnValueChangedAsObservable().Select(msg => !msg.IsEmpty()).Subscribe(filled =>
            {
                confirmButton.interactable = filled;
            }).AddTo(this);
            WebsocketManager.Instance.JoinRoomSuccess.Subscribe(msg =>
            {
                if (msg.ok)
                {
                    confirmText.text = "접속 중...";
                    errorMessage.Value = "";
                }
                else
                {
                    confirmButton.interactable = true;
                    confirmText.text = "접속";
                    errorMessage.Value = msg.message;
                }
            }).AddTo(this);
            errorMessage.SubscribeToText(errorText);
            gameObject.SetActive(false);
            closeButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
            });
        }
        
        
        private void OnDisable()
        {
            errorMessage.Value = "";
            confirmText.text = "접속";
            confirmButton.interactable = true;
            passwordInputField.text = "";
        }
    }
}