using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.ResourceBar
{
    public class ResourceBarView : MonoBehaviour
    {
        private ResourceBarModel _model;
        public Button addGoldButton;
        public Button addCrystalButton;
        public Button showBanknoteDescriptionBtn;
        public Button showCoinDescriptionBtn;
        public RectTransform banknoteDescription;
        public RectTransform coinDescription;
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI crystalText;
        public TextMeshProUGUI silverText;

        private void Awake()
        {
            ResourceBarPresenter.Initialize(_model, this);
        }

        private void Start()
        {
            banknoteDescription.gameObject.SetActive(false);
            coinDescription.gameObject.SetActive(false);
        }
    }
}