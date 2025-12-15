using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 참가 명령 - 새로운 플레이어가 게임에 참가할 때 처리합니다
/// </summary>
public class PlayerJoinCommand : IGameCommand
{
    required public int ClientId;
    required public string SessionId;
    required public bool AsSpectator; // 관전자로 참가 여부
    required public ClientConnection Client;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        // ============================================================
        // 관전자 참가 처리
        // ============================================================
        if (AsSpectator)
        {
            // 관전자는 Waiting 상태에서만 참가 가능
            if (gameManager.CurrentGameState != GameState.Waiting)
            {
                Logger.log($"[Game][Client-{ClientId}] 관전자 참가 거부: 게임 진행 중 (현재 상태: {gameManager.CurrentGameState})");
                await new RemoveClientCommand
                {
                    ClientId = ClientId,
                    Client = Client,
                    IsNormalDisconnect = false,
                    ReportToLobby = false
                }.ExecuteAsync(gameManager);
                return;
            }

            Logger.log($"[Game][Client-{ClientId}] 관전자 참가 요청");

            string spectatorNickname;
            string spectatorProviderId;

            if (Program.DEV_MODE)
            {
                spectatorNickname = $"관전자{ClientId}";
                spectatorProviderId = $"dev:spectator_{ClientId}";
            }
            else
            {
                Logger.log($"[Game][Client-{ClientId}] 관전자 세션 검증 중...");
                var sessionData = await LobbyReporter.ValidateSessionAsync(SessionId);

                if (sessionData == null || !sessionData.Ok)
                {
                    Logger.log($"[Game][Client-{ClientId}] 관전자 참가 거부: 유효하지 않은 세션");
                    await new RemoveClientCommand
                    {
                        ClientId = ClientId,
                        Client = Client,
                        IsNormalDisconnect = false,
                        ReportToLobby = false
                    }.ExecuteAsync(gameManager);
                    return;
                }

                spectatorNickname = sessionData.Nickname;
                spectatorProviderId = sessionData.ProviderId;
                Logger.log($"[Game][Client-{ClientId}] 관전자 세션 검증 완료: {spectatorNickname}");
            }

            // ProviderId 기반 중복 체크 (재접속 시 유령 세션 제거)
            var existingSpectator = gameManager.Spectators.Values
                .FirstOrDefault(s => s.ProviderId == spectatorProviderId);

            if (existingSpectator != null)
            {
                Logger.log($"[Game][{spectatorNickname}] 중복 로그인 감지: 기존 관전자 세션 강제 종료 (ID={existingSpectator.Id})");
                await new RemoveClientCommand
                {
                    ClientId = existingSpectator.Id,
                    Client = gameManager.Clients.GetValueOrDefault(existingSpectator.Id),
                    IsNormalDisconnect = false,
                    ReportToLobby = false
                }.ExecuteAsync(gameManager);
            }

            // Spectators에 추가
            var spectator = new Spectator(ClientId, spectatorNickname, spectatorProviderId);
            gameManager.Spectators.TryAdd(ClientId, spectator);

            // ClientConnection에 Nickname 설정 (로그용)
            Client.Nickname = $"[관전]{spectatorNickname}";

            Logger.log($"[Game][{spectatorNickname}] 관전자 추가 완료: ID={ClientId}");

            // 맵 데이터 전송
            await gameManager.SendToPlayer(ClientId, new MapData { MapIndex = gameManager.MapIndex });

            // 관전자 응답 전송
            await gameManager.SendToPlayer(ClientId, new JoinResponseFromServer
            {
                PlayerId = ClientId,
                RoomName = gameManager.RoomName,
                RoomPassword = gameManager.RoomPassword,
                IsSpectator = true
            });

            // 현재 게임 상태 전송 (Waiting 상태만 가능)
            await gameManager.SendToPlayer(ClientId, new WaitingStartFromServer
            {
                PlayersInfo = gameManager.PlayersInRoom()
            });
            await gameManager.SendToPlayer(ClientId, new NewHostBroadcast { HostId = gameManager.Host?.Id ?? 0 });

            Logger.log($"[Game][{spectatorNickname}] 관전자 참가 완료");
            return;
        }

        // ============================================================
        // 일반 플레이어 참가 처리 (기존 로직)
        // ============================================================
        if (gameManager.CurrentGameState != GameState.Waiting)
        {
            Logger.log($"[Game][Client-{ClientId}] 참가 거부: 게임 진행 중");
            await new RemoveClientCommand
            {
                ClientId = ClientId,
                Client = Client,
                IsNormalDisconnect = false,
                ReportToLobby = false
            }.ExecuteAsync(gameManager);
            return;
        }

        if (gameManager.Players.Count >= Constants.MAX_PLAYER_COUNT)
        {
            Logger.log($"[Game][Client-{ClientId}] 참가 거부: 최대 플레이어 수 초과");
            await new RemoveClientCommand
            {
                ClientId = ClientId,
                Client = Client,
                IsNormalDisconnect = false,
                ReportToLobby = false
            }.ExecuteAsync(gameManager);
            return;
        }

        // ClientId 기반 중복 체크 (혹시 모를 예외 상황 대비)
        var existingPlayer = gameManager.GetPlayerById(ClientId);
        if (existingPlayer != null)
        {
            Logger.log($"[Game][Client-{ClientId}] 참가 거부: 이미 존재하는 플레이어");
            await new RemoveClientCommand
            {
                ClientId = ClientId,
                Client = Client,
                IsNormalDisconnect = false,
                ReportToLobby = false
            }.ExecuteAsync(gameManager);
            return;
        }

        string nickname;
        string providerId;
        Dictionary<string, string> equippedCostumes;

        if (Program.DEV_MODE)
        {
            // Dev 모드: 로비 서버 없이 단독 실행 시
            Logger.log($"[Game][Client-{ClientId}] Dev 모드: 세션 검증 스킵, 테스트 데이터 사용");
            nickname = "테스트플레이어";
            providerId = $"dev:test_{ClientId}";
            equippedCostumes = new Dictionary<string, string>
            {
                ["Helmet"] = "MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]",
                ["Vest"] = "MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]",
                ["Bracers"] = "MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]",
                ["Leggings"] = "MilitaryHeroes.Basic.Armor.Sheriff [ShowEars]",
                ["Firearm1H"] = "MilitaryHeroes.Basic.Firearm1H.Peacemaker",
                ["Body"] = "Common.Basic.Body.Type4",
                ["Body_Paint"] = "FFC878FF",
                ["Hair"] = "Common.Basic.Hair.Default",
                ["Hair_Paint"] = "963200FF",
                ["Beard"] = "None",
                ["Beard_Paint"] = "963200FF",
                ["Eyebrows"] = "Common.Basic.Eyebrows.Default",
                ["Eyes"] = "Common.Basic.Eyes.Boy",
                ["Eyes_Paint"] = "00C8FFFF",
                ["Ears"] = "Common.Basic.Ears.Human",
                ["Mouth"] = "Common.Basic.Mouth.Default",
                ["Makeup"] = "None",
                ["Mask"] = "None",
                ["Earrings"] = "None"
            };
        }
        else
        {
            Logger.log($"[Game][Client-{ClientId}] 참가 요청: 세션 검증 중...");

            // 세션 토큰으로 로비 서버에서 사용자 정보 조회
            var sessionData = await LobbyReporter.ValidateSessionAsync(SessionId);

            if (sessionData == null || !sessionData.Ok)
            {
                Logger.log($"[Game][Client-{ClientId}] 참가 거부: 유효하지 않은 세션");
                await new RemoveClientCommand
                {
                    ClientId = ClientId,
                    Client = Client,
                    IsNormalDisconnect = false,
                    ReportToLobby = false
                }.ExecuteAsync(gameManager);
                return;
            }

            nickname = sessionData.Nickname;
            providerId = sessionData.ProviderId;
            equippedCostumes = sessionData.EquippedCostumes;
            Logger.log($"[Game][Client-{ClientId}] 세션 검증 완료: {nickname}");
        }

        // ProviderId 기반 중복 체크 (재접속 시 유령 세션 제거)
        var existingPlayerByProviderId = gameManager.Players.Values
            .FirstOrDefault(p => p.ProviderId == providerId);

        if (existingPlayerByProviderId != null)
        {
            Logger.log($"[Game][{nickname}] 중복 로그인 감지: 기존 세션 강제 종료 (ID={existingPlayerByProviderId.Id})");

            // ReportToLobby=false로 설정하여 로비 보고 생략 (진짜 세션의 currentRoom 보호)
            var removeCommand = new RemoveClientCommand
            {
                ClientId = existingPlayerByProviderId.Id,
                Client = gameManager.Clients.GetValueOrDefault(existingPlayerByProviderId.Id),
                IsNormalDisconnect = false,
                ReportToLobby = false  // 로비 보고 생략
            };
            await removeCommand.ExecuteAsync(gameManager);
        }

        Logger.log($"[Game][Client-{ClientId}] 맵 데이터 전송 중");
        await gameManager.SendToPlayer(ClientId, new MapData { MapIndex = gameManager.MapIndex });

        // 새 플레이어 생성
        var player = await gameManager.AddPlayer(ClientId, nickname, providerId, equippedCostumes);

        // ClientConnection에 Nickname 설정 (로그용)
        Client.Nickname = player.Nickname;

        // 로비서버에 플레이어 입장 보고
        LobbyReporter.ReportPlayerJoin(providerId);

        Logger.log($"[Game][{player.Nickname}] 플레이어 생성 완료: ID={player.Id}");
        
        // 클라이언트에게 플레이어 ID, 방 이름, 비밀번호 전송
        await gameManager.SendToPlayer(ClientId, new JoinResponseFromServer
        {
            PlayerId = player.Id,
            RoomName = gameManager.RoomName,
            RoomPassword = gameManager.RoomPassword,
            IsSpectator = false
        });
        
        // 현재 게임 상태 전송
        var waitingPacket = new WaitingStartFromServer 
        { 
            PlayersInfo = gameManager.PlayersInRoom() 
        };
        await gameManager.SendToPlayer(ClientId, waitingPacket);

        await gameManager.BroadcastToAll(new NewHostBroadcast { HostId = gameManager.Host?.Id ?? 0 });
        
        Logger.log($"[Game][{player.Nickname}] 참가 완료: 현재 인원 {gameManager.Players.Count}명");
    }
} 