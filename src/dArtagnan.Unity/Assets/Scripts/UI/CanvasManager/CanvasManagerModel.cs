using dArtagnan.Shared;
using Game;
using R3;

namespace UI.CanvasManager
{
    public class CanvasManagerModel
    {
        public readonly ReactiveProperty<GameScreen> Screen = new();

        public CanvasManagerModel()
        {
            GameModel.Instance.ConnectionFailure.Subscribe(_ => Screen.Value = GameScreen.NetworkFailure);
            GameModel.Instance.State.Subscribe(state =>
            {
                Screen.Value = state switch
                {
                    GameState.Waiting or GameState.Round => GameScreen.HUD,
                    GameState.InitialRoulette => GameScreen.InitialRoulette,
                    GameState.Shop => GameModel.Instance.inShopWhileLocalIsBankrupt.CurrentValue ? GameScreen.HUD : GameScreen.InGameStore,
                    _ => Screen.Value
                };
            });
        }
    }
}