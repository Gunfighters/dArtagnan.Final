using UnityEngine;
using UnityEngine.UI;

namespace Lobby.SettingsPopup
{
    public class SettingsPopupView : MonoBehaviour
    {
        private SettingsPopupModel _model;
        [Header("FxVolume")]
        public Slider fxVolumeSlider;
        public Image fxVolumeImage;
        public Sprite fxVolumeOnSprite;
        public Sprite fxVolumeOffSprite;
        [Header("BGM Volume")]
        public Slider bgmVolumeSlider;
        public Image bgmVolumeImage;
        public Sprite bgmVolumeOnSprite;
        public Sprite bgmVolumeOffSprite;
        [Header("Haptic Toggle")]
        public Toggle hapticToggle;
        public Image hapticToggleImage;
        public Sprite hapticToggleOnSprite;
        public Sprite hapticToggleOffSprite;
        [Header("Buttons")]
        public Button closeButton;
        public Button languageButton;
        public Button changeNicknameButton;
        public Button rateButton;
        public Button tutorialButton;
        public Button quitButton;
        public Button creditButton;
        public Button probabilityLookupButton;
        [Header("Credit")]
        public RectTransform creditPopup;

        private void Awake()
        {
            _model = GetComponent<SettingsPopupModel>();
        }

        private void Start()
        {
            SettingsPopupPresenter.Initialize(_model, this);
        }
    }
}