using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Closet
{
    public class ClosetView : MonoBehaviour
    {
        private ClosetModel _model;
        public Button closeClosetButon;
        public Button homeButton;
        public RectTransform slotContainer;
        public ClosetSlotModel slotPrefab;

        private void Awake()
        {
            _model = GetComponent<ClosetModel>();
            ClosetPresenter.Initialize(_model, this);
        }
    }
}