using System;
using dArtagnan.Shared;
using UnityEngine;

namespace ShopItemScriptableObject
{
    [Serializable]
    public struct ShopItemCollectionData
    {
        public ShopItem Item;
        public Sprite icon;
    }
}