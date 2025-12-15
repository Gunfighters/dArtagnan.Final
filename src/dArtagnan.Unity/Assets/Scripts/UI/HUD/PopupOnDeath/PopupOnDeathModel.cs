using Game;
using R3;
using UnityEngine;

namespace UI.HUD.PopupOnDeath
{
    public class PopupOnDeathModel : MonoBehaviour
    {
        public SerializableReactiveProperty<string> mainText;
        public SerializableReactiveProperty<string> balanceText;
        public SerializableReactiveProperty<string> revivalText;
        public SerializableReactiveProperty<bool> isBankrupt;
        private void Start()
        {
            GameModel.Instance.LocalPlayer.WhereNotNull().Subscribe(local =>
            {
                Debug.Log(local.Alive);
                local.Balance.Subscribe(b =>
                {
                    balanceText.Value = $"현재 소지금: {b}";
                });
                local.Balance.Select(b => b <= 0).Subscribe(yes =>
                {
                    isBankrupt.Value = yes;
                    mainText.Value = yes ? "파산했습니다!" : "사망했습니다!";
                    revivalText.Value = yes ? "더는 부활하지 못합니다." : "다음 라운드에 부활합니다.";
                });
            });
        }
    }
}