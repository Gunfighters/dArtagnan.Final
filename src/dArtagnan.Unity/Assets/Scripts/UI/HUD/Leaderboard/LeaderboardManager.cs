using System.Linq;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Leaderboard
{
    public class LeaderboardManager : MonoBehaviour
    {
        public LeaderboardItemView[] Items { get; private set; }
        public SerializableReactiveProperty<LeaderboardItemInfo[]> infos;

        private void Awake()
        {
            Items = GetComponentsInChildren<LeaderboardItemView>(true);
            infos.Subscribe(arr =>
            {
                foreach (var view in Items)
                {
                    view.gameObject.SetActive(false);
                }

                foreach (var info in arr.OrderBy(r => r.rank))
                {
                    var view = Items.First(i => !i.gameObject.activeSelf);
                    view.rank.Value = info.rank;
                    view.nickname.Value = info.nickname;
                    view.reward.Value = info.reward;
                    view.gameObject.SetActive(true);
                }
            });
        }

        private void Start()
        {
            GameModel.Instance.GameWinners
                .Where(w => w.Any())
                .Select(e => e.Select(r => new LeaderboardItemInfo { nickname = r.Nickname, rank = r.Rank, reward = r.RewardMoney }).ToArray())
                .Subscribe(arr => infos.Value = arr);
        }
    }
}