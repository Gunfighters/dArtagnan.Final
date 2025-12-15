using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using NativeDialog;
using R3;
using TMPro;
using UI.Unit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.PlayerList
{
    public class PlayerListItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [Range(0, 1)] [SerializeField] private float aliveOpacity;
        [Range(0, 1)] [SerializeField] private float deadOpacity;
        public PlayerModel PlayerModel { get; private set; }
        public Image hostCrown;
        public BalanceUnit balanceImage;
        public TextMeshProUGUI balanceText;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (PlayerModel != GameModel.Instance.LocalPlayer.CurrentValue)
                DialogManager.ShowSelect("플레이어 신고", $"{PlayerModel.Nickname.CurrentValue}님을 부적절한 언행/닉네임으로 신고하고, 차단합니다.\n신고는 24시간 내에 처리됩니다.", result =>
                {
                    if (result)
                    {
                        Debug.Log($"{PlayerModel.Nickname.CurrentValue} 신고 및 차단됨");
                        GameModel.Instance.mutedPlayers.Add(PlayerModel.ID.CurrentValue);
                        GameModel.Instance.NewChatBroadcast.OnNext(new ChatBroadcast { PlayerId = -1, Message = $"{PlayerModel.Nickname.CurrentValue}님을 신고 및 차단했습니다." });
                    }
                });
        }

        public void Initialize(PlayerModel model)
        {
            PlayerModel = model;
            model.Nickname.Subscribe(newNick => name = nicknameText.text = newNick).AddTo(this);
            var color = model.Color;
            backgroundImage.color = color;
            model.Accuracy.Subscribe(newAcc => accuracyText.text = $"{newAcc}%").AddTo(this);
            model.Alive.Subscribe(newAlive =>
            {
                backgroundImage.color = newAlive ? color : Color.grey;
                var nicknameTextColor = nicknameText.color;
                nicknameTextColor.a = newAlive ? aliveOpacity : deadOpacity;
                nicknameText.color = nicknameTextColor;
            }).AddTo(this);
            model.Balance.SubscribeToText(balanceText).AddTo(this);
            GameModel.Instance.State.Select(s => s is GameState.Round or GameState.Shop).Subscribe(balanceText.gameObject.SetActive).AddTo(this);
            GameModel.Instance.State.Select(s => s is GameState.Round or GameState.Shop).Subscribe(balanceImage.gameObject.SetActive).AddTo(this);
            GameModel.Instance.State.Select(s => s is GameState.Round or GameState.Shop).Subscribe(accuracyText.gameObject.SetActive).AddTo(this);
            model.HideAccuracyAndRange
                .CombineLatest(model.Alive, (hide, alive) => !hide && alive)
                .Subscribe(shouldShow => accuracyText.enabled = shouldShow).AddTo(this);
            GameModel.Instance.HostPlayer
                .CombineLatest(GameModel.Instance.State,
                    (newHost, state) => state == GameState.Waiting && newHost == model)
                .Subscribe(hostCrown.gameObject.SetActive).AddTo(this);
        }
    }
}