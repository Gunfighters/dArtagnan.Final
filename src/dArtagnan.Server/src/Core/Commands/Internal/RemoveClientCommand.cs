using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 클라이언트 제거 명령 - 정상/비정상 종료 모두 통일된 방식으로 처리합니다
/// </summary>
public class RemoveClientCommand : IGameCommand
{
    required public int ClientId;
    required public ClientConnection? Client;
    required public bool IsNormalDisconnect; // 정상 종료 여부
    public bool ReportToLobby = true; // 로비 서버에 퇴장 보고 여부
    
    public async Task ExecuteAsync(GameManager gameManager)
    {
        var disconnectType = IsNormalDisconnect ? "정상 퇴장" : "비정상 종료";
        Logger.log($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 시작");

        Logger.log($"[DEBUG] RemoveClientInternal called for client {ClientId}");
        Logger.log($"[DEBUG] Current thread: {Thread.CurrentThread.ManagedThreadId}");
        Logger.log($"[DEBUG] Stack trace: {Environment.StackTrace}");

        // ============================================================
        // 관전자 퇴장 처리
        // ============================================================
        if (gameManager.Spectators.TryRemove(ClientId, out var spectator))
        {
            Logger.log($"[Game][{spectator.Nickname}] 관전자 퇴장 처리");

            gameManager.Clients.TryRemove(ClientId, out _);

            if (Client != null)
            {
                _ = Task.Run(() => Client.DisconnectAsync());
            }

            Logger.log($"[Game][{spectator.Nickname}] 관전자 제거 완료 (현재 관전자: {gameManager.Spectators.Count}, 접속자: {gameManager.Clients.Count})");
            return;
        }

        // ============================================================
        // 플레이어 퇴장 처리 (기존 로직)
        // ============================================================
        var player = gameManager.GetPlayerById(ClientId);

        if (player != null)
        {
            Logger.log($"[게임] 플레이어 {player.Id}({player.Nickname}) 퇴장 처리");

            // 로비서버에 플레이어 퇴장 보고
            if (ReportToLobby)
            {
                LobbyReporter.ReportPlayerLeave(player.ProviderId);
                Logger.log($"[Lobby][{player.Nickname}] 퇴장 보고 전송");
            }
            else
            {
                //애초에 접속이 안됨 처리
                Logger.log($"[Lobby][{player.Nickname}] 로비 보고 생략 (타임아웃 또는 중복 로그인)");
            }

            await gameManager.BroadcastToAllExcept(new LeaveBroadcast
            {
                PlayerId = player.Id
            }, ClientId);

            await gameManager.BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1, // 시스템 메시지
                Message = $"{player.Nickname}님이 퇴장했습니다"
            });

            // 게임 진행 중에 나간 플레이어는 게임 결과 전송을 위해 따로 저장
            if (gameManager.CurrentGameState != GameState.Waiting)
            {
                // 파산하지 않은 상태로 나가는 경우 퇴장 시점 기록
                if (!player.Bankrupt)
                {
                    player.BankruptTime = gameManager.GameElapsedTime; // 퇴장 시점을 파산 시점으로 기록
                    Logger.log($"[Player][{player.Nickname}] 게임 중 퇴장: 퇴장 시간 {player.BankruptTime:F2}초 기록");
                }

                gameManager.DisconnectedPlayers.Add(player);
            }
        }

        gameManager.Players.TryRemove(ClientId, out _);
        gameManager.Clients.TryRemove(ClientId, out _);
        LobbyReporter.ReportPlayerCount(gameManager.Clients.Count);

        if (player != null)
        {
            Logger.log($"[게임] 플레이어 {player.Id} 제거 완료 (현재 인원: {gameManager.Players.Count}, 접속자: {gameManager.Clients.Count})");
        }

        if (player == gameManager.Host)
        {
            var nextHost = gameManager.Players.Values.FirstOrDefault(p => p.Alive && p is not Bot);
            await gameManager.SetHost(nextHost);
        }
        await gameManager.CheckAndHandleGameEndAsync();
        
        // 연결 종료 (비동기로 처리)
        if (Client != null)
        {
            _ = Task.Run(() => Client.DisconnectAsync());
        }
        
        Logger.log($"[게임] 클라이언트 {ClientId} {disconnectType} 처리 완료");
    }
} 