using System;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using Lobby;
using UnityEngine;

namespace Networking
{
    public class NetworkManager : MonoBehaviour
    {
        private readonly Channel<IPacket> _channel = Channel.CreateSingleConsumerUnbounded<IPacket>();
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;

        private void Awake()
        {
            PacketChannel.On<MovementDataFromClient>(Send);
            PacketChannel.On<ShootingFromClient>(Send);
            PacketChannel.On<PlayerIsTargetingFromClient>(Send);
            PacketChannel.On<StartGameFromClient>(Send);
            PacketChannel.On<UpdateAccuracyStateFromClient>(Send);
            PacketChannel.On<ChatFromClient>(Send);
            PacketChannel.On<InitialRouletteDoneFromClient>(Send);
            PacketChannel.On<UpdateMiningStateFromClient>(Send);
            PacketChannel.On<PurchaseItemFromClient>(Send);
            PacketChannel.On<ShopRouletteFromClient>(Send);
            PacketChannel.On<UpdateRoomNameFromClient>(Send);
            PacketChannel.On<GameResultsBroadcast>(e =>
            {
                foreach (var r in e.Rankings)
                {
                    Debug.Log($"{r.PlayerId}: {r.Rank} / {r.RewardMoney}");
                }
            });
            if (WebsocketManager.Instance)
                WebsocketManager.Instance.ConnectionClosed += NotifyGameModelOfWebsocketClosed;
        }

        private void NotifyGameModelOfWebsocketClosed()
        {
            GameModel.Instance.ConnectionFailure.OnNext(true);
        }

        private void Start()
        {
            Connect(GameLobbyParameter.GameServerIP, GameLobbyParameter.GameServerPort);
        }

        private void Update()
        {
            while (_channel.Reader.TryRead(out var packet))
            {
                PacketChannel.Raise(packet);
            }
        }

        private void OnDestroy()
        {
            _isConnected = false;
            PacketChannel.Clear();
            _stream?.Close();
            _client?.Close();
            if (WebsocketManager.Instance)
                WebsocketManager.Instance.ConnectionClosed -= NotifyGameModelOfWebsocketClosed;
        }

        private void Connect(string host, int port)
        {
            ConnectToServer(host, port)
                .ContinueWith(StartListeningLoop)
                .Forget();
        }

        private async UniTask ConnectToServer(string host, int port)
        {
            _client = new TcpClient { NoDelay = true };
            try
            {
                await _client.ConnectAsync(host, port).AsUniTask();
            }
            catch (Exception e)
            {
                GameModel.Instance.ConnectionFailure?.OnNext(true);
                Debug.LogException(e);
                return;
            }
            _stream = _client.GetStream();
            _isConnected = true;
            Send(new JoinRequest {
                SessionId = WebsocketManager.Instance?.gameSessionToken.CurrentValue ?? "",
                AsSpectator = WebsocketManager.Instance?.joinAsSpectator ?? false
            });

            // 연결 성공 후 Ping 루프 시작
            StartPingLoop().Forget();
        }

        private void Send<T>(T payload) where T : IPacket
        {
            try
            {
                NetworkUtils.SendPacketSync(_stream, payload);
            }
            catch
            {
                _isConnected = false;
                GameModel.Instance.ConnectionFailure?.OnNext(true);
            }
        }

        private async UniTask StartListeningLoop()
        {
            await UniTask.SwitchToThreadPool();
            try
            {
                while (true)
                {
                    var packet = NetworkUtils.ReceivePacketSync(_stream);
                    _channel.Writer.TryWrite(packet);
                }
            }
            catch
            {
                _isConnected = false;
                await UniTask.SwitchToMainThread();
                GameModel.Instance.ConnectionFailure?.OnNext(true);
            }
        }

        private async UniTask StartPingLoop()
        {
            // 서버 타임아웃의 절반 주기로 Ping 전송
            const int pingInterval = Constants.CONNECTION_TIMEOUT_SECONDS / 2;

            while (_isConnected)
            {
                Send(new PingPacket());
                await UniTask.WaitForSeconds(pingInterval);
                if (!_isConnected) break;
            }
        }
    }
}