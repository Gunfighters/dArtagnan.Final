using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerMinimapMarker : MonoBehaviour
    {
        public Sprite triangleSprite;
        public Sprite circleSprite;
        public SpriteRenderer spriteRenderer;
        private PlayerModel _model;

        private void Awake()
        {
            _model = GetComponent<PlayerModel>();
            _model.ID.Subscribe(_ =>
            {
                var c = _model.Color;
                c.a = 0.9f; 
                spriteRenderer.color = c;
            });
            _model.Balance.Subscribe(bal =>
                spriteRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 6f, bal / 150f));
        }

        private void Start()
        {
            GameModel.Instance.LocalPlayer
                .Select(local => local == _model ? triangleSprite : circleSprite)
                .Subscribe(sprite => spriteRenderer.sprite = sprite);
        }
    }
}