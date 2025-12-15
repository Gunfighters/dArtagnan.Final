using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby.SettingsPopup
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleManager : MonoBehaviour
    {
        private Toggle _toggle;
        [Header("Handle")]
        public Image targetHandleGraphic;
        public Sprite onSprite;
        public Sprite offSprite;
        [Header("Background")]
        public Image targetBackgroundGraphic;
        public Sprite onBackground;
        public Sprite offBackground;
        [Header("Anchored Position")]
        public Vector2 onPosition;
        public Vector2 offPosition;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.OnValueChangedAsObservable().Subscribe(SwitchSpriteAndPosition).AddTo(this);
            SwitchSpriteAndPosition(_toggle.isOn);
        }

        private void SwitchSpriteAndPosition(bool on)
        {
            targetHandleGraphic.sprite = on ? onSprite : offSprite;
            targetBackgroundGraphic.sprite = on ? onBackground : offBackground;
            targetHandleGraphic.rectTransform.anchoredPosition = on ? onPosition : offPosition;;
        }
    }
}