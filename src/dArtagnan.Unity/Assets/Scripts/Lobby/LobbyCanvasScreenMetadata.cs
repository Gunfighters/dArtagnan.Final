using System;
using UnityEngine;

namespace Lobby
{
    [Serializable]
    public struct LobbyCanvasScreenMetadata
    {
        public LobbyCanvasScreenType screenType;
        public Canvas canvas;
        public RectTransform background;
        public AudioClip audioClip;
    }
}