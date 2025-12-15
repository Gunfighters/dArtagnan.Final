using Audio;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lobby
{
    public class WebsocketFailureConfirmButton : MonoBehaviour
    {
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.OnClickAsObservable().Subscribe(_ =>
            {
                AudioClipPlayer.Instance.Play(AudioClipType.ButtonClicked);
                SceneManager.LoadScene("Login");
            });
        }
    }
}