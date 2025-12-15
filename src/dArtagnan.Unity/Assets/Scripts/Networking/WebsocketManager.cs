using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Assets.HeroEditor4D.InventorySystem.Scripts.Data;
using Audio;
using Costume;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using JetBrains.Annotations;
using Lobby;
using Lobby.LobbyAlertMessage;
using Newtonsoft.Json;
using ObservableCollections;
using R3;
using UnityEngine;
using UnityEngine.Purchasing;
using Utils;

namespace Networking
{
    public class WebsocketManager : MonoBehaviour
    {
        public static WebsocketManager Instance { get; private set; }

        [CanBeNull] private ClientWebSocket _ws;
        [CanBeNull] private CancellationTokenSource _cts;
        private readonly Channel<string> _channel = Channel.CreateSingleConsumerUnbounded<string>();
        [Header("Identity")]
        public readonly Subject<bool> AuthSuccess = new();
        public SerializableReactiveProperty<string> nickname;
        public SerializableReactiveProperty<string> providerId;
        public SerializableReactiveProperty<bool> isNewUser;
        public SerializableReactiveProperty<bool> showTutorial = new();
        public readonly Subject<bool> NicknameSetSuccessful = new();
        public readonly Subject<string> NicknameSetFailureReason = new();
        [Header("Resource")]
        public SerializableReactiveProperty<int> crystal;
        public SerializableReactiveProperty<int> gold;
        public SerializableReactiveProperty<int> silver;
        public SerializableReactiveProperty<int> level;
        public SerializableReactiveProperty<int> currentExp;
        public SerializableReactiveProperty<int> expToNextLevel;
        public readonly ObservableDictionary<EquipmentPart, ItemSprite> CurrentEquipments = new();
        public readonly ObservableDictionary<BodyPart, ItemSprite> CurrentBodyParts = new();
        public readonly ObservableDictionary<Paint, Color> CurrentPaints = new();
        public readonly ObservableDictionary<EquipmentPart, ItemSprite[]> OwnedEquipments = new();
        public readonly ObservableDictionary<BodyPart, ItemSprite[]> OwnedBodyParts = new();
        public SerializableReactiveProperty<Color[]> OwnedPaints;
        public readonly ReactiveProperty<Dictionary<string, ShopConstants>> ShopConstantsDict = new();
        public readonly Subject<bool> OwnedChanged = new();
        [Header("Session")]
        public SerializableReactiveProperty<string> gameSessionToken;
        public bool joinAsSpectator;
        [Header("Room")]
        public SerializableReactiveProperty<RoomInfo[]> roomList;
        public readonly Subject<CreateRoomResponseMessage> CreateRoomSuccess = new();
        public readonly Subject<JoinRoomResponseMessage> JoinRoomSuccess = new();
        public SerializableReactiveProperty<string> roomName;
        public SerializableReactiveProperty<string> roomId;
        public SerializableReactiveProperty<string> password;
        [Header("Error")]
        public SerializableReactiveProperty<string> errorOccured;
        [Header("References")]
        public SpriteCollection spriteCollection;
        public event Action ConnectionClosed;
        public SerializableReactiveProperty<ShopBuyPartRouletteResponse> onShopBuyParRouletteResponse;
        public SerializableReactiveProperty<ShopBuyPartDirectResponse> shopBuyPartDirectResponse;
        
        public readonly ObservableQueue<NotificationMessage> Notifications = new();
        
        private void Awake()
        {
            if (Instance)
                Destroy(Instance.gameObject);
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateRoomSuccess.Subscribe(res =>
            {
                roomName.Value = res.roomName;
                roomId.Value = res.roomId;
                password.Value = res.password;
            });
            JoinRoomSuccess.Where(res => res.ok).Subscribe(res =>
            {
                roomName.Value = res.roomName;
                roomId.Value = res.roomId;
                password.Value = res.password;
            });
            OwnedEquipments.ObserveChanged().Subscribe(_ => OwnedChanged.OnNext(true));
            OwnedBodyParts.ObserveChanged().Subscribe(_ => OwnedChanged.OnNext(true));
            OwnedPaints.Subscribe(_ => OwnedChanged.OnNext(true));
        }

        public bool Owns(EquipmentPart part, Item item) => OwnedEquipments[part].Any(e => e?.Id == item?.Id);
        
        public bool Owns(BodyPart part, Item item) => OwnedBodyParts[part].Any(e => e?.Id == item?.Id);

        public bool Owns(EquipmentPart part, string id) => OwnedEquipments[part].Any(e => e?.Id == id);
        public bool Owns(BodyPart part, string id) => OwnedBodyParts[part].Any(e => e?.Id == id);

        public bool Owns(EquipmentPart part, ItemSprite sprite) => OwnedEquipments[part].Contains(sprite);
        public bool Owns(BodyPart part, ItemSprite sprite) => OwnedBodyParts[part].Contains(sprite);

        public bool Owns(Color color)
        {
            return OwnedPaints.CurrentValue.Contains(color);
        }

        private void Update()
        {
            while (_channel.Reader.TryRead(out var msg))
            {
                Handle(msg);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public async UniTask Connect(string endpoint, string sessionID)
        {
            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            var wsUrl = endpoint
                .Replace("http://", "ws://")
                .Replace("https://", "wss://");
            await _ws.ConnectAsync(new Uri(wsUrl), _cts!.Token);
            var authMessage = new AuthMessage { type = "auth", sessionId = sessionID };
            await SendWebSocketMessage(JsonUtility.ToJson(authMessage));
            UniTask.RunOnThreadPool(StartListeningLoop).Forget();
        }

        private async UniTask SendWebSocketMessage(string message)
        {
            if (_ws!.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _ws.SendAsync(buffer, WebSocketMessageType.Text, true, _cts!.Token);
            }
        }

        private async UniTask StartListeningLoop()
        {
            try
            {
                var messageBuilder = new List<byte>();
                var buffer = new byte[4096];
                while (_ws!.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
                {
                    messageBuilder.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(buffer, _cts.Token);
                        for (var i = 0; i < result.Count; i++)
                            messageBuilder.Add(buffer[i]);
                    } while (!result.EndOfMessage);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                        _channel.Writer.TryWrite(Encoding.UTF8.GetString(messageBuilder.ToArray()));
                }
            }
            catch (Exception e)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogError($"Error while listening on websocket: {e.Message}");
                ConnectionClosed?.Invoke();
            }
        }

        private void Handle(string message)
        {
            var parsed = JsonConvert.DeserializeObject<MessageType>(message);
            switch (parsed.type)
            {
                case "update_experience":
                    var resp = JsonConvert.DeserializeObject<UpdateExperienceMessage>(message);
                    level.Value = resp.level;
                    currentExp.Value = resp.currentExp;
                    expToNextLevel.Value = resp.expToNextLevel;
                    break;
                case "auth_success":
                    var authResponse = JsonConvert.DeserializeObject<AuthSuccessMessage>(message);
                    if (authResponse.ok)
                    {
                        nickname.Value = authResponse.nickname;
                        providerId.Value = authResponse.providerId;
                        isNewUser.Value = authResponse.isNewUser;
                        showTutorial.Value = authResponse.isNewUser;
                        gold.Value = authResponse.gold;
                        crystal.Value = authResponse.crystal;
                        silver.Value = authResponse.silver;
                        level.Value = authResponse.level;
                        expToNextLevel.Value = authResponse.expToNextLevel;
                        currentExp.Value = authResponse.currentExp;
                        gameSessionToken.Value = authResponse.gameSessionToken;
                        ShopConstantsDict.Value = authResponse.shopConstants;
                        foreach (var pair in authResponse.ownedCostumes)
                        {
                            if (Enum.GetNames(typeof(EquipmentPart)).Contains(pair.Key))
                            {
                                var arr = pair.Value
                                    .Select(i => spriteCollection.AllSprites.FirstOrDefault(s => s.Id == i))
                                    .ToArray();
                                OwnedEquipments[pair.Key.StringToEquipmentPart()] = arr;
                            }
                            else if (Enum.GetNames(typeof(BodyPart)).Contains(pair.Key))
                            {
                                var arr = pair.Value
                                    .Select(i => spriteCollection.AllSprites.FirstOrDefault(s => s.Id == i))
                                    .ToArray();
                                OwnedBodyParts[pair.Key.StringToBodyPart()] = arr;
                            }
                            else if (pair.Key == "Paint")
                            {
                                var colorArr = pair.Value.Select(i =>
                                {
                                    if (!i.StartsWith('#'))
                                        i = '#' + i;
                                    
                                    if (!ColorUtility.TryParseHtmlString(i, out var c))
                                    {
                                        throw new Exception($"Invalid color: {i}");
                                    }
                                    return c;
                                }).ToArray();
                                OwnedPaints.Value = colorArr;
                            }
                            else
                                throw new ArgumentOutOfRangeException(nameof(pair.Key), pair.Key, null);
                        }
                        foreach (var pair in authResponse.equippedCostumes)
                        {
                            if (Enum.GetNames(typeof(EquipmentPart)).Contains(pair.Key))
                            {
                                var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == pair.Value);
                                CurrentEquipments[pair.Key.StringToEquipmentPart()] = found;
                            }
                            else if (Enum.GetNames(typeof(BodyPart)).Contains(pair.Key))
                            {
                                var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == pair.Value);
                                CurrentBodyParts[pair.Key.StringToBodyPart()] = found;
                            }
                            else if (Enum.GetNames(typeof(Paint)).Contains(pair.Key))
                            {
                                var a = pair.Value;
                                if (!a.StartsWith('#'))
                                    a = '#' + a;
                                if (!ColorUtility.TryParseHtmlString(a, out var c))
                                {
                                    throw new Exception("Invalid color: " + a);
                                }
                                CurrentPaints[pair.Key.StringToPaint()] = c;
                            }
                            else
                                throw new ArgumentOutOfRangeException(nameof(pair.Key), pair.Key, null);
                        }
                    }
                    else
                    {
                        errorOccured.OnNext($"로비 서버 인증에 실패했습니다. {authResponse}");
                    }
                    AuthSuccess.OnNext(authResponse.ok);
                    break;
                case "create_room_response":
                    var createResponse = JsonConvert.DeserializeObject<CreateRoomResponseMessage>(message);
                    if (createResponse.ok)
                        CreateRoomSuccess.OnNext(createResponse);
                    else
                        LobbyAlertMessageModel.Instance.Text.OnNext(createResponse.message);
                    break;
                case "join_room_response":
                    var joinResponse = JsonConvert.DeserializeObject<JoinRoomResponseMessage>(message);
                    JoinRoomSuccess.OnNext(joinResponse);
                    break;
                case "nickname_set":
                    var nicknameSetResponse = JsonConvert.DeserializeObject<NicknameSetResponse>(message);
                    if (nicknameSetResponse.ok)
                    {
                        nickname.Value = nicknameSetResponse.nickname;
                        isNewUser.Value = false;
                        NicknameSetSuccessful.OnNext(true);
                    }
                    else
                    {
                        NicknameSetSuccessful.OnNext(false);
                        NicknameSetFailureReason.OnNext(nicknameSetResponse.message);
                    }
                    break;
                case "rooms_update":
                    var roomsUpdateResponse = JsonConvert.DeserializeObject<RoomsUpdateMessage>(message);
                    Array.Sort(roomsUpdateResponse.rooms, (a, b) => -a.joinable.CompareTo(b.joinable));
                    roomList.Value = roomsUpdateResponse.rooms;
                    break;
                case "update_crystal":
                    var updateCrystalResponse = JsonConvert.DeserializeObject<UpdateCrystalMessage>(message);
                    crystal.Value = updateCrystalResponse.crystal;
                    break;
                case "update_gold":
                    var updateGoldResponse = JsonConvert.DeserializeObject<UpdateGoldMessage>(message);
                    gold.Value = updateGoldResponse.gold;
                    break;
                case "update_silver":
                    var updateSilverResponse = JsonConvert.DeserializeObject<UpdateSilverMessage>(message);
                    silver.Value = updateSilverResponse.silver;
                    break;
                case "update_inventory":
                    var updateInventoryResponse = JsonConvert.DeserializeObject<UpdateInventoryMessage>(message);
                    foreach (var pair in updateInventoryResponse.ownedCostumes)
                    {

                        if (Enum.GetNames(typeof(EquipmentPart)).Contains(pair.Key))
                        {
                            var found = pair.Value
                                .Select(i => spriteCollection.AllSprites.FirstOrDefault(s => s.Id == i))
                                .ToArray();
                            OwnedEquipments[pair.Key.StringToEquipmentPart()] = found;
                        }
                        else if (Enum.GetNames(typeof(BodyPart)).Contains(pair.Key))
                        {
                            var found = pair.Value
                                .Select(i => spriteCollection.AllSprites.FirstOrDefault(s => s.Id == i))
                                .ToArray();
                            OwnedBodyParts[pair.Key.StringToBodyPart()] = found;
                        }
                        else if (pair.Key == "Paint")
                        {
                            var colorArr = pair.Value.Select(i =>
                            {
                                if (!i.StartsWith('#'))
                                    i = '#' + i;
                                if (!ColorUtility.TryParseHtmlString(i, out var c))
                                {
                                    throw new Exception($"Invalid color: {i}");
                                }
                                return c;
                            }).ToArray();
                            OwnedPaints.Value = colorArr;
                        }
                        else
                            throw new ArgumentOutOfRangeException(nameof(pair.Key), pair.Key, null);
                    }
                    break;
                case "change_costume_response":
                    var response = JsonConvert.DeserializeObject<ChangeCostumeResponse>(message);
                    if (response.ok)
                    {
                        if (Enum.GetNames(typeof(EquipmentPart)).Contains(response.partType))
                        {
                            var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == response.costumeId);
                            CurrentEquipments[response.partType.StringToEquipmentPart()] = found;
                        }
                        else if (Enum.GetNames(typeof(BodyPart)).Contains(response.partType))
                        {
                            var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == response.costumeId);
                            CurrentBodyParts[response.partType.StringToBodyPart()] = found;
                        }
                        else if (Enum.GetNames(typeof(Paint)).Contains(response.partType))
                        {
                            if (!response.costumeId.StartsWith('#'))
                                response.costumeId = '#' + response.costumeId;
                            if (!ColorUtility.TryParseHtmlString(response.costumeId, out var c))
                            {
                                throw new Exception($"Invalid color: {response.costumeId}");
                            }
                            CurrentPaints[response.partType.StringToPaint()] = c;
                        }
                        else
                            throw new ArgumentOutOfRangeException(nameof(response.partType), response.partType, null);
                    }
                    else
                        LobbyAlertMessageModel.Instance.Text.OnNext(response.message);
                    break;
                case "update_equipped_costumes":
                    var updateEquippedCostumesMessage = JsonConvert.DeserializeObject<UpdateEquippedCostumesMessage>(message);
                    foreach (var pair in updateEquippedCostumesMessage.equippedCostumes)
                    {
                        if (Enum.GetNames(typeof(EquipmentPart)).Contains(pair.Key))
                        {
                            var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == pair.Value);
                            CurrentEquipments[pair.Key.StringToEquipmentPart()] = found;
                        }
                        else if (Enum.GetNames(typeof(BodyPart)).Contains(pair.Key))
                        {
                            var found = spriteCollection.AllSprites.FirstOrDefault(s => s.Id == pair.Value);
                            CurrentBodyParts[pair.Key.StringToBodyPart()] = found;
                        }
                        else if (Enum.GetNames(typeof(Paint)).Contains(pair.Key))
                        {
                            var s = pair.Value;
                            if (!s.StartsWith('#'))
                                s = '#' + s;
                            if (!ColorUtility.TryParseHtmlString(s, out var c))
                            {
                                throw new Exception($"Invalid color: {s}");
                            }
                            CurrentPaints[pair.Key.StringToPaint()] = c;
                        }
                        else
                            throw new ArgumentOutOfRangeException(nameof(pair.Key), pair.Key, null);
                    }
                    break;
                case "shop_buy_part_roulette_response":
                    var rouletteResponse = JsonConvert.DeserializeObject<ShopBuyPartRouletteResponse>(message);
                    onShopBuyParRouletteResponse.Value = rouletteResponse;
                    if (!rouletteResponse.ok)
                        LobbyAlertMessageModel.Instance.Text.OnNext(rouletteResponse.message);
                    break;
                case "update_nickname":
                    var nicknameUpdate = JsonConvert.DeserializeObject<UpdateNicknameMessage>(message);
                    nickname.Value = nicknameUpdate.nickname;
                    break;
                case "shop_buy_part_direct_response":
                    var designatedPurchaseResponse = JsonConvert.DeserializeObject<ShopBuyPartDirectResponse>(message);
                    shopBuyPartDirectResponse.Value = designatedPurchaseResponse;
                    if (!designatedPurchaseResponse.ok)
                        LobbyAlertMessageModel.Instance.Text.OnNext(designatedPurchaseResponse.message);
                    break;
                case "iap_purchase_response":
                    var iapPurchaseResponse = JsonConvert.DeserializeObject<IAPPurchaseResponse>(message);
                    if (!iapPurchaseResponse.ok)
                        LobbyAlertMessageModel.Instance.Text.OnNext($"보석 구매에 실패했습니다.");
                    else
                        AudioClipPlayer.Instance.Play(AudioClipType.Purchased);
                    break;
                case "error":
                    var errorMessage = JsonConvert.DeserializeObject<ErrorMessage>(message);

                    // errorType 기반 처리 (나중에 특정 타입별 UI 추가 가능)
                    switch (errorMessage.errorType)
                    {
                        // GENERIC이거나 아직 정의하지 않은 타입은 화면에 메시지 표시
                        default:
                            LobbyAlertMessageModel.Instance.Text.OnNext(errorMessage.message);
                            break;
                    }
                    break;
                case "notification":
                    var noti = JsonConvert.DeserializeObject<NotificationMessage>(message);
                    Notifications.Enqueue(noti);
                    Debug.Log(message);
                    break;
                default:
                    throw new Exception($"Unknown message type: {parsed.type}");
            }
        }

        public async UniTask CreateRoom(string roomName, bool isPrivate)
        {
            var msg = new CreateRoomMessage { type = "create_room", roomName = roomName, hasPassword = isPrivate }; // TODO
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask JoinRoom(string roomID = null, string password = null)
        {
            var msg = new JoinRoomMessage { type = "join_room", roomId = roomID, password = password };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask SubmitNickname(string submitted)
        {
            var msg = new NicknameSubmission { nickname = submitted };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask ChangeCostume(EquipmentPart part, string id)
        {
            var msg = new ChangeCostumeMessage { partType = part.ToString(), costumeId = id };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }
        
        public async UniTask ChangeCostume(BodyPart part, string id)
        {
            var msg = new ChangeCostumeMessage { partType = part.ToString(), costumeId = id };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask PurchaseCostumePartDesignated(EquipmentPart part, string id)
        {
            var msg = new ShopBuyPartDirectMessage { partType = part.ToString(), costumeId = id, currency = "silver" };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }
        
        public async UniTask PurchaseCostumePartDesignated(BodyPart part, string id)
        {
            var msg = new ShopBuyPartDirectMessage { partType = part.ToString(), costumeId = id, currency = "silver" };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask PurchaseCostumeRoulette(EquipmentPart part, string currency)
        {
            var msg = new ShopBuyPartRouletteMessage { partType = part.ToString(), currency = currency };
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }
        
        public async UniTask PurchaseCostumeRoulette(BodyPart part, string currency)
        {
            var msg = new ShopBuyPartRouletteMessage { partType = part.ToString(), currency = currency};
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        public async UniTask PurchaseIAP(string productId, string store, string payload, string transactionId)
        {
            var msg = new IAPPurchaseMessage { productId = productId, payload = payload }; // TODO
            var serialized = JsonUtility.ToJson(msg);
            await SendWebSocketMessage(serialized);
        }

        private void Disconnect()
        {
            _cts?.Cancel();
            _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _ws?.Dispose();
            _cts?.Dispose();
            _ws = null;
            _cts = null;
        }
    }
}