using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.Controls
{
    public class ShootJoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public FixedJoystick shootingJoystick;
        public Image cooldownImage;
        public AudioSource reloadSound;
        public Image JoystickAxis;
        public Image HandleOutline;
        public Image Icon;

        private readonly Color _orange = Color.Lerp(Color.red, Color.yellow, 0.5f);
        private Image _localPlayerRangeCircle;
        private Slider _localPlayerReloadingSlider;
        private float ReloadProgress => 1 - GameModel.Instance.LocalPlayer.CurrentValue.RemainingReloadingTime.CurrentValue / GameModel.Instance.LocalPlayer.CurrentValue.TotalReloadingTime.CurrentValue;
        private bool Loaded => ReloadProgress >= 1;
        private bool _aiming;
        private bool _reloading;

        private void Start()
        {
            GameModel.Instance.LocalPlayer.Subscribe(newLocal =>
            {
                newLocal.Alive.Subscribe(newAlive =>
                {
                    if (!newAlive)
                    {
                        var targetModel = GameModel.Instance.GetPlayerModel(newLocal.Targeting.CurrentValue);
                        if (targetModel) targetModel.Highlighted.Value = false;
                    }
                });
                _localPlayerRangeCircle = newLocal.GetComponent<PlayerShoot>().rangeCircle;
                // _localPlayerReloadingSlider = newLocal.GetComponent<PlayerShoot>().reloadingSlider;
            });
        }

        private void Update()
        {
            if (GameModel.Instance.LocalPlayer is null) return;
            var newTargetModel = _aiming ? GameModel.Instance.LocalPlayer.CurrentValue.CalculateTarget(shootingJoystick.Direction) : null;
            if ((newTargetModel?.ID.CurrentValue ?? -1) != GameModel.Instance.LocalPlayer.CurrentValue.Targeting.CurrentValue)
            {
                var targetModel = GameModel.Instance.GetPlayerModel(GameModel.Instance.LocalPlayer.CurrentValue.Targeting.CurrentValue);
                if (targetModel) targetModel.Highlighted.Value = false;
                if (newTargetModel) newTargetModel.Highlighted.Value = true;
                GameModel.Instance.LocalPlayer.CurrentValue.Targeting.Value = newTargetModel?.ID.CurrentValue ?? -1;
                PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = GameModel.Instance.LocalPlayer.CurrentValue.Targeting.CurrentValue });
            }
            if (!Loaded)
                HandleOutline.color = Icon.color = Color.grey;
            else if (GameModel.Instance.LocalPlayer.CurrentValue.CalculateTarget(Vector2.zero) is null)
                HandleOutline.color = Icon.color = _orange;
            else
            {
                HandleOutline.color = Color.red;
                Icon.color = Color.white;
            }

            shootingJoystick.enabled = Loaded;
            cooldownImage.fillAmount = ReloadProgress;
            if (_reloading && Loaded)
            {
                reloadSound.Play();
                _reloading = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Loaded) return;
            JoystickAxis.enabled = true;
            _aiming = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            JoystickAxis.enabled = false;
            _aiming = false;
            if (!Loaded)
                AlertReloadingCircle().Forget();
            else if (GameModel.Instance.LocalPlayer.CurrentValue.Targeting.CurrentValue == -1)
                AlertCircle().Forget();
            else
            {
                GameModel.Instance.LocalPlayer.CurrentValue.Mining.Value = false;
                PacketChannel.Raise(new UpdateMiningStateFromClient { IsMining = false });
                PacketChannel.Raise(new ShootingFromClient { TargetId = GameModel.Instance.LocalPlayer.CurrentValue.Targeting.CurrentValue });
            }
        }

        private async UniTask AlertCircle()
        {
            _localPlayerRangeCircle.gameObject.SetActive(false);
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.gameObject.SetActive(true);
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.gameObject.SetActive(false);
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.gameObject.SetActive(true);
        }

        private async UniTask AlertReloadingCircle()
        {
            _localPlayerRangeCircle.enabled = false;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.enabled = true;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.enabled = false;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerRangeCircle.enabled = true;
        }

        private async UniTask AlertReloadingSlider()
        {
            var sliderBox = _localPlayerReloadingSlider.GetComponent<Image>();
            var sliderFill = _localPlayerReloadingSlider.GetComponentInChildren<Image>(true);
            _localPlayerReloadingSlider.enabled = sliderBox.enabled = sliderFill.enabled = false;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerReloadingSlider.enabled = sliderBox.enabled = sliderFill.enabled = true;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerReloadingSlider.enabled = sliderBox.enabled = sliderFill.enabled = false;
            await UniTask.WaitForSeconds(0.1f);
            _localPlayerReloadingSlider.enabled = sliderBox.enabled = sliderFill.enabled = true;
        }
    }
}