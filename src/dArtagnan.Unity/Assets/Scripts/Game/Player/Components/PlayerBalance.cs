using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Audio;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UI.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public enum AnimationType
    {
        None,
        Tax,
        Loot,
        Mine,
        Prize
    }
    public class PlayerBalance : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup balanceContainer;
        [SerializeField] private BalanceUnit prefab;
        [SerializeField] private Transform unitPoolContainer;
        [SerializeField] private TextMeshProUGUI balanceTextTmp;
        [SerializeField] private Transform mineRewardContainer;
        [SerializeField] private TextMeshProUGUI bankruptText;
        public int bankruptTextDuration = 1;
        public float iconGrabDuration;
        public float iconScaledownDuration;
        public float iconScaleUpDuration;
        public float prizeAwardAnimationDuration;
        private PlayerModel _model;
        private CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _isAnimationPlaying = new(4, 4);
        private AnimationType _animationType = AnimationType.None;
        private readonly Stack<BalanceUnit> _balanceUnitPool = new();
        private readonly List<BalanceUnit> _animatingUnitsTax = new();
        private readonly List<BalanceUnit> _animatingUnitsLoot = new();
        private readonly List<BalanceUnit> _animatingUnitsMine = new();

        // 소지금을 잃는 애니메이션을 재생해야 하는 경우, 기존 애니메이션을 중단한다.
        // 얻는 애니메이션이라면 기존 애니메이션과 같이 재생한다.

        public void PlayTaxAnimation(int delta, int amountBefore, int deductionAmountBefore)
        {
            _animationType = AnimationType.Tax;
            InterruptCurrentAnimation();
            SetBalanceAndMarkUnitsToBeDeducted(amountBefore, deductionAmountBefore);
            MinimizeTaxedUnits(delta).ContinueWith(() =>
            {
                if (_isAnimationPlaying.CurrentCount == 4)
                {
                    Debug.Log($"Resetting after Tax of {_model.Nickname}");
                    ShowExactBalanceCurrent();
                }
            });
        }

        public void PlayMineAnimation(int delta)
        {
            _animationType = AnimationType.Mine;
            PopupMineReward(delta).ContinueWith(() =>
            {
                if (_isAnimationPlaying.CurrentCount == 4)
                {
                    Debug.Log($"Resetting after Mine of {_model.Nickname}");
                    ShowExactBalanceCurrent();
                }
            });
        }

        public void PlayPrizeAnimation(int delta)
        {
            _animationType = AnimationType.Prize;
            PopupPrizeAwards(delta).ContinueWith(() =>
            {
                if (_isAnimationPlaying.CurrentCount == 4)
                {
                    Debug.Log($"Resetting after Prize of {_model.Nickname}");
                    ShowExactBalanceCurrent();
                }
            });
        }

        public void PlayLootAnimation(PlayerBalance target, int targetBalanceBefore, int delta, int vanished)
        {
            _animationType = AnimationType.Loot;
            target.InterruptCurrentAnimation();
            target.SetBalanceAndMarkUnitsToBeDeducted(targetBalanceBefore - vanished, 0);
            GrabUnitsAll(target, delta)
                .ContinueWith(() =>
                {
                    if (_isAnimationPlaying.CurrentCount == 4)
                    {
                        Debug.Log($"Resetting after Loot of {_model.Nickname}");
                        ShowExactBalanceCurrent();
                    }
                });
        }

        private void InterruptCurrentAnimation()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _animatingUnitsLoot.ForEach(u => u.Reset());
            _animatingUnitsLoot.Clear();
            _animatingUnitsTax.ForEach(ReturnToPool);
            _animatingUnitsTax.Clear();
            _animatingUnitsMine.ForEach(u => u.Reset());
            _animatingUnitsMine.Clear();
        }

        private void Awake()
        {
            bankruptText.enabled = false;
            foreach (var unit in balanceContainer.GetComponentsInChildren<BalanceUnit>())
                ReturnToPool(unit);
            _model = GetComponent<PlayerModel>();
            _model.Initialized += ShowExactBalanceCurrent;
            _model.Bankrupt.Subscribe(yes =>
            {
                if (yes)
                {
                    bankruptText.enabled = true;
                    UniTask.WaitForSeconds(bankruptTextDuration).ContinueWith(() => bankruptText.enabled = false);
                }
            });
        }

        private void OnDestroy() => _model.Initialized -= ShowExactBalanceCurrent;

        private void ShowExactBalanceCurrent()
        {
            SetBalanceAndMarkUnitsToBeDeducted(_model.Balance.CurrentValue, _model.NextDeductionAmount.CurrentValue);
        }

        private BalanceUnit ActivateNewUnit()
        {
            var u = _balanceUnitPool.Any() ? _balanceUnitPool.Pop() : Instantiate(prefab, balanceContainer.transform);
            u.Reset();
            u.transform.SetParent(balanceContainer.transform);
            u.gameObject.SetActive(true);
            return u;
        }

        private void ReturnToPool(BalanceUnit unit)
        {
            unit.gameObject.SetActive(false);
            unit.Reset();
            unit.transform.SetParent(unitPoolContainer);
            _balanceUnitPool.Push(unit);
        }

        private void SetBalanceAndMarkUnitsToBeDeducted(int balance, int deductionAmount)
        {
            Debug.Log($"{_model.Nickname}.SetBalanceAndMarkUnitsToBeDeducted({balance}, {deductionAmount})");
            foreach (var u in balanceContainer.GetComponentsInChildren<BalanceUnit>())
                ReturnToPool(u);
            for (var i = 0; i < balance; i++)
                ActivateNewUnit().toBeDeducted.Value = balance - i <= deductionAmount;
            Debug.Log($"Done setting {_model.Nickname}");
        }

        private async UniTask MinimizeTaxedUnits(int delta)
        {
            await _isAnimationPlaying.WaitAsync();
            try
            {
                var s = new Stack<BalanceUnit>(GetComponentsInChildren<BalanceUnit>());
                var tasks = new List<UniTask>();
                for (var i = 0; i < delta; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var u = s.Pop();
                    _animatingUnitsTax.Add(u);
                    tasks.Add(u
                        .ScaleDown(iconScaledownDuration, _cts.Token)
                        .ContinueWith(() =>
                        {
                            ReturnToPool(u);
                            _animatingUnitsTax.Remove(u);
                        }));
                }

                await UniTask.WhenAll(tasks);
            }
            finally
            {
                _isAnimationPlaying.Release();
            }
        }

        private async UniTask MinimizeVanishedUnits(int delta)
        {
            var s = new Stack<BalanceUnit>(GetComponentsInChildren<BalanceUnit>());
            var tasks = new List<UniTask>();
            for (var i = 0; i < delta; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var u = s.Pop();
                tasks.Add(u.ScaleDown(iconScaledownDuration, _cts.Token).ContinueWith(() => ReturnToPool(u)));
            }
            await UniTask.WhenAll(tasks);
        }

        private async UniTask GrabUnitsAll(PlayerBalance from, int amount)
        {
            await _isAnimationPlaying.WaitAsync();
            try
            {
                var tasks = new UniTask[amount];
                var pool = new Stack<BalanceUnit>(from.balanceContainer.GetComponentsInChildren<BalanceUnit>());
                for (var i = 0; i < amount; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var toBeLooted = pool.Pop();
                    _animatingUnitsLoot.Add(toBeLooted);
                    tasks[i] = GrabUnit(toBeLooted)
                        .ContinueWith(() =>
                        {
                            from.ReturnToPool(toBeLooted);
                            _animatingUnitsLoot.Remove(toBeLooted);
                        });
                    await UniTask.WaitForSeconds(Mathf.Min(0.2f, iconGrabDuration / amount),
                        cancellationToken: _cts.Token);
                }

                await UniTask.WhenAll(tasks);
            }
            finally
            {
                _isAnimationPlaying.Release();
            }
        }

        private async UniTask PopupPrizeAwards(int amount)
        {
            await _isAnimationPlaying.WaitAsync();
            try
            {
                var tasks = new UniTask[amount];
                for (var i = 0; i < amount; i++)
                {
                    var u = ActivateNewUnit();
                    tasks[i] = u.ScaleUp(iconScaleUpDuration, CancellationToken.None);
                    AudioClipPlayer.Instance.Play(AudioClipType.BalanceUnitPop);
                    await UniTask.WaitForSeconds(Mathf.Min(0.2f, prizeAwardAnimationDuration / amount));
                }

                await UniTask.WhenAll(tasks);
            }
            finally
            {
                _isAnimationPlaying.Release();
            }
        }

        private async UniTask GrabUnit(BalanceUnit target)
        {
            AudioClipPlayer.Instance.Play(AudioClipType.BalanceUnitPop);
            target.LayoutElement.ignoreLayout = true;
            // Ensure consistent size (105x118) during grab animation
            target.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, balanceContainer.cellSize.x);
            target.RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, balanceContainer.cellSize.y);
            target.transform.SetParent(balanceContainer.transform);
            var placeholder = ActivateNewUnit();
            placeholder.Icon.enabled = false;
            var elapsed = 0f;
            try
            {
                while (elapsed < iconGrabDuration)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    target.transform.position = Vector2.Lerp(target.transform.position, placeholder.transform.position,
                        elapsed / iconGrabDuration);
                    await UniTask.WaitForEndOfFrame(cancellationToken: _cts.Token);
                }
            }
            finally
            {
                placeholder.Icon.enabled = true;
            }
        }

        private async UniTask PopupMineReward(int amount)
        {
            await _isAnimationPlaying.WaitAsync();
            try
            {
                var tasks = new UniTask[amount];
                for (var i = 0; i < amount; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var u = ActivateNewUnit();
                    _animatingUnitsMine.Add(u);
                    tasks[i] = u.ScaleUp(iconScaleUpDuration, _cts.Token)
                        .ContinueWith(() => _animatingUnitsMine.Remove(u));
                    if (_model == GameModel.Instance.LocalPlayer.CurrentValue)
                        AudioClipPlayer.Instance.Play(AudioClipType.BalanceUnitPop);
                    await UniTask.WaitForSeconds(Mathf.Min(0.2f, iconGrabDuration / amount),
                        cancellationToken: _cts.Token);
                }

                await UniTask.WhenAll(tasks);
            }
            finally
            {
                _isAnimationPlaying.Release();
            }
        }
    }
}