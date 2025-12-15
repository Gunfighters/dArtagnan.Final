using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.PopupOnDeath
{
    public class PopupOnDeathView : MonoBehaviour
    {
        private PopupOnDeathModel _model;
        public Image containerImage;
        public Sprite normalDeath;
        public Sprite bankruptDeath;
        public TextMeshProUGUI mainText;
        public TextMeshProUGUI balanceText;
        public TextMeshProUGUI revivalText;

        private void Awake()
        {
            _model = GetComponent<PopupOnDeathModel>();
            PopupOnDeathPresenter.Initialize(_model, this);
        }
    }
}