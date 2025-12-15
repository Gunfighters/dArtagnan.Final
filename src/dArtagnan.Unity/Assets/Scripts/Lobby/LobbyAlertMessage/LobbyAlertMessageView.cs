using TMPro;
using UnityEngine;

namespace Lobby.LobbyAlertMessage
{
    public class LobbyAlertMessageView : MonoBehaviour
    {
        private LobbyAlertMessageModel _model;
        public TextMeshProUGUI text;

        private void Awake()
        {
            _model = GetComponent<LobbyAlertMessageModel>();
        }

        private void Start()
        {
            LobbyAlertMessagePresenter.Initialize(_model, this);
            gameObject.SetActive(false);
        }
    }
}