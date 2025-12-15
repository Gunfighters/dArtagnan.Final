using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.KillLog
{
    public class KillLogContainer : MonoBehaviour
    {
        public KillLogItem killLogPrefab;
        public BankruptLogItem bankruptLogPrefab;
        public MineResultLogItem mineResultLogPrefab;
        public int maxSimultaneousLines;
        private readonly Stack<KillLogItem> _killLogPool = new();
        private readonly Stack<BankruptLogItem> _bankruptLogPool = new();
        private readonly Stack<MineResultLogItem> _mineResultLogPool = new();
        private readonly Queue<LogItem> _activeItems = new();
        public float itemDuration;
        public float fadeOutDuration;

        private void Awake()
        {
            foreach (var c in GetComponentsInChildren<LogItem>())
                ReturnToPool(c);
        }

        private void Start()
        {
            GameModel.Instance.NewKillLogInformation.Subscribe(info =>
            {
                var item = GetItem<KillLogItem>();
                item.leftText.text = info.left.Nickname.CurrentValue;
                item.rightText.text = info.right.Nickname.CurrentValue;
                DisplayAndScheduleFadeOut(item);
            });
            GameModel.Instance.NewBankruptInformation.Subscribe(info =>
            {
                var item = GetItem<BankruptLogItem>();
                item.nicknameText.text = info.Nickname.CurrentValue;
                item.transform.SetAsLastSibling();
                DisplayAndScheduleFadeOut(item);
            });
            GameModel.Instance.NewMineRewardInformation.Subscribe(info =>
            {
                var item = GetItem<MineResultLogItem>();
                item.nicknameText.text = info.rewarded.Nickname.CurrentValue;
                item.amountText.text = info.amount.ToString();
                DisplayAndScheduleFadeOut(item);
            });
        }

        private void DisplayAndScheduleFadeOut(LogItem item)
        {
            item.transform.SetAsLastSibling();
            _activeItems.Enqueue(item);
            UniTask
                .WaitForSeconds(itemDuration, cancellationToken: item.Cts.Token)
                .ContinueWith(() => item.FadeOut(fadeOutDuration).SuppressCancellationThrow());
            if (_activeItems.Count > maxSimultaneousLines)
                ReturnToPool(_activeItems.Dequeue());
        }

        private T GetItem<T>() where T : LogItem
        {
            LogItem item;
            if (typeof(T) == typeof(BankruptLogItem))
                item = _bankruptLogPool.Any() ? _bankruptLogPool.Pop() : Instantiate(bankruptLogPrefab, transform);
            else if (typeof(T) == typeof(KillLogItem))
                item = _killLogPool.Any() ? _killLogPool.Pop() : Instantiate(killLogPrefab, transform);
            else if (typeof(T) == typeof(MineResultLogItem))
                item = _mineResultLogPool.Any() ? _mineResultLogPool.Pop() : Instantiate(mineResultLogPrefab, transform);
            else
                throw new ArgumentOutOfRangeException();
            item.Reset();
            item.gameObject.SetActive(true);
            return (T) item;
        }

        private void ReturnToPool(LogItem item)
        {
            item.gameObject.SetActive(false);
            switch (item)
            {
                case BankruptLogItem logItem:
                    _bankruptLogPool.Push(logItem);
                    break;
                case KillLogItem killLogItem:
                    _killLogPool.Push(killLogItem);
                    break;
                case MineResultLogItem mineResultLogItem:
                    _mineResultLogPool.Push(mineResultLogItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}