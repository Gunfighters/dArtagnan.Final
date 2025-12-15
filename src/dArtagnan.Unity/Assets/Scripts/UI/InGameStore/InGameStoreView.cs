using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.InitialRoulette;
using UI.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGameStore
{
    public class InGameStoreView : MonoBehaviour
    {
        private InGameStoreModel _model;
        public Slider timeSlider;
        public Sprite timeSliderNormalImage;
        public Sprite timeSliderHurryImage;
        public Image timerImage;
        public TextMeshProUGUI accuracyResetCostText;
        public Transform spin;
        public Button spinButton;
        public List<Graphic> spinButtonGraphics;
        public RouletteSlotView[] AccuracySlots { get; private set; }
        public TextMeshProUGUI itemName;
        public TextMeshProUGUI description;
        public TextMeshProUGUI cost;
        public BalanceUnit costIcon;
        public TextMeshProUGUI currentBalance;
        public List<ItemSlotView> itemSlots;
        public Transform OwnedItemListContainer;
        public InGameItemView ownedItemPrefab;
        public List<InGameItemView> OwnedItems { get; private set; }
        public ItemTooltip itemTooltip;
        public PurchaseButton purchaseButton;
        public TextMeshProUGUI currentAccuracyText;
        public ParticleSystem giftBoxParticles;
        public TextMeshProUGUI giftBoxFailureText;

        private void Awake()
        {
            _model = GetComponent<InGameStoreModel>();
            AccuracySlots = spin.GetComponentsInChildren<RouletteSlotView>();
            OwnedItems = OwnedItemListContainer.GetComponentsInChildren<InGameItemView>().ToList();
            OwnedItems.ForEach(o => o.gameObject.SetActive(false));
        }
        private void Start()
        {
            InGameStorePresenter.Initialize(_model, this);
        }
    }
}