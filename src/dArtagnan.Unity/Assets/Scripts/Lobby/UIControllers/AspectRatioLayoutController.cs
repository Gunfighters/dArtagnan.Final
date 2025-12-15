using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    [RequireComponent(typeof(LayoutElement))]
    [RequireComponent(typeof(AspectRatioFitter))]
    public class AspectRatioLayoutController : MonoBehaviour
    {
        public float aspectRatio = 1f;
        private LayoutElement _layoutElement;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _layoutElement = GetComponent<LayoutElement>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            UpdatePreferredWidth();
        }

        private void UpdatePreferredWidth()
        {
            var height = _rectTransform.rect.height;
            var preferredWidth = height * aspectRatio;
            _layoutElement.preferredWidth = preferredWidth;
        }
    }
}