using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InitialRoulette
{
    public class InitialRouletteView : MonoBehaviour
    {
        public Transform spin;
        public Button spinButton;
        public List<Graphic> spinButtonGraphics;
        public RouletteSlotView[] Slots { get; private set; }
        private InitialRouletteModel _model;
        private void Awake()
        {
            Slots = spin.GetComponentsInChildren<RouletteSlotView>();
            _model = GetComponent<InitialRouletteModel>();
        }
        private void Start()
        {
            InitialRoulettePresenter.Initialize(_model, this);
        }
        private void OnEnable()
        {
            spinButton.interactable = true;
            spinButtonGraphics.ForEach(c =>
            {
                var color = c.color;
                color.a = 1f;
                c.color = color;
            });
        }
    }
}