using Game;
using Game.Player.Components;
using Game.Player.Data;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public class SpectatingCarouselModel
    {
        public readonly ReactiveProperty<PlayerModel> SpectateTarget = new();

        public SpectatingCarouselModel()
        {
            GameModel.Instance.CameraTarget.Subscribe(target => SpectateTarget.Value = target);
        }
    }
}