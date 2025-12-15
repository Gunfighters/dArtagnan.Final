using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Lobby;
using Networking;
using R3;
using UnityEngine;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public AudioClip BGMInGame;
        public AudioClip BGMWaiting;
        public AudioClip BGMWinner;
        private AudioSource _bgmPlayer;
        public Settings settingsSo;

        private void Awake()
        {
            _bgmPlayer = GetComponent<AudioSource>();
            settingsSo.bgmVolume.Subscribe(v => _bgmPlayer.volume = v); 
        }

        private void Start()
        {
            GameModel.Instance.State.Subscribe(s =>
            {
                switch (s)
                {
                    case GameState.Waiting:
                        UniTask.WaitUntil(() => !_bgmPlayer.isPlaying)
                            .ContinueWith(() =>
                            {
                                _bgmPlayer.resource = BGMWaiting;
                                _bgmPlayer.loop = true;
                                _bgmPlayer.Play();
                            });
                        break;
                    case GameState.InitialRoulette:
                        _bgmPlayer.resource = BGMInGame;
                        _bgmPlayer.loop = true;
                        _bgmPlayer.Play();
                        break;
                }
            });
            GameModel.Instance.ShowVictory.Subscribe(_ =>
            {
                _bgmPlayer.resource = BGMWinner;
                _bgmPlayer.loop = false;
                _bgmPlayer.Play();
            });
        }
    }
}