using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.KillLog
{
    public class BankruptLogItem : LogItem
    {
        private Image _background;
        public Image icon;
        public TextMeshProUGUI nicknameText;
        private Color _originalColor;
    }
}