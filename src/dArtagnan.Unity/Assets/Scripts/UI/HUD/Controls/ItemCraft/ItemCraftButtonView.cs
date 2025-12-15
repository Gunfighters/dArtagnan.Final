using dArtagnan.Shared;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.HUD.Controls.ItemCraft
{
    public class ItemCraftButtonView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        // [SerializeField] private Image outline;
        // [SerializeField] private Image craftIcon;
        // [SerializeField] private Image filler;
        // [SerializeField] private ShopItemSo shopItemCollection;
        //
        // private void Start()
        // {
        //     ItemCraftButtonPresenter.Initialize(new ItemCraftButtonModel(), this);
        // }
        //
        public void OnPointerClick(PointerEventData eventData)
        {
            if (GameModel.Instance.LocalPlayer.CurrentValue.Mining.CurrentValue) return;
            GameModel.Instance.LocalPlayer.CurrentValue.Direction.Value = Vector2.zero;
            PacketChannel.Raise(GameModel.Instance.LocalPlayer.CurrentValue.GetMovementDataFromClient());
            GameModel.Instance.LocalPlayer.CurrentValue.Mining.Value = true;
            PacketChannel.Raise(new UpdateMiningStateFromClient { IsMining = true });
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
        //
        // public void ShowItem(ItemId id)
        // {
        //     if (id is ItemId.None or 0) return;
        //     _hasItem = true;
        //     _item = shopItemCollection.items.First(item => item.data.Id == id);
        //     currentItemIcon.sprite = _item.icon;
        //     descriptionText.text = _item.data.Description;
        //     currentItemIcon.enabled = true;
        //     craftIcon.enabled = false;
        // }
        //
        // public void HideItem()
        // {
        //     _hasItem = false;
        //     currentItemIcon.enabled = false;
        //     craftIcon.enabled = true;
        // }
    }
}