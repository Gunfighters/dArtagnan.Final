using dArtagnan.Shared;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGameStore
{
    public class PurchaseButton : MonoBehaviour
    {
        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
        }
    }
}