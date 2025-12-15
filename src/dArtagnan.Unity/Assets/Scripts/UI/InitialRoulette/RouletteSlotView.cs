using TMPro;
using UnityEngine;

namespace UI.InitialRoulette
{
    public class RouletteSlotView : MonoBehaviour
    {
        public TextMeshProUGUI Text { get; private set; }
        private void Awake()
        {
            Text = GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}