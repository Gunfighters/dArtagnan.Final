using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Assets.HeroEditor4D.InventorySystem.Scripts.Data;
using Costume;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game.Misc;
using Game.Player.Data;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerModel : MonoBehaviour
    {
        [Header("Identity")]
        public SerializableReactiveProperty<int> ID;
        public SerializableReactiveProperty<string> Nickname;
        public TextMeshProUGUI nicknameText;
        public ColorPool colorPool;
        [Header("Health")]
        public SerializableReactiveProperty<bool> Alive;
        [Header("Physics")]
        public SerializableReactiveProperty<Vector2> PositionFromServer;
        public SerializableReactiveProperty<Vector2> Direction;
        public float Speed;
        public float LastServerPositionUpdateTimestamp;
        public bool NeedToCorrect;
        [Header("Shooting")]
        public SerializableReactiveProperty<int> Accuracy;
        public SerializableReactiveProperty<int> AccuracyState;
        public SerializableReactiveProperty<float> TotalReloadingTime;
        public SerializableReactiveProperty<float> RemainingReloadingTime;
        public SerializableReactiveProperty<int> Targeting;
        public SerializableReactiveProperty<float> Range;
        [Header("Monetary")]
        public SerializableReactiveProperty<int> Balance;
        public SerializableReactiveProperty<int> NextDeductionAmount;
        public SerializableReactiveProperty<bool> Bankrupt;
        [Header("Minecraft")]
        public SerializableReactiveProperty<bool> Mining;
        [Header("Etc")]
        public SerializableReactiveProperty<bool> Highlighted;
        public SerializableReactiveProperty<bool> Fury;
        public SpriteCollection spriteCollection;
        public readonly Subject<ItemUsedBroadcast> ItemTriggeredOnThisPlayer = new(); 
        public ReadOnlyReactiveProperty<bool> HideAccuracyAndRange { get; private set; }

        public event Action Initialized;
        public readonly ReactiveProperty<Dictionary<BodyPart, ItemSprite>> CurrentBodyParts = new();
        public readonly ReactiveProperty<Dictionary<Paint, Color>> Paints = new();
        public readonly ReactiveProperty<Dictionary<EquipmentPart, ItemSprite>> CurrentEquipments = new();
        public SerializableReactiveProperty<ItemId[]> OwnedItems;
        public readonly Subject<FireInfo> Fire = new();
        private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[20];
        public readonly Subject<int> MiningComplete = new();

        public Color Color => colorPool.colors[ID.CurrentValue % colorPool.colors.Count];

        private void Awake()
        {
            Nickname.Subscribe(newName => gameObject.name = newName);
            Nickname.SubscribeToText(nicknameText);
            HideAccuracyAndRange = OwnedItems
                .Select(items => items.Contains(ItemId.HideAccuracy))
                .ToReadOnlyReactiveProperty();
            Alive.Subscribe(_ => Direction.Value = Vector2.zero);
            Observable
                .Interval(TimeSpan.FromSeconds(0.5f))
                .TakeWhile(_ => this == GameModel.Instance.LocalPlayer.CurrentValue && Alive.CurrentValue)
                .Subscribe(_ => PacketChannel.Raise(GetMovementDataFromClient()))
                .AddTo(this);
            this
                .OnCollisionStay2DAsObservable()
                .TakeWhile(_ => this == GameModel.Instance.LocalPlayer.CurrentValue && Alive.CurrentValue)
                .ThrottleFirstFrame(10)
                .Subscribe(_ => PacketChannel.Raise(GetMovementDataFromClient()))
                .AddTo(this);
            GameModel.Instance.State.Subscribe(_ => Direction.Value = Vector2.zero).AddTo(this);
        }

        public void Initialize(PlayerInformation info)
        {
            ID.Value = info.PlayerId;
            Nickname.Value = info.Nickname;
            Accuracy.Value = info.Accuracy;
            AccuracyState.Value = info.AccuracyState;
            Alive.Value = info.Alive;
            Targeting.Value = info.Targeting;
            Range.Value = info.Range;
            transform.position = PositionFromServer.Value = info.MovementData.Position.ToUnityVec();
            Direction.Value = info.MovementData.Direction.IntToDirection().normalized;
            Speed = info.MovementData.Speed;
            NeedToCorrect = true;
            LastServerPositionUpdateTimestamp = Time.time;
            Balance.Value = info.Balance;
            OwnedItems.Value = info.OwnedItems.ToArray();
            RemainingReloadingTime.Value = info.RemainingReloadTime;
            TotalReloadingTime.Value = info.TotalReloadTime;
            NextDeductionAmount.Value = info.NextDeductionAmount;
            Mining.Value = info.IsMining;
            Fury.Value = info.Fury;
            var parsed = info.EquippedCostumes.ParseAppearanceInformation(spriteCollection);
            CurrentEquipments.Value = parsed.equipments;
            CurrentBodyParts.Value = parsed.bodyParts;
            Paints.Value = parsed.paints;
            Initialized?.Invoke();
        }

        public MovementDataFromClient GetMovementDataFromClient() => new()
        {
            MovementData =
            {
                Direction = Direction.CurrentValue.DirectionToInt(),
                Position = transform.position.ToSystemVec(),
                Speed = Speed,
            }
        };

        private bool CanShoot(PlayerModel target)
        {
            if (Vector2.Distance(target.transform.position, transform.position) > Range.CurrentValue) return false;
            var origin = transform.position;
            var direction = target.transform.position - origin;
            Physics2D.RaycastNonAlloc(origin, direction, _raycastHits, Range.CurrentValue, LayerMask.GetMask("Player", "Obstacle"));
            return _raycastHits.First(h => h.transform != transform).transform == target.transform;
        }
        
        [CanBeNull]
        public PlayerModel CalculateTarget(Vector2 aim)
        {
            var targetPool = GameModel.Instance
                .Survivors
                .Where(target => target != this)
                .Where(CanShoot)
                .ToArray();
            if (!targetPool.Any()) return null;
            if (aim == Vector2.zero)
                return targetPool
                    .Aggregate((a, b) =>
                        Vector2.Distance(a.transform.position, transform.position)
                        < Vector2.Distance(b.transform.position, transform.position)
                            ? a
                            : b);
            return targetPool
                .Aggregate((a, b) =>
                    Vector2.Angle(aim, a.transform.position - transform.position)
                    < Vector2.Angle(aim, b.transform.position - transform.position)
                        ? a
                        : b);
        }
    }
}