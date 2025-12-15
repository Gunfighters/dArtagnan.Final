using TMPro;
using UnityEngine;

namespace UI.NonTemporaryAlertMessage
{
    public class NonTemporaryAlertMessageView : MonoBehaviour
    {
        private NonTemporaryAlertMessageModel _model;
        public TextMeshProUGUI text;

        private void Awake()
        {
            _model = GetComponent<NonTemporaryAlertMessageModel>();
        }

        private void Start()
        {
            NonTemporaryAlertMessagePresenter.Initialize(_model, this);
            gameObject.SetActive(false);
        }
    }
}