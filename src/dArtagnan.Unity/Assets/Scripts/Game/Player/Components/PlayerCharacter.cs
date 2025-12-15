using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using R3;
using R3.Triggers;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerCharacter : MonoBehaviour
    {
        private PlayerModel _model;
        public Character4D character;
        public SpriteCollection spriteCollection;
        public FirearmCollection firearmCollection;
        private ReadOnlyReactiveProperty<string> firearmName;
        private PlayerModel _target;
        public Vector2 DirectionTowardTarget => (_target.transform.position - transform.position).SnapToCardinalDirection();

        private void Awake()
        {
            _model = GetComponent<PlayerModel>();
            _model.Direction.Select(d => d.SnapToCardinalDirection()).Subscribe(character.SetDirection);
            _model.Alive.Subscribe(yes => character.SetExpression(yes ? "Default" : "Dead"));
            _model.Direction
                .Select(yes => yes != Vector2.zero)
                .CombineLatest(_model.Alive, (moving, alive) => new { moving, alive })
                .Subscribe(ops =>
                {
                    if (!ops.alive)
                        character.AnimationManager.SetState(CharacterState.Death);
                    else if (_model.Mining.CurrentValue)
                        return;
                    else if (ops.moving)
                        character.AnimationManager.SetState(CharacterState.Walk);
                    else
                        character.AnimationManager.SetState(CharacterState.Idle);
                });
            _model.Fire.Subscribe(info =>
            {
                _target = info.Target;
                character.AnimationManager.Fire();
                character.CreateFirearmFx(firearmName.CurrentValue);
            });
            _model.CurrentEquipments.WhereNotNull()
                .CombineLatest(_model.CurrentBodyParts.WhereNotNull(), (e, b) => new { e, b })
                .CombineLatest(_model.Paints.WhereNotNull(), (ops, p) => new { ops.e, ops.b, p })
                .Subscribe(ops => character.UpdateCostumeByData(ops.b, ops.p, ops.e));
            firearmName = _model.CurrentEquipments
                .Select(e => e[EquipmentPart.Firearm1H].Name)
                .ToReadOnlyReactiveProperty();
            var shovelItemSprite = spriteCollection.MeleeWeapon2H.First(i => i.Name == "Shovel");
            _model.Mining.Subscribe(yes =>
            {
                character.AnimationManager.SetState(yes ? CharacterState.Mine : CharacterState.Idle);
                if (yes)
                {
                    character.Equip(shovelItemSprite, EquipmentPart.MeleeWeapon2H);
                }
                else if (_model.CurrentEquipments.CurrentValue is not null)
                {
                    character.Equip(_model.CurrentEquipments.CurrentValue[EquipmentPart.Firearm1H], EquipmentPart.Firearm1H);
                }
            });
        }
    }
}