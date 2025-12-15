using System;
using UnityEngine;

namespace Audio
{
    public enum AudioClipType
    {
        ButtonClicked,
        Purchased,
        CountDown,
        BalanceUnitPop,
        ShieldTriggered,
        Victory,
        GachaResult,
        FearBulletEffect,
    }

    [Serializable]
    public struct AudioClipMetaData
    {
        public AudioClipType type;
        public AudioClip clip;
    }
}