using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using dArtagnan.Shared;
using Newtonsoft.Json;

namespace dArtagnan.Server;

/// <summary>
/// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
/// 커맨드에서 공통으로 쓰이는 유틸함수들을 모아둔다.
/// 나머지 게임로직은 커맨드에서 직접 처리한다.
/// </summary>
public class GameManager
{
    public const float EMPTY_SERVER_TIMEOUT = 10f;
    public int MapIndex = 1;
    public int[,] CollisionMap;
    public readonly Dictionary<Cell, bool> SpawnPointOnRoundStart = new();
    public Cell SpawnPointOnRoomEntry = new(0, 0);

    // 커맨드 시스템
    private readonly Channel<IGameCommand> _commandQueue = Channel.CreateUnbounded<IGameCommand>(
        new UnboundedChannelOptions
        {
            SingleReader = true, // 단일 소비자
            SingleWriter = false // 다중 생산자
        });


    public readonly ConcurrentDictionary<int, ClientConnection> Clients = new();

    // 방 정보
    public string RoomName { get; private set; } = string.Empty;
    public string RoomPassword { get; private set; } = string.Empty;
    public readonly ConcurrentDictionary<int, Player> Players = new();
    public readonly ConcurrentDictionary<int, Spectator> Spectators = new(); // 관전자 (읽기 전용)
    public readonly List<Player> DisconnectedPlayers = new(); // 게임 진행 중 나간 플레이어 (게임 결과 전송용)
    public HashSet<int> initialRouletteCompletedPlayers = []; // 이니셜 룰렛을 완료한 플레이어 ID
    public float InitialRouletteTimer = 0f; // 이니셜 룰렛 타이머
    public float ShopTimer = 0f; // 상점 지속시간 타이머
    public int BaseBettingAmount => Round > 0 && Round <= Constants.MAX_ROUNDS ? Constants.BETTING_AMOUNTS[Round - 1] : 0;
    public float BettingTimer = 0f; // 베팅금 차감 타이머 constants.BETTING_PERIOD 마다
    public int DeductionCount = 0; // 현재 라운드 베팅금 징수 횟수
    public GameState CurrentGameState = GameState.Waiting;

    // 서버 종료 타이머
    public float emptyServerTimer = 0f;
    public Player? Host;

    public int Round = 0;
    public float GameElapsedTime = 0f; // 게임 시작(InitialRoulette)부터 경과 시간

    // 베팅금/판돈 시스템
    public int TotalPrizeMoney = 0; // 총 판돈

    public GameManager()
    {
        _ = Task.Run(ProcessCommandsAsync);
        SetupMapData();
        TestCanShootAt();

        // 환경변수에서 방 이름 및 비밀번호 읽기
        var roomName = Environment.GetEnvironmentVariable("ROOM_NAME");
        RoomName = string.IsNullOrWhiteSpace(roomName) ? "Room" : roomName;

        var roomPassword = Environment.GetEnvironmentVariable("ROOM_PASSWORD");
        RoomPassword = roomPassword ?? string.Empty;

        Logger.log($"[Game][System] 방 이름 초기화: {RoomName}{(string.IsNullOrEmpty(RoomPassword) ? "" : " (비밀방)")}");
    }

    private void SetupMapData()
    {
        CollisionMap = GetMapResource(MapIndex, MapTileType.Collision);
        var spawnPointMap = GetMapResource(MapIndex, MapTileType.SpawnPointOnRoundStart);
        var spawnPointOnRoomEntryMap = GetMapResource(MapIndex, MapTileType.SpawnPointOnRoomEntry);
        var width = spawnPointMap.GetLength(0);
        var height = spawnPointMap.GetLength(1);
        for (var i = 0; i < width; i++)
        for (var j = 0; j < height; j++)
        {
            if (spawnPointMap[i, j] == 1)
                SpawnPointOnRoundStart[new Cell(i, j)] = false;
            if (spawnPointOnRoomEntryMap[i, j] == 1)
                SpawnPointOnRoomEntry = new Cell(i, j);
        }
    }
    private static int[,] GetMapResource(int index, MapTileType type)
    {
        var asm = Assembly.GetExecutingAssembly();
        var firstName = $"Map{index}";
        var middleName = type switch
        {
            MapTileType.Collision => "CollisionMap",
            MapTileType.SpawnPointOnRoomEntry => "SpawnPointOnRoomEntryMap",
            MapTileType.SpawnPointOnRoundStart => "SpawnPointOnRoundStartMap",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        const string lastName = "json";
        var resourceName = firstName + "." + middleName + "." + lastName;
        using var stream = asm.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var result = reader.ReadToEnd();
        return JsonConvert.DeserializeObject<int[,]>(result)!;
    }

    public bool CanMoveFromToAdjacent(Cell a, Cell b)
    {
        var c = new Cell(a.X, b.Y);
        var d = new Cell(b.X, a.Y);
        return CollisionMap[a.X, b.Y] == 0
               && CollisionMap[b.X, b.Y] == 0
               && CollisionMap[c.X, c.Y] == 0
               && CollisionMap[d.X, d.Y] == 0;
    }

    /// <summary>
    /// DDA 알고리즘을 이용한 충돌 판정 함수.
    /// </summary>
    /// <param name="from">시점</param>
    /// <param name="to">종점</param>
    /// <returns>시점에서 종점으로 충돌 없이 선을 그릴 수 있는지 여부. 그릴 수 있으면 True, 아니면 False</returns>
    public bool CanShootAt(Vector2 from, Vector2 to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        
        int x = (int)Math.Floor(from.X);
        int y = (int)Math.Floor(from.Y);
        
        int endX = (int)Math.Floor(to.X);
        int endY = (int)Math.Floor(to.Y);
        
        int stepX = dx >= 0 ? 1 : -1;
        int stepY = dy >= 0 ? 1 : -1;
        
        float tDeltaX = Math.Abs(dx) < 1e-6f ? float.MaxValue : Math.Abs(1.0f / dx);
        float tDeltaY = Math.Abs(dy) < 1e-6f ? float.MaxValue : Math.Abs(1.0f / dy);
        
        float tMaxX = tDeltaX * (stepX > 0 ? (x + 1 - from.X) : (from.X - x));
        float tMaxY = tDeltaY * (stepY > 0 ? (y + 1 - from.Y) : (from.Y - y));
        
        while (x != endX || y != endY)
        {
            if (x >= 0 && x < CollisionMap.GetLength(0) && y >= 0 && y < CollisionMap.GetLength(1))
            {
                if (CollisionMap[x, y] == 1)
                {
                    return false;
                }
            }
            
            if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                x += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                y += stepY;
            }
        }
        
        if (x >= 0 && x < CollisionMap.GetLength(0) && y >= 0 && y < CollisionMap.GetLength(1))
        {
            if (CollisionMap[x, y] == 1)
            {
                return false;
            }
        }
        
        return true;
    }

    private void TestCanShootAt()
    {
        Logger.log("[Game][System] CanShootAt 함수 테스트 시작");

        // 테스트용 간단한 맵 생성 (5x5)
        var testMap = new[,]
        {
            { 0, 0, 0, 0, 0 },
            { 0, 1, 0, 1, 0 },
            { 0, 0, 0, 0, 0 },
            { 0, 1, 0, 1, 0 },
            { 0, 0, 0, 0, 0 }
        };

        var originalMap = CollisionMap;
        CollisionMap = testMap;

        try
        {
            var r = CanShootAt(new Vector2(0.1f, 0.1f), new Vector2(1.2f, 4.4f));
            Logger.log($"[Game][System] 테스트 결과: {r} == False");
            r = CanShootAt(new Vector2(0, 0), new Vector2(0, 4));
            Logger.log($"[Game][System] 테스트 결과: {r} == True");
            r = CanShootAt(new Vector2(0, 0), new Vector2(0.5f, 0.5f));
            Logger.log($"[Game][System] 테스트 결과: {r} == True");
            r = CanShootAt(new Vector2(0, 0), new Vector2(4, 0.9f));
            Logger.log($"[Game][System] 테스트 결과: {r} == True");
            r = CanShootAt(new Vector2(0, 0), new Vector2(4, 1.2f));
            Logger.log($"[Game][System] 테스트 결과: {r} == False");
        }
        finally
        {
            CollisionMap = originalMap;
        }
    }
    
    /// <summary>
    /// Command를 큐에 추가하는 메서드
    /// </summary>
    public async Task EnqueueCommandAsync(IGameCommand command)
    {
        await _commandQueue.Writer.WriteAsync(command);
    }

    /// <summary>
    /// Command Queue 처리 루프
    /// </summary>
    private async Task ProcessCommandsAsync()
    {
        await foreach (var command in _commandQueue.Reader.ReadAllAsync())
        {
            try
            {
                await command.ExecuteAsync(this);
            }
            catch (Exception ex)
            {
                Logger.log($"[Game][System] 커맨드 실행 오류: {ex.Message}");
                Logger.log($"[Game][System] 스택 추적: {ex.StackTrace ?? "정보 없음"}");
            }
        }
    }

    internal int GetNextAvailableId()
    {
        int id = 1;
        while (Clients.ContainsKey(id) || Players.ContainsKey(id))
        {
            id++;
        }

        return id;
    }


    public async Task SetHost(Player? player)
    {
        Host = player;
        Logger.log($"[Game][{Host?.Nickname ?? "System"}] 방장 설정: ID={Host?.Id}");
        if (Host != null)
        {
            await BroadcastToAll(new NewHostBroadcast { HostId = Host.Id });
        }
    }

    public async Task UpdateRoomName(string newRoomName)
    {
        var oldRoomName = RoomName;
        RoomName = newRoomName;
        Logger.log($"[Game][System] 방 이름 변경: \"{oldRoomName}\" → \"{newRoomName}\"");

        // 같은 방 모든 플레이어에게 브로드캐스트
        await BroadcastToAll(new UpdateRoomNameBroadcast { RoomName = newRoomName });

        // 로비 서버에 리포트
        LobbyReporter.ReportRoomName(newRoomName);
    }

    public async Task<Player> AddPlayer(int clientId, string nickname, string providerId, Dictionary<string, string> equippedCostumes)
    {
        var player = new Player(clientId, nickname, providerId, equippedCostumes, SpawnPointOnRoomEntry.ToVec(), this);
        await player.InitToWaiting();
        Players.TryAdd(player.Id, player);
        if (Host == null)
        {
            await SetHost(player);
        }

        // 다른 플레이어들에게 새 플레이어 참가 알림
        await BroadcastToAllExcept(new JoinBroadcast
        {
            PlayerInfo = player.PlayerInformation
        }, clientId);

        // 입장 시스템 메시지 브로드캐스트
        await BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1, // 시스템 메시지
            Message = $"{nickname}님이 입장했습니다"
        });

        return player;
    }

    /// <summary>
    /// 봇을 생성하고 게임에 추가합니다
    /// </summary>
    public async Task<Bot> AddBot(int botId, string nickname)
    {
        var bot = new Bot(botId, nickname, SpawnPointOnRoomEntry.ToVec(), this);
        await bot.InitToWaiting(); // 초기화 수동 호출
        Players.TryAdd(bot.Id, bot);

        Logger.log($"[Bot][{nickname}] 봇 생성 완료: ID={botId}");

        // 다른 플레이어들에게 봇 참가 알림
        await BroadcastToAll(new JoinBroadcast
        {
            PlayerInfo = bot.PlayerInformation
        });

        return bot;
    }

    public Player? GetPlayerById(int clientId)
    {
        Players.TryGetValue(clientId, out var player);
        return player;
    }

    public Spectator? GetSpectatorById(int clientId)
    {
        Spectators.TryGetValue(clientId, out var spectator);
        return spectator;
    }

    public bool IsSpectator(int clientId)
    {
        return Spectators.ContainsKey(clientId);
    }

    public async Task BroadcastToAll(IPacket packet)
    {
        Logger.log($"[Packet][All] ➡️ {packet.GetType().Name} 브로드캐스트");

        // 실제 클라이언트에게 패킷 전송
        var clientTasks = Clients.Values
            .Select(client => client.SendPacketAsync(packet)).ToList();

        // 봇들에게는 AI 처리 로직 실행
        var botTasks = Players.Values.OfType<Bot>()
            .Select(bot => bot.HandlePacketAsync(packet)).ToList();

        // 모든 작업을 병렬로 실행
        var allTasks = clientTasks.Concat(botTasks);
        if (allTasks.Any())
        {
            await Task.WhenAll(allTasks);
        }
    }

    public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
    {
        var excludedPlayer = GetPlayerById(excludeClientId);
        var excludedName = excludedPlayer?.Nickname ?? $"Client-{excludeClientId}";
        Logger.log($"[Packet][All] ➡️ {packet.GetType().Name} 브로드캐스트 (제외: {excludedName})");
        var tasks = Clients.Values
            .Where(client => client.Id != excludeClientId)
            .Select(client => client.SendPacketAsync(packet));

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 특정 플레이어에게만 패킷을 전송합니다 (봇인 경우 AI 로직 실행)
    /// </summary>
    public async Task SendToPlayer(int playerId, IPacket packet)
    {
        var player = GetPlayerById(playerId);
        var playerName = player?.Nickname ?? $"Client-{playerId}";
        Logger.log($"[Packet][{playerName}] ➡️ {packet.GetType().Name} 전송");

        // 플레이어가 봇인지 확인
        if (Players.TryGetValue(playerId, out var foundPlayer) && foundPlayer is Bot bot)
        {
            // 봇인 경우 AI 처리 로직 실행
            await bot.HandlePacketAsync(packet);
        }
        else if (Clients.TryGetValue(playerId, out var client))
        {
            // 실제 클라이언트인 경우 패킷 전송
            await client.SendPacketAsync(packet);
        }
        else
        {
            Logger.log($"[Packet][Unknown] 플레이어 찾기 실패: ID={playerId}");
        }
    }

    public List<PlayerInformation> PlayersInRoom()
    {
        return Players.Values.Select(player => player.PlayerInformation).ToList();
    }

    /// <summary>
    /// 게임/라운드 종료 조건을 체크하고 적절한 처리를 수행합니다
    /// </summary>
    public async Task CheckAndHandleGameEndAsync()
    {
        // 실제 플레이어 없음
        var realPlayers = Players.Values.Where(p => p is not Bot).ToList();
        if (realPlayers.Count == 0)
        {
            Logger.log("[게임] 실제 플레이어가 모두 나가서 게임을 대기 상태로 초기화합니다");

            // 게임이 진행 중이었다면 게임 결과 전송
            if (CurrentGameState != GameState.Waiting)
            {
                await AnnounceGameWinner();
            }

            await StartWaitingStateAsync();
            return;
        }
        
        // 버그 방지: MAX_ROUNDS 도달 시 바로 초기화 (승자 발표 없이)
        if (Round >= Constants.MAX_ROUNDS)
        {
            Logger.log($"[Game][System] MAX_ROUNDS({Constants.MAX_ROUNDS}) 도달: 게임 강제 초기화");
            await StartWaitingStateAsync();
            return;
        }

        if(CurrentGameState == GameState.Waiting){
            //아무것도 안함
        }else if(CurrentGameState == GameState.Round){
            if (ShouldEndRound())
            {
                await EndRoundAsync();

                if (ShouldEndGame())
                {
                    await AnnounceGameWinner();
                    await StartWaitingStateAsync();
                }
                else
                {
                    await StartShopStateAsync();
                }
            }
        }else{
            if (ShouldEndGame())
            {
                await AnnounceGameWinner();
                await StartWaitingStateAsync();
            }
        }
    }

    /// <summary>
    /// 라운드 종료 조건을 확인합니다
    /// </summary>
    public bool ShouldEndRound()
    {
        return Players.Values.Count(p => p.Alive) <= 1;
    }

    /// <summary>
    /// 게임 종료 조건을 확인합니다
    /// </summary>
    public bool ShouldEndGame()
    {
        // 파산하지 않은 플레이어가 1명 이하일 때 게임 종료
        return Players.Values.Count(p => !p.Bankrupt) <= 1;
    }

    /// <summary>
    /// 대기 상태로 게임 전체를 초기화 (모든 플레이어 + 게임 상태)
    /// </summary>
    public async Task InitToWaiting()
    {
        // 실제 플레이어들만 대기 상태로 초기화 및 배치
        foreach (var p in Players.Values)
        {
            await p.InitToWaiting();
        }

        // 게임 상태 초기화
        Round = 0;
        TotalPrizeMoney = 0;
        BettingTimer = 0f;
        ShopTimer = 0f;
        GameElapsedTime = 0f;

        // 시스템 상태 초기화
        initialRouletteCompletedPlayers.Clear();
        DisconnectedPlayers.Clear(); // 게임 종료 시 나간 플레이어 목록 초기화

        // 게임 상태 변경
        CurrentGameState = GameState.Waiting;
    }

    /// <summary>
    /// 라운드 상태로 초기화 (파산하지 않은 플레이어 + 라운드 상태)
    /// </summary>
    public async Task InitToRound(int newRound)
    {
        Round = newRound;

        // 파산하지 않은 플레이어만 라운드 상태로 초기화 및 배치
        var alivePlayers = Players.Values.Where(p => !p.Bankrupt).ToList();
        foreach (var player in alivePlayers)
        {
            await player.InitToRound();
        }


        // 베팅 시스템 초기화
        BettingTimer = 0f;
        TotalPrizeMoney = 0;

        // 게임 상태 변경
        CurrentGameState = GameState.Round;
    }

    /// <summary>
    /// 모든 봇을 제거합니다
    /// </summary>
    private async Task RemoveAllBots()
    {
        var botsToRemove = Players.Values.OfType<Bot>().ToList();
        if (botsToRemove.Count == 0)
        {
            return;
        }

        Logger.log($"[Bot][System] 봇 제거 시작: {botsToRemove.Count}명");

        foreach (var bot in botsToRemove)
        {
            Players.TryRemove(bot.Id, out _);
            Logger.log($"[Bot][{bot.Nickname}] 봇 제거 완료: ID={bot.Id}");

            // 다른 플레이어들에게 봇 퇴장 알림
            await BroadcastToAll(new LeaveBroadcast
            {
                PlayerId = bot.Id,
            });
        }

        Logger.log($"[Bot][System] 모든 봇 제거 완료: 남은 참가자 {Players.Count}명");
    }

    /// <summary>
    /// 다음 라운드를 시작합니다
    /// </summary>
    public async Task StartRoundStateAsync(int newRound)
    {
        foreach (var pair in SpawnPointOnRoundStart)
        {
            SpawnPointOnRoundStart[pair.Key] = false;
        }
        var oldState = CurrentGameState;
        await InitToRound(newRound);
        DeductionCount = 0; // 라운드 시작 시 징수 횟수 초기화

        Logger.log($"[Round][System] 라운드 {newRound} 시작: 베팅금 {BaseBettingAmount}달러");
        Logger.log($"[Game][System] 게임 상태 변경: {oldState} → {CurrentGameState}");

        // 모든 플레이어의 차감 금액 업데이트
        foreach (var player in Players.Values)
        {
            await player.UpdateNextDeductionAmount();
        }

        foreach (var s in Spectators.Values)
        {
            int multiplier = (int)Math.Pow(2, DeductionCount / Constants.DEDUCTION_MULTIPLY_PERIOD);
            int baseAmount = BaseBettingAmount * multiplier;
            await SendToPlayer(s.Id, new UpdatePlayerNextTaxAmount
            {
                PlayerId = -1, // 관전자용 전원
                TaxAmount = baseAmount
            });
        }

        await BroadcastToAll(new RoundStartFromServer
        {
            PlayersInfo = PlayersInRoom(),
            Round = Round,
        });

        await BroadcastToAll(new StakesUpdateBroadcast
        {
            Delta = 0,
            StakeBefore = 0,
            StakeAfter = 0,
        });

        // 라운드 시작 시스템 메시지 브로드캐스트
        await BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1, // 시스템 메시지
            Message = $"라운드 {Round} 시작! 베팅금: {BaseBettingAmount}달러"
        });
    }

    /// <summary>
    /// 게임을 완전히 초기화하고 대기 상태로 돌아갑니다
    /// </summary>
    public async Task StartWaitingStateAsync()
    {
        var oldState = CurrentGameState;

        await InitToWaiting();

        // 서버 종료 타이머 리셋
        emptyServerTimer = 0f;

        Logger.log($"[Game][System] 게임 상태 변경: {oldState} → {CurrentGameState}");

        await BroadcastToAll(new WaitingStartFromServer { PlayersInfo = PlayersInRoom() });

        // 대기 상태 시스템 메시지 브로드캐스트
        await BroadcastToAll(new ChatBroadcast
        {
            PlayerId = -1, // 시스템 메시지
            Message = "게임이 종료되었습니다. 대기 상태로 전환됩니다."
        });

        await RemoveAllBots();
        LobbyReporter.ReportState(0);
    }



    /// <summary>
    /// 라운드를 종료하고 정리 작업을 수행합니다
    /// </summary>
    private async Task EndRoundAsync()
    {
        // ============================================================
        // 1. 생존자 확인 및 상금 계산
        // ============================================================
        var survivors = Players.Values.Where(p => p.Alive).ToList();
        int prizePerWinner = survivors.Count == 0 ? 0 : TotalPrizeMoney / survivors.Count;
        var winnerIds = new List<int>();

        // ============================================================
        // 2. 생존자에게 상금 지급
        // ============================================================
        int roundWinnerBalanceBefore = 0, roundWinnerBalanceAfter = 0;
        if (survivors.Count == 1)
            roundWinnerBalanceBefore = survivors[0].Balance;
        foreach (var winner in survivors)
        {
            await winner.Deposit(prizePerWinner);
            winnerIds.Add(winner.Id);
        }
        if (survivors.Count == 1)
            roundWinnerBalanceAfter = survivors[0].Balance;

        // ============================================================
        // 3. 은행가의 부적 아이템 효과 처리
        // ============================================================
        foreach (var player in Players.Values.Where(p => !p.Bankrupt))
        {
            if (player.GetItemCount(ItemId.CreditCard) > 0)
            {
                int interest = (int)Math.Round(player.Balance * ItemConstants.InterestFarm.INTEREST_RATE);
                if (interest > 0)
                {
                    await player.Deposit(interest);
                    Logger.log($"[Item][{player.Nickname}] 은행가의 부적 이자 획득: +{interest}달러");

                    await BroadcastToAll(new ChatBroadcast
                    {
                        PlayerId = -1,
                        Message = $"{player.Nickname}님이 은행가의 부적에서 {interest}달러를 받았습니다"
                    });
                }
            }
        }

        // ============================================================
        // 4. 승리자 로그 출력
        // ============================================================
        if (survivors.Count == 1)
        {
            Logger.log($"[Round][{survivors[0].Nickname}] 라운드 {Round} 승리: 판돈 {TotalPrizeMoney}달러 획득");
        }
        else
        {
            Logger.log($"[Round][System] 라운드 {Round} 종료: 생존자 {survivors.Count}명, 각자 {prizePerWinner}달러 획득");
        }

        // ============================================================
        // 5. 라운드 승리자 브로드캐스트
        // ============================================================
        await BroadcastToAll(new RoundWinnerBroadcast
        {
            PlayerIds = winnerIds,
            Round = Round,
            PrizeMoney = TotalPrizeMoney,
            WinnerBalanceBefore = roundWinnerBalanceBefore,
            WinnerBalanceAfter = roundWinnerBalanceAfter
        });

        // ============================================================
        // 6. 승리자 시스템 메시지 브로드캐스트
        // ============================================================
        if (survivors.Count == 1)
        {
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1,
                Message = $"라운드 {Round} 승리: {survivors[0].Nickname}님! (상금: {TotalPrizeMoney}달러)"
            });
        }
        else if (survivors.Count > 1)
        {
            var winnerNames = string.Join(", ", survivors.Select(s => s.Nickname));
            await BroadcastToAll(new ChatBroadcast
            {
                PlayerId = -1,
                Message = $"라운드 {Round} 승리: {winnerNames}! (각자 {prizePerWinner}달러 획득)"
            });
        }

        // ============================================================
        // 7. RoundTemporary 태그 아이템 제거
        // ============================================================
        foreach (var player in Players.Values)
        {
            var temporaryItems = player.Items
                .Where(itemId => ItemConstants.Items.ContainsKey(itemId) &&
                                ItemConstants.Items[itemId].Tags.HasFlag(ItemTags.RoundTemporary))
                .ToList();

            foreach (var itemId in temporaryItems)
            {
                await player.RemoveItem(itemId, false);
                Logger.log($"[Item][{player.Nickname}] 라운드 종료로 임시 아이템 제거: {itemId}");
            }
        }

        Logger.log($"[Round][System] 라운드 {Round} 종료 처리 완료");


        // 잠시 대기 (라운드 종료 연출 시간)
        await Task.Delay(2500);
    }

    public async Task AnnounceGameWinner()
    {
        // 1. 모든 플레이어(현재 + 나간) 수집
        var allPlayers = Players.Values.Concat(DisconnectedPlayers).ToList();
        Logger.log($"[Game][System] 최종 결과 계산 시작: 현재 플레이어 {Players.Count}명 + 나간 플레이어 {DisconnectedPlayers.Count}명 = 총 {allPlayers.Count}명");

        // 2. 파산 시간 기준으로 정렬 (생존자가 1등, 늦게 파산할수록 높은 순위)
        //    커스텀 정렬: -1(생존자)을 가장 높은 순위로, 그 외는 내림차순
        var sortedPlayers = allPlayers
            .OrderBy(p => p.BankruptTime == -1f ? float.MinValue : -p.BankruptTime) // -1은 가장 작은 값으로, 나머지는 음수 변환하여 큰 값이 앞으로
            .ThenBy(p => p.Id) // 동시 파산 시 ID로 정렬 (일관성)
            .ToList();

        // 3. 순위 계산 (비표준 방식: 동점자들을 뒤 등수로 배치)
        //    예: 상위 2명 동점 → 2, 2, 3 (1등 없음)
        var rankings = new List<PlayerRanking>();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var player = sortedPlayers[i];
            int rank;

            if (i == 0)
            {
                // 첫 번째 플레이어: 동점자 수를 미리 계산
                int tieCount = sortedPlayers.Count(p => p.BankruptTime == player.BankruptTime);
                rank = tieCount; // 동점자 수만큼의 등수
            }
            else if (player.BankruptTime == sortedPlayers[i - 1].BankruptTime)
            {
                // 이전 플레이어와 동점
                rank = rankings[i - 1].Rank;
            }
            else
            {
                // 새로운 등수 그룹 시작: 현재 인덱스 + 동점자 수
                int tieCount = sortedPlayers.Skip(i).Count(p => p.BankruptTime == player.BankruptTime);
                rank = i + tieCount;
            }

            // 상금 계산: Balance + 순위별 보상 (파산자는 Balance가 0이므로 보상만 받음)
            int rankBonus = rank >= 1 && rank <= 8 ? Constants.RANK_REWARDS[rank] : 0;
            int reward = Math.Min(
                player.Balance + rankBonus,
                Constants.MAX_REWARD_TO_LOBBY
            );

            rankings.Add(new PlayerRanking
            {
                Rank = rank,
                PlayerId = player.Id,
                RewardMoney = reward,
                Nickname = player.Nickname
            });
        }

        // 4. 최종 승자 결정 (1등이 단 한 명인 경우만)
        var winners = rankings.Where(r => r.Rank == 1).ToList();
        int winnerId = -1; // 기본값: 승자 없음
        int winnerReward = 0; // 기본값: 보상 없음

        if (winners.Count == 1)
        {
            winnerId = winners[0].PlayerId;
            winnerReward = winners[0].RewardMoney;
            var winnerPlayer = allPlayers.First(p => p.Id == winnerId);
            Logger.log($"[Game][System] 최종 승리자: {winnerPlayer.Nickname} (상금: {winnerReward})");
        }
        else if (winners.Count > 1)
        {
            var winnerNames = string.Join(", ", winners.Select(w =>
                allPlayers.First(p => p.Id == w.PlayerId).Nickname));
            Logger.log($"[Game][System] 공동 1등: {winnerNames} ({winners.Count}명)");
        }
        else
        {
            Logger.log($"[Game][System] 최종 생존자 없음 (전원 동시 파산)");
        }

        // 5. 승자 발표 브로드캐스트
        await BroadcastToAll(new GameWinnerBroadcast
        {
            WinnerId = winnerId,
            RewardMoney = winnerReward
        });

        // 잠시 대기 (승자 발표 연출 시간)
        await Task.Delay(5000);

        // 6. 종합 결과 브로드캐스트
        await BroadcastToAll(new GameResultsBroadcast { Rankings = rankings });

        // 잠시 대기 (종합 결과 연출 시간)
        // 임시 비활
        await Task.Delay(5000);

        // 7. 로비 서버에 결과 전송
        if (!Program.DEV_MODE)
        {
            var allResults = allPlayers
                .Where(p => p is not Bot)
                .Select(p =>
                {
                    var ranking = rankings.First(r => r.PlayerId == p.Id);
                    return new PlayerGameResult
                    {
                        ProviderId = p.ProviderId,
                        Rank = ranking.Rank,
                        RewardMoney = ranking.RewardMoney // 이미 MAX_REWARD_TO_LOBBY로 제한됨
                    };
                })
                .ToList();

            if (allResults.Count > 0)
            {
                try
                {
                    Logger.log($"[Lobby][System] 게임 결과 전송 시작: {allResults.Count}명");
                    foreach (var result in allResults)
                    {
                        var ranking = rankings.First(r => r.PlayerId == allPlayers.First(p => p.ProviderId == result.ProviderId).Id);
                        Logger.log($"[Lobby][System] - {ranking.Nickname}: {result.Rank}등, 보상 {result.RewardMoney}달러");
                    }
                    LobbyReporter.ReportBulkGameResults(allResults);
                    Logger.log($"[Lobby][System] 게임 결과 전송 완료: {allResults.Count}명");
                }
                catch (Exception ex)
                {
                    Logger.log($"[Lobby][System] 게임 결과 전송 실패: {ex.Message}");
                }
            }
        }
    }
    
    /// <summary>
    /// 이니셜 룰렛을 시작합니다 (룰렛 풀 생성 후 플레이어 대기)
    /// </summary>
    public async Task StartInitialRouletteStateAsync()
    {
        var oldState = CurrentGameState;

        // Initial Roulette 상태로 변경
        CurrentGameState = GameState.InitialRoulette;
        initialRouletteCompletedPlayers.Clear();
        InitialRouletteTimer = 0f; // 타이머 초기화
        GameElapsedTime = 0f; // 게임 시작 시간 초기화

        Logger.log($"[Game][System] 게임 상태 변경: {oldState} → {CurrentGameState}");
        Logger.log($"[Game][System] 모든 플레이어 확인 대기 중 ({Constants.INITIAL_ROULETTE_DURATION}초 제한)");

        // 각 플레이어에게 개별 룰렛 풀 생성 및 정확도 할당
        foreach (var player in Players.Values.Where(p => !p.Bankrupt))
        {
            // 각 플레이어에게 개별 룰렛 풀 생성 (게임 끝까지 유지됨)
            player.RoulettePool.Clear();
            for (var i = 0; i < 8; i++)
            {
                player.RoulettePool.Add(Player.PickAccuracy(Constants.INITIAL_ROULETTE_MIN_ACCURACY, Constants.INITIAL_ROULETTE_MAX_ACCURACY + 1, true));
            }

            // 풀에서 랜덤하게 하나를 선택하여 플레이어 정확도로 설정
            var selectedAccuracy = player.RoulettePool[Random.Shared.Next(player.RoulettePool.Count)];
            await player.SetAccuracy(selectedAccuracy);

            Logger.log($"[Shop][{player.Nickname}] 이니셜 룰렛 할당: 정확도 {selectedAccuracy}%, 풀 [{string.Join(", ", player.RoulettePool)}]");

            await SendToPlayer(player.Id, new InitialRouletteStartFromServer
            {
                YourAccuracy = selectedAccuracy,
                AccuracyPool = player.RoulettePool // 각 플레이어마다 다른 룰렛 풀
            });
        }

        foreach (var s in Spectators.Values)
        {
            await SendToPlayer(s.Id, new InitialRouletteStartFromServer());
        }

        Logger.log($"[Shop][System] 이니셜 룰렛 시작: 모든 플레이어에게 전송 완료");
    }
    
    /// <summary>
    /// 상점을 시작합니다 (각 플레이어에게 개별 상점 제공)
    /// </summary>
    public async Task StartShopStateAsync()
    {
        var oldState = CurrentGameState;

        // Shop 상태로 변경
        CurrentGameState = GameState.Shop;
        ShopTimer = 0f;

        // 라운드에 비례해서 상점 시간 감소
        float shopDuration = Math.Max(6f, Constants.SHOP_DURATION - (Round - 1) * 2 );

        Logger.log($"[Game][System] 게임 상태 변경: {oldState} → {CurrentGameState}");
        Logger.log($"[Shop][System] 상점 시작: {shopDuration}초 후 종료");
        await Task.WhenAll(Players.Values.Select(p =>
        {
            var shopData = GeneratePlayerShopData(p);
            p.ShopData = shopData;
            Logger.log($"[Shop][{p.Nickname}] 상점 데이터 생성: 룰렛 풀 [{string.Join(", ", shopData.ShopRoulettePool)}]");
            return SendToPlayer(p.Id, new ShopStartFromServer
            {
                ShopData = shopData,
                Duration = (int)shopDuration,
                YouAreBankrupt = p.Bankrupt
            });
        }));
        await Task.WhenAll(Spectators.Values.Select(s => SendToPlayer(s.Id, new ShopStartFromServer { Duration = (int)shopDuration, YouAreBankrupt = true })));

        Logger.log($"[Shop][System] 상점 데이터 전송 완료: 모든 플레이어");
    }
    
    /// <summary>
    /// 플레이어에게 개별 상점 데이터를 생성합니다
    /// </summary>
    private ShopData GeneratePlayerShopData(Player player)
    {
        // 3개의 램덤 아이템 생성
        var shopItems = new List<ShopItem>();
        
        // ShopItem 태그를 가진 아이템들만 필터링
        var shopItemsOnly = ItemConstants.Items.Values
            .Where(item => item.Tags.HasFlag(ItemTags.ShopItem))
            .ToList();
        
        // 플레이어가 이미 소유한 중첩 불가능한 ShopItem들 제외
        var playerOwnedNonStackableItems = player.Items
            .Where(itemId => ItemConstants.Items.ContainsKey(itemId) && 
                           ItemConstants.Items[itemId].Tags.HasFlag(ItemTags.ShopItem) && 
                           !ItemConstants.Items[itemId].Tags.HasFlag(ItemTags.Stackable))
            .ToHashSet();
        
        // 사용 가능한 아이템 = Stackable이거나, 플레이어가 소유하지 않은 NonStackable 아이템
        var availableItems = shopItemsOnly
            .Where(item => item.Tags.HasFlag(ItemTags.Stackable) || 
                          !playerOwnedNonStackableItems.Contains(item.Id))
            .ToList();
        
        for (int i = 0; i < Constants.SHOP_ITEM_COUNT; i++)
        {
            if (availableItems.Count == 0)
            {
                // 사용 가능한 아이템이 없으면 None 아이템으로 채움
                var noneItem = ItemConstants.Items[ItemId.None];
                shopItems.Add(new ShopItem
                {
                    ItemId = noneItem.Id,
                    Price = noneItem.BasePrice,
                    Name = noneItem.Name,
                    Description = noneItem.Description
                });
            }
            else
            {
                var randomItem = SelectRandomItemByWeight(availableItems);

                shopItems.Add(new ShopItem
                {
                    ItemId = randomItem.Id,
                    Price = randomItem.BasePrice,
                    Name = randomItem.Name,
                    Description = randomItem.Description
                });

                // 중복 방지: NonStackable 아이템은 선택 후 제거
                if (!randomItem.Tags.HasFlag(ItemTags.Stackable))
                {
                    availableItems.Remove(randomItem);
                }
            }
        }
        
        // 플레이어의 기존 룰렛 풀 사용 (InitialRoulette에서 생성된 풀)
        return new ShopData
        {
            YourItems = shopItems,
            ShopRoulettePrice = Constants.SHOP_ROULETTE_PRICE,
            ShopRoulettePool = player.RoulettePool // 플레이어의 기존 룰렛 풀 재사용
        };
    }
    
    /// <summary>
    /// 가중치 기반으로 램덤 아이템을 선택합니다
    /// </summary>
    private ItemData SelectRandomItemByWeight(List<ItemData> items)
    {
        var totalWeight = items.Sum(item => item.Weight);
        var randomValue = Random.Shared.Next(totalWeight);
        
        var currentWeight = 0;
        foreach (var item in items)
        {
            currentWeight += item.Weight;
            if (randomValue < currentWeight)
            {
                return item;
            }
        }
        
        return items[0]; // 기본값
    }
}