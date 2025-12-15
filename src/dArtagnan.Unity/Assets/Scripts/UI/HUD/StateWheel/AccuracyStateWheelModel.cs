using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Playing
{
    public class AccuracyStateWheelModel
    {
        public readonly ReactiveProperty<int> State = new();

        public AccuracyStateWheelModel()
        {
            GameModel.Instance.State.Subscribe(state =>
            {
                if (state == GameState.Round)
                {
                    State.Value = GameModel.Instance.LocalPlayer.CurrentValue.AccuracyState.Value;
                }
            });
            GameModel.Instance.LocalPlayer
                .WhereNotNull()
                .Subscribe(player => player.AccuracyState.Subscribe(v => State.Value = v));
        }
    }
}