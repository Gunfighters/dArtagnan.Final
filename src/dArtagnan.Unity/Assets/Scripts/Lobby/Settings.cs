using R3;
using UnityEngine;

namespace Lobby
{
    [CreateAssetMenu(fileName = "Settings", menuName = "d'Artagnan/Settings", order = 0)]
    public class Settings : ScriptableObject
    {
        public SerializableReactiveProperty<float> fxVolume;
        public SerializableReactiveProperty<float> bgmVolume;
        public SerializableReactiveProperty<bool> haptic;
    }
}