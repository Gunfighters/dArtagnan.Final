using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AlertMessage
{
    public class AlertMessageView : MonoBehaviour
    {
        public TextMeshProUGUI messageText;
        public List<Image> decoImage;
        private AlertMessageModel _model;

        private void Awake()
        {
            _model = GetComponent<AlertMessageModel>();
        }
        private void Start()
        {
            AlertMessagePresenter.Initialize(_model, this);
        }
    }
}