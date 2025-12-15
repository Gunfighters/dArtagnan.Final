using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UI.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.StakeBoard
{
    public class StakeBoardView : MonoBehaviour
    {
        [Header("UI")]
        public BalanceUnit iconPrefab;
        public TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI stakeText;
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private Transform itemPoolContainer;
        public MonetaryUnitCollection monetaryUnitCollection;
        public Slider deductionProgressSlider;
        public Image deductionProgressSliderFill;
        public Sprite normalProgressSliderFill;
        public Sprite emergencyProgressSliderFill;
        public BalanceUnit deductionUnitImage;
        public TextMeshProUGUI deductionUnitCount;
        public TextMeshProUGUI timeUntilNextDeduction;
        public float iconScaleUpDuration;
        public float awardAnimationDuration;
        private readonly Stack<BalanceUnit> _balanceUnitPool = new();
        private StakeBoardModel _stakeBoardModel;
        private CancellationTokenSource _cts = new();
        public SerializableReactiveProperty<StakeBoardAnimationType> currentAnimation = new(StakeBoardAnimationType.None);
        private readonly List<BalanceUnit> _animatingUnits = new();
        public float iconScaleDownDuration;

        private void Awake()
        {
            foreach (var c in itemContainer.GetComponentsInChildren<BalanceUnit>())
                ReturnToPool(c);
            _stakeBoardModel = GetComponent<StakeBoardModel>();
            _stakeBoardModel.AmountChanged
                .Select(pair => pair.Current)
                .Subscribe(SetAmount);
        }

        private void Start()
        {
            StakeBoardPresenter.Initialize(_stakeBoardModel, this);
        }

        private BalanceUnit GetNewUnit()
        {
            var u = _balanceUnitPool.Any() ? _balanceUnitPool.Pop() : Instantiate(iconPrefab, itemContainer);
            u.gameObject.SetActive(true);
            u.transform.SetParent(itemContainer);
            return u;
        }

        private void ReturnToPool(BalanceUnit unit)
        {
            unit.gameObject.SetActive(false);
            unit.Reset();
            unit.RectTransform.SetParent(itemPoolContainer);
            _balanceUnitPool.Push(unit);
        }

        public void RequestAnimation(StakeBoardAnimationType type, int fromAmount, int toAmount)
        {
            Debug.Log($"Stake {type} {fromAmount} >> {toAmount}");
            if (currentAnimation.CurrentValue != StakeBoardAnimationType.None)
                InterruptCurrentAnimation();
            // SetAmount(toAmount);
            currentAnimation.Value = type;
            _animatingUnits.Clear();
            _cts = new CancellationTokenSource();
            switch (type)
            {
                case StakeBoardAnimationType.None:
                    currentAnimation.Value = StakeBoardAnimationType.None;
                    break;
                case StakeBoardAnimationType.Award:
                    LoseAllUnitsSequentially().SuppressCancellationThrow()
                        .ContinueWith(_ => currentAnimation.Value = StakeBoardAnimationType.None);
                    break;
                case StakeBoardAnimationType.Increase:
                    var unitTask = ShowNewAmountAnimation(fromAmount, toAmount).SuppressCancellationThrow();
                    var textTask = ShowNewAmountTextAnimation(fromAmount, toAmount).SuppressCancellationThrow();
                    UniTask.WhenAll(unitTask, textTask).ContinueWith(_ => currentAnimation.Value = StakeBoardAnimationType.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private void InterruptCurrentAnimation()
        {
            _cts.Cancel();
            switch (currentAnimation.CurrentValue)
            {
                case StakeBoardAnimationType.None:
                    break;
                case StakeBoardAnimationType.Increase:
                    InterruptIncrease();
                    break;
                case StakeBoardAnimationType.Award:
                    InterruptAward();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentAnimation), currentAnimation, null);
            }
        }

        private void InterruptIncrease()
        {
            _animatingUnits.ForEach(u => u.Reset());
        }

        private void InterruptAward()
        {
            _animatingUnits.ForEach(ReturnToPool);
        }

        public void SetAmount(int amount)
        {
            var count = monetaryUnitCollection.CalculateUnitsByAmount(amount).Count;
            var activeUnits = itemContainer.GetComponentsInChildren<BalanceUnit>();
            foreach (var u in activeUnits) ReturnToPool(u);
            for (var i = 0; i < count; i++) GetNewUnit();
            stakeText.text = $"${amount}";
        }

        private async UniTask ShowNewAmountAnimation(int fromAmount, int toAmount)
        {
            SetAmount(fromAmount);
            var delta = toAmount - fromAmount;
            var count = monetaryUnitCollection.CalculateUnitsByAmount(delta).Count;
            var tasks = new UniTask[count];
            for (var i = 0; i < count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var u = GetNewUnit();
                _animatingUnits.Add(u);
                tasks[i] = u.ScaleUp(iconScaleUpDuration, _cts.Token);
            }
            await UniTask.WhenAll(tasks);
            _animatingUnits.Clear();
        }

        private async UniTask ShowNewAmountTextAnimation(int from, int to)
        {
            var elapsed = 0f;
            while (elapsed < iconScaleUpDuration)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var m = Mathf.Lerp(from, to, elapsed / iconScaleUpDuration);
                stakeText.text = $"${m:N0}";
                elapsed += Time.deltaTime;
                await UniTask.WaitForEndOfFrame(cancellationToken: _cts.Token);
            }
            stakeText.text = $"${to:N0}";
        }

        [ContextMenu("Lose All Units Sequentially")]
        private async UniTask LoseAllUnitsSequentially()
        {
            var unitNumber = monetaryUnitCollection.Units[0].threshold;
            var activeUnits = itemContainer.GetComponentsInChildren<BalanceUnit>().Reverse().ToArray();
            var count = activeUnits.Length;
            for (var i = 0; i < count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var u = activeUnits[i];
                ReturnToPool(u);
                stakeText.text = $"${unitNumber * (count - i - 1):N0}";
                await UniTask.WaitForSeconds(Mathf.Min(0.2f, awardAnimationDuration / count));
            }
        }
    }
}