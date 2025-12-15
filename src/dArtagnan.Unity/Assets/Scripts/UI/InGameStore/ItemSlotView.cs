using Audio;
using dArtagnan.Shared;
using R3;
using ShopItemScriptableObject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.InGameStore
{
    public class ItemSlotView : MonoBehaviour, IPointerClickHandler
    {
        public SerializableReactiveProperty<bool> selected;
        public SerializableReactiveProperty<ItemId> itemId;
        public Image icon;
        public Image background;
        public Sprite normalBackground;
        public Sprite selectedBackground;
        public ShopItemCollection shopItemCollection;
        public readonly Subject<bool> IsClicked = new();

        private void Awake()
        {
            selected.Subscribe(newSelected =>
            {
                background.sprite = newSelected ? selectedBackground : normalBackground;
            });
            itemId.Subscribe(newId =>
            {
                if ((int)newId == -1) newId = ItemId.None;
                icon.sprite = shopItemCollection.FindItemByID(newId).icon;
                icon.enabled = newId != ItemId.None;
            });
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
            IsClicked.OnNext(true);
        }
    }
}