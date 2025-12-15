using System;
using UnityEngine;

namespace UI.HUD.Leaderboard
{
    [Serializable]
    public struct LeaderboardItemInfo
    {
        public int rank;
        public string nickname;
        public int reward;
    }
}