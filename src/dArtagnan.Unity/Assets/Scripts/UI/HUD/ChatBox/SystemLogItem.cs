using System.Threading;
using TMPro;
using UI.HUD.KillLog;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.ChatBox
{
    public class SystemLogItem : LogItem
    {
        public Image _background;
        public TextMeshProUGUI text;
        private Color bgColor;
        private Color textColor;
    }
}