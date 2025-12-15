using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using JetBrains.Annotations;
using Networking;
using R3;
using UnityEngine;

namespace UI.HUD
{
    public class HUDModel : MonoBehaviour
    {
        [Header("Tags")]
        public SerializableReactiveProperty<bool> waiting;
        public SerializableReactiveProperty<bool> inRound;
        public SerializableReactiveProperty<bool> alive;
        public SerializableReactiveProperty<bool> dead;
        public SerializableReactiveProperty<bool> host;
        public SerializableReactiveProperty<bool> always;
        public SerializableReactiveProperty<bool> aliveInRound;
        [Header("Ribbon")]
        public SerializableReactiveProperty<bool> showRibbon;
        public SerializableReactiveProperty<string> ribbonText;
        public SerializableReactiveProperty<string> ribbonRewardText;
        [Header("Timer For Bankrupt")]
        public SerializableReactiveProperty<bool> showTimerForBankrupt;
        public SerializableReactiveProperty<float> timerForBankruptRatio;
        [Header("Misc")]
        public SerializableReactiveProperty<bool> localPlayerFury;
        public SerializableReactiveProperty<bool> showPassword;
        public SerializableReactiveProperty<bool> showLeaderboard;

        private void Start()
        {
            GameModel.Instance.State.Subscribe(s =>
            {
                switch (s)
                {
                    case GameState.Round:
                        inRound.Value = true;
                        waiting.Value = false;
                        break;
                    case GameState.Waiting:
                        inRound.Value = false;
                        waiting.Value = true;
                        break;
                }
            }).AddTo(this);
            GameModel.Instance.HostPlayer
                .WhereNotNull()
                .CombineLatest(GameModel.Instance.LocalPlayer, (x, y) => x == y)
                .CombineLatest(GameModel.Instance.State.Select(s => s == GameState.Waiting), (localIsHost, isWaiting) => localIsHost && isWaiting)
                .Subscribe(showHostGUI =>
                {
                    host.Value = showHostGUI;
                }).AddTo(this);
            GameModel.Instance.LocalPlayer
                .WhereNotNull()
                .Select(p => p.Alive)
                .Subscribe(observableAlive =>
                {
                    observableAlive.Subscribe(newAlive =>
                    {
                        alive.Value = newAlive;
                        dead.Value = !newAlive;
                    });
                    observableAlive
                        .CombineLatest(GameModel.Instance.State,
                            (isAlive, state) => isAlive && state == GameState.Round)
                        .Subscribe(yes => aliveInRound.Value = yes);
                }).AddTo(this);
            dead.Value = WebsocketManager.Instance?.joinAsSpectator ?? false;
            GameModel.Instance.ShowVictory
                .Subscribe(t => ShowRibbon(t.winner, t.reward)).AddTo(this);
            GameModel.Instance.GameWinners
                .Subscribe(_ =>
                {
                    showRibbon.Value = false;
                    showLeaderboard.Value = true;
                });
            GameModel.Instance.State.Subscribe(_ => showLeaderboard.Value = false);
            GameModel.Instance.inShopWhileLocalIsBankrupt.Subscribe(yes => showTimerForBankrupt.Value = yes);
            GameModel.Instance.RemainingShopTime
                .CombineLatest(GameModel.Instance.MaxShopTime, (r, f) => r / f)
                .Where(v => !float.IsNaN(v))
                .Subscribe(ratio => timerForBankruptRatio.Value = ratio);
        }

        private void ShowRibbon([CanBeNull] PlayerModel winner, int reward)
        {
            showRibbon.Value = true;
            if (winner is null)
            {
                ribbonText.Value = "아무도 우승하지 못했습니다!";
                ribbonRewardText.Value = "1등 없이 게임이 끝납니다.";
            }
            else
            {
                ribbonText.Value = $"{winner.Nickname}님이 우승했습니다!";
                ribbonRewardText.Value = $"1등 보상: {reward}";
            }
        }
    }
}