using dArtagnan.Shared;
using Game;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.GameStartButton
{
    public class GameStartButton : MonoBehaviour
    {
        private TextMeshProUGUI innerText;
        private Button btn;
        private void Awake()
        {
            innerText = GetComponentInChildren<TextMeshProUGUI>();
            btn = GetComponent<Button>();
            btn.onClick.AddListener(() => PacketChannel.Raise(new StartGameFromClient()));
        }

        private void Start()
        {
            GameModel.Instance.HostPlayer
                .CombineLatest(GameModel.Instance.LocalPlayer, (host, local) => host == local)
                .Subscribe(isHostLocal =>
                {
                    innerText.text = isHostLocal ? "게임 시작" : "기다리는 중";
                    btn.interactable = isHostLocal;
                });
        }
    }
}