using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.RoomInfoItem
{
    public class RoomInfoItemView : MonoBehaviour
    {
        private RoomInfoItemModel _model;
        public TextMeshProUGUI roomName;
        public TextMeshProUGUI headcount;
        public Graphic[] graphics;
        public Button btn;
        public Image leftIcon;
        public string roomId;

        private void Awake()
        {
            _model = GetComponent<RoomInfoItemModel>();
            RoomInfoItemPresenter.Initialize(_model, this);
        }
    }
}