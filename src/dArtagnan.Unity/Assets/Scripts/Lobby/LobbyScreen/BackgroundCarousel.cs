using UnityEngine;
using UnityEngine.UI;

namespace Lobby.LobbyScreen
{
    public class BackgroundCarousel : MonoBehaviour
    {
        public Image bg1;
        public Image bg2;
        public float speed;

        private void Update()
        {
            var v = bg1.rectTransform.anchoredPosition;
            v.x -= Time.deltaTime * speed;
            bg1.rectTransform.anchoredPosition = v;
            var nextStartingPoint = new Vector2(bg1.rectTransform.anchoredPosition.x + bg1.rectTransform.rect.width, bg1.rectTransform.anchoredPosition.y);
            bg2.rectTransform.anchoredPosition = nextStartingPoint;
            if (nextStartingPoint.x <= 0)
            {
                bg1.rectTransform.anchoredPosition = new Vector2(bg2.rectTransform.anchoredPosition.x + bg2.rectTransform.rect.width, bg1.rectTransform.anchoredPosition.y);
                (bg1, bg2) = (bg2, bg1);
            }
        }
    }
}