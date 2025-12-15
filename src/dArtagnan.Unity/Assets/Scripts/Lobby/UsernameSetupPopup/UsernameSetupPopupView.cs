using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.UsernameSetupPopup
{
    public class UsernameSetupPopupView : MonoBehaviour
    {
        private UsernameSetupPopupModel _model;
        public TMP_InputField nicknameInputField;
        public Button privacyPolicyLinkButton;
        public Toggle privacyPolicyToggle;
        public Button confirmButton;
        public Button closeButton;
        public TextMeshProUGUI errorLog;

        private void Awake()
        {
            _model = GetComponent<UsernameSetupPopupModel>();
            UsernameSetupPopupPresenter.Initialize(_model, this);
        }

        private void OnEnable()
        {
            errorLog.text = "닉네임은 언제든지 바꿀 수 있습니다.";
            errorLog.color = Color.white;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = "변경";
        }
    }
}