using System.Collections.Generic;
using System.Linq;
using Game.Player.Components;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public class PlayerListView : MonoBehaviour
    {
        public readonly Stack<PlayerListItem> ItemPool = new();
        public readonly Stack<PlayerListItem> ShownItems = new();
        public PlayerListItem itemPrefab;
        private PlayerListModel _model;

        private void Awake()
        {
            foreach (var item in GetComponentsInChildren<PlayerListItem>())
                ReturnToPool(item);
            _model = GetComponent<PlayerListModel>();
        }

        private void Start()
        {
            PlayerListPresenter.Initialize(_model, this);
        }

        public PlayerListItem GetNewItem()
        {
            var item = Instantiate(itemPrefab, transform);
            item.gameObject.SetActive(true);
            ShownItems.Push(item);
            return item;
        }

        public void ReturnToPool(PlayerListItem item)
        {
            Destroy(item.gameObject);
        }
    }
}