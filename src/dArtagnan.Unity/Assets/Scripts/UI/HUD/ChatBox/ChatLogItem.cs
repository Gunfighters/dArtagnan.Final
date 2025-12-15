using System.Threading;
using TMPro;
using UI.HUD.KillLog;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.ChatBox
{
    public class ChatLogItem : LogItem
    {
        public TextMeshProUGUI text;
        public Image background;
        private Color bgColor;
        private Color textColor;
    }
}