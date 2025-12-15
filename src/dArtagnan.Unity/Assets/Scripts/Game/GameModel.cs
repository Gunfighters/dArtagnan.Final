using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Audio;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game.Player.Components;
using Game.Player.Data;
using JetBrains.Annotations;
using Lobby;
using Networking;
using ObservableCollections;
using R3;
using UnityEngine;
using Utils;
#if UNITY_IOS && !UNITY_EDITOR
using Apple.CoreHaptics;
#endif

namespace Game
{
    public class GameModel : MonoBehaviour
    {
        public static GameModel Instance { get; private set; }
        [Header("State")]
        public SerializableReactiveProperty<GameState> State;
        [Header("Round")]
        public SerializableReactiveProperty<int> Round;
        public SerializableReactiveProperty<int> Stakes;
        public SerializableReactiveProperty<string> RoomName;
        public SerializableReactiveProperty<float> progressToDeduction;
        public SerializableReactiveProperty<float> deductionPeriod;
        [Header("Players")]
        public SerializableReactiveProperty<int> HostPlayerId;
        public SerializableReactiveProperty<int> LocalPlayerId;
        public ReadOnlyReactiveProperty<PlayerModel> LocalPlayer { get; private set; }
        public ReadOnlyReactiveProperty<PlayerModel> HostPlayer { get; private set; }
        [Header("Roulette")]
        public SerializableReactiveProperty<int> TargetAccuracy;
        public SerializableReactiveProperty<int> AccuracyResetCost;
        [Header("Misc")]
        public SerializableReactiveProperty<bool> IsPrivateRoom;
        public SerializableReactiveProperty<string> Password;
        public SerializableReactiveProperty<PlayerModel> CameraTarget;
        public SerializableReactiveProperty<float> RemainingShopTime;
        public SerializableReactiveProperty<float> MaxShopTime;
        public SerializableReactiveProperty<string> NonTemporaryMessage;
        public SerializableReactiveProperty<bool> inShopWhileLocalIsBankrupt;
        public SerializableReactiveProperty<PlayerRanking[]> GameWinners;
        public readonly Subject<bool> PlayerBalanceUpdated = new();
        public Settings settings;
        public readonly List<int> mutedPlayers = new();
        public SerializableReactiveProperty<int> BaseTax;

        private PlayerModel _localPlayerKilledBy;

        public readonly Subject<(PlayerModel winner, int reward)> ShowVictory = new();
        public readonly Subject<bool> GiftBoxResult = new();
        public readonly Subject<ChatBroadcast> NewChatBroadcast = new();
        public readonly Subject<RoundWinnerBroadcast> RoundWinners = new();
        public readonly ObservableList<int> AccuracyPool = new();
        public readonly ObservableList<ShopItem> ItemPool = new();
        public readonly ObservableDictionary<int, PlayerModel> PlayerModels = new();
        public readonly Subject<bool> ConnectionFailure = new();
        public readonly Subject<(PlayerModel left, bool middle, PlayerModel right)> NewKillLogInformation = new();
        public readonly Subject<PlayerModel> NewBankruptInformation = new();
        public readonly Subject<(PlayerModel rewarded, int amount)> NewMineRewardInformation = new();
        public readonly Subject<int> SpinShopRoulette = new();
        #if UNITY_IOS && !UNITY_EDITOR
        private readonly CHHapticEngine _hapticEngine = new();
        #endif
        public IEnumerable<PlayerModel> Survivors =>
            PlayerModels
                .Where(pair => pair.Value.Alive.CurrentValue)
                .Select(pair => pair.Value);

        [CanBeNull]
        public PlayerModel GetPlayerModel(int id) => PlayerModels.GetValueOrDefault(id, null);

        private void Awake()
        {
            if (Instance && Instance != this)
                Destroy(Instance.gameObject);
            Instance = this;
            LocalPlayer = LocalPlayerId
                .CombineLatest(PlayerModels.ObserveAdd(), (newLocalId, _) => newLocalId)
                .Select(GetPlayerModel)
                .WhereNotNull()
                .ToReadOnlyReactiveProperty();
            HostPlayer = HostPlayerId
                .CombineLatest(PlayerModels.ObserveAdd(), (newHostId, _) => newHostId)
                .Select(GetPlayerModel)
                .WhereNotNull()
                .ToReadOnlyReactiveProperty();
            LocalPlayer
                .WhereNotNull()
                .Select(p => p.Alive)
                .Subscribe(reactiveAlive => reactiveAlive.Subscribe(newAlive =>
                {
                    if (newAlive)
                        CameraTarget.Value = LocalPlayer.CurrentValue;
                }));
            if (WebsocketManager.Instance?.joinAsSpectator ?? false)
                HostPlayer.WhereNotNull().Subscribe(newHost => CameraTarget.Value = newHost);
            deductionPeriod.Value = Constants.BETTING_PERIOD;
            State.Subscribe(s => Debug.Log(s)).AddTo(this);
            State.Where(s => s is GameState.Round or GameState.Waiting).Subscribe(_ => inShopWhileLocalIsBankrupt.Value = false);
            State.Where(s => s == GameState.Round).Subscribe(_ =>
            {
                progressToDeduction.Value = Stakes.Value = 0;
                _localPlayerKilledBy = null;
            });
            RoundWinners.Subscribe(e =>
            {
                if (e.PlayerIds.Count > 0)
                {
                    var winner = GetPlayerModel(e.PlayerIds[0])!;
                    winner.GetComponent<PlayerBalance>().PlayPrizeAnimation(e.PrizeMoney);
                    winner.Balance.Value = e.WinnerBalanceAfter;
                }
            });
            WebsocketManager.Instance?.password.Subscribe(p =>
            {
                IsPrivateRoom.Value = p != "";
                Password.Value = p;
            });
            PacketChannel.On<JoinBroadcast>(OnJoin);
            PacketChannel.On<JoinResponseFromServer>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<UpdateRoomNameBroadcast>(e => RoomName.Value = e.RoomName);
            PacketChannel.On<LeaveBroadcast>(e => RemovePlayer(e.PlayerId));
            PacketChannel.On<WaitingStartFromServer>(e => UpdatePlayerModelsByInfoList(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => UpdatePlayerModelsByInfoList(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(_ => State.Value = GameState.Round);
            PacketChannel.On<RoundStartFromServer>(e => Round.Value = e.Round);
            PacketChannel.On<WaitingStartFromServer>(_ => State.Value = GameState.Waiting);
            PacketChannel.On<InitialRouletteStartFromServer>(_ => State.Value = GameState.InitialRoulette);
            PacketChannel.On<MovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<ShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
            PacketChannel.On<UpdateAccuracyStateBroadcast>(OnAccuracyStateBroadcast);
            PacketChannel.On<UpdateAccuracyBroadcast>(OnAccuracyUpdate);
            PacketChannel.On<UpdateRangeBroadcast>(OnRangeUpdate);
            PacketChannel.On<UpdateSpeedBroadcast>(OnSpeedUpdate);
            PacketChannel.On<UpdateOwnedItemsBroadcast>(OnOwnedItemsUpdate);
            PacketChannel.On<InitialRouletteStartFromServer>(OnInitialRoulette);
            PacketChannel.On<ShopStartFromServer>(OnShopStart);
            PacketChannel.On<ReloadTimeBroadcast>(OnReloadTimeBroadcast);
            PacketChannel.On<MiningStateBroadcast>(OnMiningState);
            PacketChannel.On<ShopRouletteResultFromServer>(OnShopRouletteResult);
            PacketChannel.On<ShopDataUpdateFromServer>(OnShopDataUpdate);
            PacketChannel.On<RoundWinnerBroadcast>(RoundWinners.OnNext);
            PacketChannel.On<LootBroadcast2>(OnLootBroadcast);
            PacketChannel.On<MineResultBroadcast>(OnMineResultBroadcast);
            PacketChannel.On<TaxBroadcast>(OnTaxBroadcast);
            PacketChannel.On<ShopTimeElapsed>(e =>
            {
                RemainingShopTime.Value = MaxShopTime.CurrentValue - e.ElapsedTime;
                if ((int)(MaxShopTime.CurrentValue - e.ElapsedTime) < 5)
                    AudioClipPlayer.Instance.Play(AudioClipType.CountDown);
            });
            PacketChannel.On<StakesUpdateBroadcast>(OnStakesUpdateBroadcast);
            PacketChannel.On<GameWinnerBroadcast>(e =>
            {
                ShowVictory.OnNext((GetPlayerModel(e.WinnerId), e.RewardMoney));
            });
            PacketChannel.On<GameResultsBroadcast>(e =>
            {
                GameWinners.Value = e.Rankings.ToArray();
            });
            PacketChannel.On<FuryStateBroadcast>(OnFuryBroadcast);
            PacketChannel.On<ItemUsedBroadcast>(OnItemUsed);
            PacketChannel.On<BankruptBroadcast>(OnBankrupt);
            PacketChannel.On<UpdatePlayerNextTaxAmount>(OnNextTaxAmount);
            PacketChannel.On<PlayerBalanceUpdate>(OnPlayerBalanceUpdate);
            PacketChannel.On<ChatBroadcast>(NewChatBroadcast.OnNext);
            PacketChannel.On<MovementDataFromClient>(e =>
            {
                Debug.Log($"Sending: {e.MovementData.Position}");
            });
            if (settings.haptic.CurrentValue)
                SetupHaptics();
        }

        private void Update()
        {
            switch (State.CurrentValue)
            {
                case GameState.Shop:
                    RemainingShopTime.Value -= Time.deltaTime;
                    RemainingShopTime.Value = Mathf.Max(0, RemainingShopTime.CurrentValue);
                    break;
                case GameState.Round:
                    progressToDeduction.Value += Time.deltaTime;
                    progressToDeduction.Value = Mathf.Min(progressToDeduction.CurrentValue, deductionPeriod.CurrentValue);
                    break;
            }
        }

        private void UpdatePlayerModelsByInfoList(List<PlayerInformation> list)
        {
            list.ForEach(info =>
            {
                if (PlayerModels.TryGetValue(info.PlayerId, out var model))
                    model.Initialize(info);
                else
                    CreatePlayer(info);
            });
            foreach (var pair in PlayerModels
                         .ToImmutableArray()
                         .Where(pair => !list.Exists(info => info.PlayerId == pair.Key)))
            {
                RemovePlayer(pair.Key);
            }
        }

        private void OnJoin(JoinBroadcast e)
        {
            CreatePlayer(e.PlayerInfo);
        }

        private void OnYouAre(JoinResponseFromServer e)
        {
            LocalPlayerId.Value = e.PlayerId;
            RoomName.Value = e.RoomName;
        }

        private void OnNewHost(NewHostBroadcast e)
        {
            HostPlayerId.Value = e.HostId;
        }

        private void CreatePlayer(PlayerInformation info)
        {
            if (PlayerModels.ContainsKey(info.PlayerId)) return;
            var view = PlayerPoolManager.Instance.Pool.Get();
            var model = view.GetComponent<PlayerModel>();
            model.Initialize(info);
            PlayerModels.Add(info.PlayerId, model);
        }

        private void RemovePlayer(int playerId)
        {
            if (PlayerModels.Remove(playerId, out var model))
                PlayerPoolManager.Instance.Pool.Release(model);
        }

        private void OnPlayerMovementData(MovementDataBroadcast e)
        {
            var targetPlayer = GetPlayerModel(e.PlayerId)!;
            if (targetPlayer == LocalPlayer.CurrentValue && LocalPlayer.CurrentValue.Alive.CurrentValue) return;
            Debug.Log($"OnMovementData: {targetPlayer.Nickname}");
            targetPlayer.PositionFromServer.Value = e.MovementData.Position.ToUnityVec();
            targetPlayer.Direction.Value = e.MovementData.Direction.IntToDirection();
            targetPlayer.Speed = e.MovementData.Speed;
            targetPlayer.LastServerPositionUpdateTimestamp = Time.time;
            targetPlayer.NeedToCorrect = true;
        }

        private void OnPlayerIsTargeting(PlayerIsTargetingBroadcast e)
        {
            var aiming = GetPlayerModel(e.ShooterId)!;
            if (aiming == LocalPlayer.CurrentValue) return;
            aiming.Targeting.Value = e.TargetId;
        }

        private void OnPlayerShoot(ShootingBroadcast e)
        {
            var shooter = GetPlayerModel(e.ShooterId)!;
            var target = GetPlayerModel(e.TargetId)!; 
            shooter.Fire.OnNext(new FireInfo { Hit = e.Hit, Target = target });
            if (e.Hit)
                NewKillLogInformation.OnNext((shooter, !e.Guarded, target)); // TODO: send whether guard was effective.
            if (e.Hit && target == LocalPlayer.CurrentValue)
                _localPlayerKilledBy = shooter;
        }

        private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Alive.Value = e.Alive;
            if (!updated.Alive.CurrentValue && updated == CameraTarget.CurrentValue)
                UniTask
                    .WaitForSeconds(1f)
                    .ContinueWith(() => CameraTarget.Value = CameraTarget.CurrentValue == LocalPlayer.CurrentValue && _localPlayerKilledBy ? _localPlayerKilledBy : PlayerModels.FirstOrDefault(p => p.Value.Alive.CurrentValue).Value);
        }

        private void OnAccuracyStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            GetPlayerModel(e.PlayerId)!.AccuracyState.Value = e.AccuracyState;
        }

        private void OnAccuracyUpdate(UpdateAccuracyBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId);
            updated!.Accuracy.Value = e.Accuracy;
        }

        private void OnRangeUpdate(UpdateRangeBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Range.Value = e.Range;
        }

        private void OnSpeedUpdate(UpdateSpeedBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Speed = e.Speed;
        }

        private void OnOwnedItemsUpdate(UpdateOwnedItemsBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.OwnedItems.Value = e.OwnedItems.ToArray();
        }

        private void OnInitialRoulette(InitialRouletteStartFromServer e)
        {
            AccuracyPool.Clear();
            AccuracyPool.AddRange(e.AccuracyPool);
            TargetAccuracy.Value = e.YourAccuracy;
        }

        private void OnShopStart(ShopStartFromServer e)
        {
            inShopWhileLocalIsBankrupt.Value = WebsocketManager.Instance?.joinAsSpectator ?? e.YouAreBankrupt;
            State.Value = GameState.Shop;
            if (!inShopWhileLocalIsBankrupt.CurrentValue)
            {
                AccuracyPool.Clear();
                AccuracyPool.AddRange(e.ShopData.ShopRoulettePool);
                AccuracyResetCost.Value = e.ShopData.ShopRoulettePrice;
                ItemPool.Clear();
                ItemPool.AddRange(e.ShopData.YourItems);
            }
            MaxShopTime.Value = e.Duration;
            RemainingShopTime.Value = e.Duration;
        }

        private void OnShopDataUpdate(ShopDataUpdateFromServer e)
        {
            AccuracyPool.Clear();
            AccuracyPool.AddRange(e.ShopData.ShopRoulettePool);
            AccuracyResetCost.Value = e.ShopData.ShopRoulettePrice;
            ItemPool.Clear();
            ItemPool.AddRange(e.ShopData.YourItems);
        }

        private void OnShopRouletteResult(ShopRouletteResultFromServer e)
        {
            TargetAccuracy.Value = e.NewAccuracy;
            AccuracyResetCost.Value = e.ShopRoulettePrice;
            SpinShopRoulette.OnNext(TargetAccuracy.CurrentValue);
        }

        private void OnReloadTimeBroadcast(ReloadTimeBroadcast e)
        {
            var reloading = GetPlayerModel(e.PlayerId)!;
            reloading.TotalReloadingTime.Value = e.TotalReloadTime;
            reloading.RemainingReloadingTime.Value = e.RemainingReloadTime;
        }

        private void OnMiningState(MiningStateBroadcast e)
        {
            var mining = GetPlayerModel(e.PlayerId)!;
            mining.Mining.Value = e.IsMining;
        }

        private void OnFuryBroadcast(FuryStateBroadcast e)
        {
            GetPlayerModel(e.PlayerId)!.Fury.Value = e.Fury;
        }

        private void OnItemUsed(ItemUsedBroadcast e)
        {
            GetPlayerModel(e.PlayerId)!.ItemTriggeredOnThisPlayer.OnNext(e);
        }

        private void OnBankrupt(BankruptBroadcast e)
        {
            var bankrupt = GetPlayerModel(e.PlayerId)!;
            bankrupt.Bankrupt.Value = true;
            NewBankruptInformation.OnNext(bankrupt);
        }

        private void OnNextTaxAmount(UpdatePlayerNextTaxAmount e)
        {
            if (e.PlayerId == -1)
                BaseTax.Value = e.TaxAmount;
            else
            {
                var taxed = GetPlayerModel(e.PlayerId)!;
                taxed.NextDeductionAmount.Value = e.TaxAmount;
            }
        }

        private void OnLootBroadcast(LootBroadcast2 e)
        {
            var fromPlayer = GetPlayerModel(e.FromPlayerId)!.GetComponent<PlayerBalance>();
            var toPlayer = GetPlayerModel(e.ToPlayerId)!.GetComponent<PlayerBalance>();
            
            toPlayer.PlayLootAnimation(fromPlayer, e.FromPlayerBalanceBefore, e.LootAmount, e.VanishedAmount);
            fromPlayer.GetComponent<PlayerModel>().Balance.Value = e.FromPlayerBalanceAfter;
            toPlayer.GetComponent<PlayerModel>().Balance.Value = e.ToPlayerBalanceAfter;
        }

        private void OnMineResultBroadcast(MineResultBroadcast e)
        {
            var player = GetPlayerModel(e.PlayerId)!;
            player.GetComponent<PlayerBalance>().PlayMineAnimation(e.RewardAmount);
            player.Balance.Value = e.PlayerBalanceAfter;
            player.MiningComplete.OnNext(e.RewardAmount);
            NewMineRewardInformation.OnNext((player, e.RewardAmount));
        }

        private void OnTaxBroadcast(TaxBroadcast e)
        {
            var player = GetPlayerModel(e.PlayerId)!;
            player.GetComponent<PlayerBalance>().PlayTaxAnimation(e.TaxAmount, e.PlayerBalanceBefore, e.TaxAmount);
            player.Balance.Value = e.PlayerBalanceAfter;
        }

        private void OnStakesUpdateBroadcast(StakesUpdateBroadcast e)
        {
            Stakes.Value = e.StakeAfter;
            progressToDeduction.Value = 0;
        }

        private void OnPlayerBalanceUpdate(PlayerBalanceUpdate e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Balance.Value = e.BalanceAfter;
            PlayerBalanceUpdated.OnNext(true);
        }

        private void SetupHaptics()
        {
            #if UNITY_IOS && !UNITY_EDITOR
            var events = new List<CHHapticEvent>
            {
                new CHHapticTransientEvent
                {
                    Time = 0f,
                    EventParameters = new List<CHHapticEventParameter>
                        {
                            new(CHHapticEventParameterID.HapticIntensity, 1f),
                            new(CHHapticEventParameterID.HapticSharpness, 1f),
                        }
                }
            };
            var pattern = new CHHapticPattern(events);
            var hapticPlayer = _hapticEngine.MakePlayer(pattern);
            _hapticEngine.Start();
            PacketChannel.On<UpdatePlayerAlive>(e =>
            {
                if (e.PlayerId == LocalPlayerId.CurrentValue && !e.Alive)
                    hapticPlayer.Start();
            });
            PacketChannel.On<ShootingBroadcast>(e =>
            {
                if (e.Hit && e.ShooterId == LocalPlayerId.CurrentValue)
                    hapticPlayer.Start();
            });
            #endif
        }

        private void OnDestroy()
        {
            #if UNITY_IOS && !UNITY_EDITOR
            if (settings.haptic.CurrentValue)
            _hapticEngine.Stop();
            #endif
        }
    }
}