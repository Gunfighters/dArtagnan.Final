using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Leaderboard
{
    public class LeaderboardItemView : MonoBehaviour
    {
        public SerializableReactiveProperty<int> rank;
        public SerializableReactiveProperty<string> nickname;
        public SerializableReactiveProperty<int> reward;
        public SerializableReactiveProperty<Color> color;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Image colorSlot;

        private void Awake()
        {
            rank.Subscribe(r => rankText.text = $"{r}등");
            rank.Subscribe(r =>
            {
                rankText.color = r switch
                {
                    1 => Color.yellow,
                    2 => Color.gray,
                    3 => new Color(0.8f, 0.5f, 0.2f),
                    _ => Color.white
                };
            });
            nickname.SubscribeToText(nicknameText);
            reward.SubscribeToText(rewardText);
            color.Subscribe(c => colorSlot.color = c);
        }
    }
}