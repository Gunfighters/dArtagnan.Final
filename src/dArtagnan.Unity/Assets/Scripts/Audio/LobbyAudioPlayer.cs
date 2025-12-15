using System.Linq;
using Lobby;
using R3;
using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class LobbyAudioPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;
        public Settings settingsSo;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            settingsSo.bgmVolume.Subscribe(v => _audioSource.volume = v).AddTo(this);
        }

        private void Start()
        {
            LobbySceneScreenManager.Instance.currentScreen.Subscribe(curr =>
            {
                _audioSource.clip =LobbySceneScreenManager.Instance.screenList.First(c => c.screenType == curr).audioClip;
                _audioSource.Play();
            });
        }
    }
}