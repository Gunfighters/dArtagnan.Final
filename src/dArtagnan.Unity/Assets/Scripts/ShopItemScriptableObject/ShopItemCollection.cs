using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using UnityEngine;

namespace ShopItemScriptableObject
{
    [CreateAssetMenu(fileName = "ShopItemCollection", menuName = "d'Artagnan/Shop Item Collection", order = 0)]
    public class ShopItemCollection : ScriptableObject
    {
        public List<ShopItemCollectionData> shopItems;

        public ShopItemCollectionData FindItemByID(ItemId id) => shopItems.First(data => data.Item.ItemId == id);

        private void OnEnable()
        {
            foreach (var pair in ItemConstants.Items)
            {
                var existing = FindItemByID(pair.Key);
                var icon = existing.icon;
                shopItems.Remove(existing);
                shopItems.Add(new ShopItemCollectionData
                {
                    Item = new ShopItem
                    {
                        Description = pair.Value.Description,
                        ItemId = pair.Key,
                        Name = pair.Value.Name,
                        Price = pair.Value.BasePrice
                    },
                    icon = icon
                });
            }
        }
    }
}