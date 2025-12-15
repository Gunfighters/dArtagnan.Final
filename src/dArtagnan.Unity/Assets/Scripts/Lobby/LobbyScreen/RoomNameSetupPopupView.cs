using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.LobbyScreen
{
    public class RoomNameSetupPopupView : MonoBehaviour
    {
        private RoomNameSetupPopupModel _model;
        public Button confirmButton;
        public Button closeButton;
        public TMP_InputField roomNameInputField;
        public TextMeshProUGUI confirmButtonText;
        public Toggle isPrivateRoom;

        private void Awake()
        {
            _model = GetComponent<RoomNameSetupPopupModel>();
            RoomNameSetupPopupPresenter.Initialize(_model, this);
        }

        private void OnEnable()
        {
            confirmButton.interactable = true;
            confirmButtonText.text = "방 만들기";
        }
    }
}