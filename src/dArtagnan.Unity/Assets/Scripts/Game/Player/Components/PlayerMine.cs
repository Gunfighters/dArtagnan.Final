using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UI.Unit;
using UnityEngine;
using UnityEngine.UI;
using Constants = dArtagnan.Shared.Constants;

namespace Game.Player.Components
{
    public class PlayerMine : MonoBehaviour
    {
        public Slider slider;
        [SerializeField] private Transform dollarIconBox;
        private PlayerModel _model;
        public RectTransform successResultContainer;
        public BalanceUnit unit;
        private readonly Stack<BalanceUnit> _balanceUnitPool = new();
        public float rewardTextDuration;
        public TextMeshProUGUI failureText;

        private void Awake()
        {
            foreach (var c in successResultContainer.GetComponentsInChildren<BalanceUnit>())
                ReturnToPool(c);
            _model = GetComponent<PlayerModel>();
            _model.Mining.Subscribe(yes =>
            {
                slider.gameObject.SetActive(yes);
                slider.value = 0;
            });
            _model.MiningComplete.Subscribe(amount =>
            {
                if (amount > 0)
                {
                    for (var i = 0; i < amount; i++)
                        GetNewUnit();
                    UniTask
                        .WaitForSeconds(rewardTextDuration)
                        .ContinueWith(() =>
                        {
                            var existing = successResultContainer.GetComponentsInChildren<BalanceUnit>();
                            foreach (var u in existing)
                                ReturnToPool(u);
                        });
                }
                else
                {
                    failureText.enabled = true;
                    UniTask.WaitForSeconds(rewardTextDuration)
                        .ContinueWith(() => failureText.enabled = false);
                }
            });
        }

        private void Update()
        {
            if (_model.Mining.CurrentValue)
                slider.value += Time.deltaTime / Constants.MINING_DURATION;
        }

        private BalanceUnit GetNewUnit()
        {
            var u = _balanceUnitPool.Any() ? _balanceUnitPool.Pop() : Instantiate(unit, successResultContainer);
            u.Reset();
            u.gameObject.SetActive(true);
            return u;
        }

        private void ReturnToPool(BalanceUnit returned)
        {
            returned.gameObject.SetActive(false);
            _balanceUnitPool.Push(returned);
        }
    }
}