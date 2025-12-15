using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.HUD.Controls
{
    public class MovementJoystick : MonoBehaviour
    {
        private VariableJoystick _variableJoystick;
        private bool _keyboardDisabled;
        private bool Moving => _variableJoystick.Direction != Vector2.zero;
        private Vector2 InputVectorSnapped => _variableJoystick.Direction.DirectionToInt().IntToDirection();
        private PlayerModel LocalPlayer => GameModel.Instance.LocalPlayer.CurrentValue;

        private void Awake() => _variableJoystick = GetComponent<VariableJoystick>();

        private void Start()
        {
            GameModel.Instance.RoundWinners.Subscribe(_ =>
            {
                _variableJoystick.OnPointerUp(new PointerEventData(EventSystem.current));
                _variableJoystick.gameObject.SetActive(false);
            });
            GameModel.Instance.State
                .Subscribe(s => _variableJoystick.gameObject.SetActive(s == GameState.Round || s == GameState.Waiting));
            GameModel.Instance.LocalPlayer.Subscribe(local =>
            {
                local.Mining.Subscribe(yes =>
                {
                    if (yes)
                    {
                        _variableJoystick.OnPointerUp(new PointerEventData(EventSystem.current));
                        _keyboardDisabled = true;
                    }
                });
            });
        }

        private void Update()
        {
            if (!LocalPlayer) return;
            var newDirection = GetInputDirection();
            if (newDirection == LocalPlayer.Direction.CurrentValue) return;
            LocalPlayer.Direction.Value = newDirection.normalized;
            PacketChannel.Raise(LocalPlayer.GetMovementDataFromClient());
        }

        private void OnDisable() => _variableJoystick.OnPointerUp(new PointerEventData(EventSystem.current));

        private Vector2 GetInputDirection()
        {
            return Moving ? InputVectorSnapped.normalized : GetKeyboardVector();
        }

        private Vector2 GetKeyboardVector()
        {
            var direction = Vector2.zero;
            if (Input.GetKeyDown(KeyCode.A)
                || Input.GetKeyDown(KeyCode.D)
                || Input.GetKeyDown(KeyCode.S)
                || Input.GetKeyDown(KeyCode.W))
            {
                _keyboardDisabled = false;
                GameModel.Instance.LocalPlayer.CurrentValue.Mining.Value = false;
                PacketChannel.Raise(new UpdateMiningStateFromClient { IsMining = false });
            }
            if (_keyboardDisabled) return Vector2.zero;
            if (Input.GetKey(KeyCode.W)) direction += Vector2.up;
            if (Input.GetKey(KeyCode.S)) direction += Vector2.down;
            if (Input.GetKey(KeyCode.A)) direction += Vector2.left;
            if (Input.GetKey(KeyCode.D)) direction += Vector2.right;
            return direction.normalized;
        }
    }
}