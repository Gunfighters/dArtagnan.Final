using System.Linq;
using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public class PlayerListModel : MonoBehaviour
    {
        public SerializableReactiveProperty<PlayerModel[]> playerList;

        private void Start()
        {
            GameModel.Instance
                .PlayerModels
                .ObserveDictionaryAdd()
                .Subscribe(_ =>
                {
                    playerList.Value = GameModel.Instance.PlayerModels.Select(pair => pair.Value).ToArray();
                });
            GameModel.Instance
                .PlayerModels
                .ObserveDictionaryRemove()
                .Subscribe(_ =>
                {
                    playerList.Value = GameModel.Instance.PlayerModels.Select(pair => pair.Value).ToArray();
                });
            GameModel.Instance
                .PlayerModels
                .ObserveClear()
                .Subscribe(_ =>
                {
                    playerList.Value = GameModel.Instance.PlayerModels.Select(pair => pair.Value).ToArray();
                });
            GameModel.Instance.PlayerBalanceUpdated
                .Subscribe(_ =>
                    playerList.Value = GameModel.Instance.PlayerModels
                        .Select(pair => pair.Value)
                        .OrderBy(m => m.Balance.CurrentValue)
                        .Reverse()
                        .ToArray());
        }
    }
}