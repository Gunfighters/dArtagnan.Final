using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.KillLog
{
    public class KillLogItem : LogItem
    {
        private Image _background;
        public TextMeshProUGUI leftText;
        public Image middleImage;
        public TextMeshProUGUI nimi;
        public TextMeshProUGUI rightText;
        public TextMeshProUGUI sentenceEnding;
        public Sprite normalKillingIcon;
        public Sprite guardIcon;
    }
}