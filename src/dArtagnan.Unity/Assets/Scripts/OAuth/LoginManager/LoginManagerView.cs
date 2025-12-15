using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OAuth.LoginManager
{
    public class LoginManagerView : MonoBehaviour
    {
        private LoginManagerModel _model;
        public Slider progressSlider;
        public TextMeshProUGUI statusMessage;
        public Button touchToStart;
        public TextMeshProUGUI errorMessage;
        private float _elapsed;

        private void Awake()
        {
            errorMessage.gameObject.SetActive(false);
            _model = GetComponent<LoginManagerModel>();
            LoginManagerPresenter.Initialize(_model, this);
        }

        private void Update()
        {
            var color = touchToStart.targetGraphic.color;
            color.a = Mathf.Cos(_elapsed * 3) / 3 + 0.67f;
            touchToStart.targetGraphic.color = color;
            _elapsed += Time.deltaTime;
        }
    }
}