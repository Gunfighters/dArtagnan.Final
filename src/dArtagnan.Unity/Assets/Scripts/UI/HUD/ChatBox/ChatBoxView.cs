using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using NativeDialog;
using R3;
using UI.HUD.KillLog;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public class ChatBoxView : MonoBehaviour
    {
        [SerializeField] private ChatLogItem chatPrefab;
        [SerializeField] private SystemLogItem systemLogPrefab;
        [SerializeField] private BankruptLogItem bankruptLogPrefab;
        [SerializeField] private KillLogItem killLogPrefab;
        [SerializeField] private MineResultLogItem mineResultLogPrefab;
        public RectTransform chatLineContainer;
        public ChatInput chatInput;
        public int maxSimultaneousLines;
        private readonly Stack<KillLogItem> _killLogPool = new();
        private readonly Stack<BankruptLogItem> _bankruptLogPool = new();
        private readonly Stack<MineResultLogItem> _mineResultLogPool = new();
        private readonly Stack<ChatLogItem> _chatLogPool = new();
        private readonly Stack<SystemLogItem> _systemMessageLogPool = new();
        private readonly Queue<LogItem> _activeItems = new();
        public float itemDuration;
        public float fadeOutDuration;

        private void Awake()
        {
            foreach (var c in chatLineContainer.GetComponentsInChildren<LogItem>())
            {
                ReturnToPool(c);
            }
            DialogManager.SetLabel("예", "아니오", "닫기");
        }

        private void Start()
        {
            GameModel.Instance.NewChatBroadcast
                .Where(e => e.PlayerId != -1)
                .Where(e => !GameModel.Instance.mutedPlayers.Contains(e.PlayerId))
                .Subscribe(e =>
            {
                var item = GetItem<ChatLogItem>();
                item.text.text = $"{GameModel.Instance.GetPlayerModel(e.PlayerId)!.Nickname}: {e.Message}";
                DisplayAndScheduleFadeOut(item);
            });
            GameModel.Instance.NewChatBroadcast.Where(e => e.PlayerId == -1).Subscribe(e =>
            {
                var item = GetItem<SystemLogItem>();
                item.text.text = e.Message;
                DisplayAndScheduleFadeOut(item);
            });
            GameModel.Instance.NewKillLogInformation.Subscribe(info =>
            {
                var item = GetItem<KillLogItem>();
                item.leftText.text = info.left.Nickname.CurrentValue;
                item.middleImage.sprite = info.middle ? item.normalKillingIcon : item.guardIcon;
                item.nimi.text = info.middle ? "님이" : "님 사격 -";
                item.sentenceEnding.text = info.middle ? "님을 처치" : "님이 방어";
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
            GameModel.Instance.NewMineRewardInformation.Where(info => info.amount > 0).Subscribe(info =>
            {
                var item = GetItem<MineResultLogItem>();
                item.nicknameText.text = info.rewarded.Nickname.CurrentValue;
                item.amountText.text = info.amount.ToString();
                DisplayAndScheduleFadeOut(item);
            });
        }

        private void Update()
        {
            var rectTransform = (RectTransform)transform;
            var vector2 = rectTransform.anchoredPosition;
            if (chatInput.InputField.isFocused)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                vector2.y = GetKeyboardSizeAndroid();
#elif UNITY_IOS
                vector2.y = GetKeyboardSizeIOS();
#endif
            }
            else
            {
                vector2.y = 0;
            }
            rectTransform.anchoredPosition = vector2;
        }

        private static float GetKeyboardSizeAndroid()
        {
            using var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            var unityPlayer = currentActivity.Get<AndroidJavaObject>("mUnityPlayer");
            var view = unityPlayer.Call<AndroidJavaObject>("getView");
            using var rect = new AndroidJavaObject("android.graphics.Rect");
            view.Call("getWindowVisibleDisplayFrame", rect);
            var screenHeight = Screen.height;
            var rectHeight = rect.Call<int>("height");
            var keyboardHeight = screenHeight - rectHeight;
            var scaleFactor = screenHeight / 1080f;
            var keyboardHeightInCanvasUnits = keyboardHeight / scaleFactor;
            return keyboardHeightInCanvasUnits;
        }

        private static float GetKeyboardSizeIOS()
        {
            var screenHeight = Screen.height;
            var keyboardHeight = TouchScreenKeyboard.area.height;

            var scaleFactor = screenHeight / 1080f;

            var keyboardHeightInCanvasUnits = keyboardHeight / scaleFactor;

            return keyboardHeightInCanvasUnits;
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
                item = _bankruptLogPool.Any() ? _bankruptLogPool.Pop() : Instantiate(bankruptLogPrefab, chatLineContainer.transform);
            else if (typeof(T) == typeof(KillLogItem))
                item = _killLogPool.Any() ? _killLogPool.Pop() : Instantiate(killLogPrefab, chatLineContainer.transform);
            else if (typeof(T) == typeof(MineResultLogItem))
                item = _mineResultLogPool.Any() ? _mineResultLogPool.Pop() : Instantiate(mineResultLogPrefab, chatLineContainer.transform);
            else if (typeof(T) == typeof(SystemLogItem))
                item = _systemMessageLogPool.Any() ? _systemMessageLogPool.Pop() : Instantiate(systemLogPrefab, chatLineContainer.transform);
            else if (typeof(T) == typeof(ChatLogItem))
                item = _chatLogPool.Any() ? _chatLogPool.Pop() : Instantiate(chatPrefab, chatLineContainer.transform);
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
                case ChatLogItem chatLogItem:
                    _chatLogPool.Push(chatLogItem);
                    break;
                case SystemLogItem systemLogItem:
                    _systemMessageLogPool.Push(systemLogItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}