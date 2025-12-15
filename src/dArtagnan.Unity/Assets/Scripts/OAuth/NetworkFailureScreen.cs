using Networking;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OAuth
{
    public class NetworkFailureScreen : MonoBehaviour
    {
        public RectTransform screen;
        public Button confirmButton;
        private void Awake()
        {
            confirmButton.OnClickAsObservable().Subscribe(_ => SceneManager.LoadScene("Login"));
        }
        private void Start()
        {
            WebsocketManager.Instance.ConnectionClosed += ShowScreen;
        }

        private void ShowScreen()
        {
            screen.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            WebsocketManager.Instance.ConnectionClosed -= ShowScreen;
        }
    }
}