using UnityEngine;
using UnityEngine.UI;

namespace Lobby.UIControllers
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class GridLayoutGroupController : MonoBehaviour
    {
        private GridLayoutGroup _gridLayoutGroup;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _gridLayoutGroup = GetComponent<GridLayoutGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            var totalWidth =  _rectTransform.rect.width - _gridLayoutGroup.padding.left - _gridLayoutGroup.padding.right;
            _gridLayoutGroup.constraintCount = (int)Mathf.Floor(totalWidth / _gridLayoutGroup.cellSize.x);
        }
    }
}