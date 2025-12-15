using System.Collections.Generic;
using System.Linq;
using Game.Player.UI;
using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerItem : MonoBehaviour
    {
        private PlayerModel _model;
        public Transform activeFxContainer;
        public ItemIconView itemIconViewPrefab;

        private readonly Stack<ItemIconView> _itemIconPool = new();
        private readonly Stack<ItemIconView> _activeIcons = new();

        private void Awake()
        {
            foreach (var c in activeFxContainer.GetComponentsInChildren<ItemIconView>())
                ReturnToPool(c);
            _model = GetComponent<PlayerModel>();
            _model.OwnedItems.Subscribe(arr =>
            {
                while (_activeIcons.Any()) ReturnToPool(_activeIcons.Pop());
                foreach (var itemId in arr)
                {
                    var icon = GetNewItemIcon();
                    icon.id.Value = itemId;
                    _activeIcons.Push(icon);
                }
            });
        }

        private ItemIconView GetNewItemIcon()
        {
            var view = _itemIconPool.Any() ? _itemIconPool.Pop() : Instantiate(itemIconViewPrefab, activeFxContainer);
            view.gameObject.SetActive(true);
            return view;
        }

        private void ReturnToPool(ItemIconView item)
        {
            item.gameObject.SetActive(false);
            _itemIconPool.Push(item);
        }
    }
}