using Assets.HeroEditor4D.Common.Scripts.EditorScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.Dressroom
{
    public class ShopView : MonoBehaviour
    {
        private ShopModel _model;
        public Button closeShopButton;
        public Button homeButton;
        public TextMeshProUGUI gold;
        public TextMeshProUGUI silver;
        public RectTransform goldbar;
        public RectTransform silverBar;
        [Header("Open Roulette")]
        public Button openRouletteOnce;
        public Button openRouletteMultiple;
        public Button buyRouletteOnceCrystal;
        public Button buyRouletteOnceBanknote;
        public Button buyRouletteMultipleCrystal;
        public Button buyRouletteMultipleBanknote;
        [Header("Roulette Screen")]
        public OneTimePartRouletteManager rouletteOnceScreen;
        public RectTransform rouletteMultipleScreen;
        [Header("References")]
        public CharacterEditor characterEditor;
        private void Awake()
        {
            _model = GetComponent<ShopModel>();
        }

        private void Start()
        {
            ShopPresenter.Initialize(_model, this);
        }

    }
}