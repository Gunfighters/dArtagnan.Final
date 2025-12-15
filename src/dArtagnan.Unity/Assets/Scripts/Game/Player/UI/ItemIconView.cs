using System.Linq;
using dArtagnan.Shared;
using R3;
using ShopItemScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class ItemIconView : MonoBehaviour
    {
        public ShopItemCollection shopItemCollection;
        public SerializableReactiveProperty<ItemId> id;
        public Image iconImage;

        private void Awake()
        {
            id.Subscribe(newId =>
                iconImage.sprite = shopItemCollection.shopItems.First(i => i.Item.ItemId == newId).icon);
        }
    }
}