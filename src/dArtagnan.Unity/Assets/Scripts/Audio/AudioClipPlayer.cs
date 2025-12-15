using System.Linq;
using Lobby;
using R3;
using UnityEngine;

namespace Audio
{
    public class AudioClipPlayer : MonoBehaviour
    {
        public static AudioClipPlayer Instance { get; private set; }
        public AudioClipMetaData[] metaData;
        private AudioSource _audioSource;
        public Settings settings;

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _audioSource = GetComponent<AudioSource>();
            settings.fxVolume.Subscribe(v => _audioSource.volume = v);
        }

        public void Play(AudioClipType type)
        {
            _audioSource.PlayOneShot(metaData.First(m => m.type == type).clip);
        }
    }
}