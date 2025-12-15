using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Game.Player.Components;
using UnityEngine;
using Utils;

namespace Game.Player.Animation
{
    public class LookDownBehaviour : StateMachineBehaviour
    {
        private Character4D _character;
        private PlayerCharacter _characterController;
        private PlayerModel _playerModel;
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            _character = animator.GetComponent<Character4D>();
            _characterController = animator.GetComponentInParent<PlayerCharacter>();
            _playerModel = _characterController.GetComponent<PlayerModel>();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            _character.SetDirection(_playerModel.Direction.CurrentValue.SnapToCardinalDirection());
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            _character.SetDirection(_playerModel.Mining.CurrentValue
                ? Vector2.down
                : _playerModel.Direction.CurrentValue.SnapToCardinalDirection());
        }
    }
}