using System.Linq;
using Game;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public static class SpectatingCarouselPresenter
    {
        public static void Initialize(SpectatingCarouselModel model, SpectatingCarouselView view)
        {
            model
                .SpectateTarget
                .Subscribe(target =>
                {
                    if (target is not null)
                    {
                        view.colorSlot.color = target.Color;
                        view.textSlot.text = target.Nickname.CurrentValue;
                    }
                });
            view
                .leftButton
                .onClick
                .AddListener(() =>
                    GameModel.Instance.CameraTarget.Value = GameModel.Instance
                        .Survivors
                        .SkipWhile(s => s != model.SpectateTarget.Value)
                        .Skip(1)
                        .DefaultIfEmpty(GameModel.Instance.Survivors.First())
                        .FirstOrDefault());
            view
                .rightButton
                .onClick
                .AddListener(() =>
                    GameModel.Instance.CameraTarget.Value = GameModel.Instance
                        .Survivors
                        .Reverse()
                        .SkipWhile(s => s != model.SpectateTarget.Value)
                        .Skip(1)
                        .DefaultIfEmpty(GameModel.Instance.Survivors.Reverse().First())
                        .FirstOrDefault());
        }
    }
}