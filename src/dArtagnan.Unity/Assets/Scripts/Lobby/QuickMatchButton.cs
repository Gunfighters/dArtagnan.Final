using Audio;
using Cysharp.Threading.Tasks;
using Networking;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    [RequireComponent(typeof(Button))]
    public class QuickMatchButton : MonoBehaviour
    {
        private Button _btn;
        public TextMeshProUGUI buttonText;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                _btn.interactable = false;
                buttonText.text = "검색 중...";
                WebsocketManager.Instance.JoinRoom().Forget();
            });
            WebsocketManager.Instance.JoinRoomSuccess.Subscribe(msg =>
            {
                if (msg.ok)
                    buttonText.text = "접속 중...";
                else
                {
                    _btn.interactable = true;
                    buttonText.text = "빠른 대전";
                }
            });
        }
    }
}