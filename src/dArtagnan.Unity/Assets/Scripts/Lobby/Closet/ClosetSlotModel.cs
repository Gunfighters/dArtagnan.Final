using System.Linq;
using Costume;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lobby.Closet
{
    public class ClosetSlotModel : MonoBehaviour
    {
        [Header("References")]
        public CostumeCollection costumeCollection;
        [Header("Value")]
        public SerializableReactiveProperty<int> id;
        public SerializableReactiveProperty<bool> owned;
        public SerializableReactiveProperty<bool> equipped;
        [Header("UI Components")]
        public RenderCostumeToTexture costumeToTexture;
        // public Button purchaseButton;
        public TextMeshProUGUI costumeNameText;
        public TextMeshProUGUI selectText;
        public TextMeshProUGUI equippedText;
        public Image frameFocus;

        private void Awake()
        {
            owned
                .CombineLatest(equipped, (owning, equipping) => (owning, equipping))
                .Subscribe(s =>
                {
                    var (owning, equipping) = s;
                    equippedText.gameObject.SetActive(equipping);
                    // purchaseButton.gameObject.SetActive(s is { equipping: false, owning: false });
                    selectText.gameObject.SetActive(!equipping && owning);
                }).AddTo(this);
            equipped.Subscribe(frameFocus.gameObject.SetActive);
        }
    }
}