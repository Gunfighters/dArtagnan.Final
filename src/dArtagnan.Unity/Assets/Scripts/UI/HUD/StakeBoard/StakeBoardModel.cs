using Game;
using Networking;
using R3;
using UnityEngine;

namespace UI.HUD.StakeBoard
{
    public class StakeBoardModel : MonoBehaviour
    {
        public SerializableReactiveProperty<int> round;
        public SerializableReactiveProperty<int> deductionUnit;
        public SerializableReactiveProperty<float> progressToDeduction;
        public SerializableReactiveProperty<float> deductionPeriod;
        public SerializableReactiveProperty<int> stakes;
        public readonly Subject<(int Previous, int Current)> AmountChanged = new();
        public readonly Subject<int> GivePrize = new();

        private void Start()
        {
            GameModel.Instance.Round.Subscribe(r => round.Value = r);
            if (WebsocketManager.Instance.joinAsSpectator)
                GameModel.Instance.BaseTax.Subscribe(a => deductionUnit.Value = a);
            else
                GameModel.Instance.LocalPlayer.WhereNotNull().Subscribe(l => l.NextDeductionAmount.Subscribe(a => deductionUnit.Value = a));
            GameModel.Instance.progressToDeduction.Subscribe(p => progressToDeduction.Value = p);
            GameModel.Instance.deductionPeriod.Subscribe(p => deductionPeriod.Value = p);
            GameModel.Instance.Stakes.Pairwise().Subscribe(tuple =>
            {
                AmountChanged.OnNext(tuple);
                stakes.Value = tuple.Current;
            });
            GameModel.Instance.RoundWinners.Subscribe(e => GivePrize.OnNext(e.PrizeMoney));
        }
    }
}