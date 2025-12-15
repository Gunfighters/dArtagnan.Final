using R3;
using ShopItemScriptableObject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.InGameStore
{
    public class InGameItemView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Image image;
        public ShopItemCollection shopItemCollection;
        public SerializableReactiveProperty<bool> showTooltip;

        public void OnPointerDown(PointerEventData eventData)
        {
            showTooltip.Value = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            showTooltip.Value = false;
        }
    }
}