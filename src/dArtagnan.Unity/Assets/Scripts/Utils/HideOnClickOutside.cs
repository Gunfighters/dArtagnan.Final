using UnityEngine;

namespace Utils
{
    [RequireComponent(typeof(RectTransform))]
    public class HideOnClickOutside : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0) && !RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, Input.mousePosition))
                gameObject.SetActive(false);
        }
    }
}