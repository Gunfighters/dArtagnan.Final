using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using dArtagnan.Shared;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dArtagnan.ClientTest;

internal class Program
{
    private static ClientWebSocket? lobbyWs;
    private static string? lobbyUrl;
    private static string? sessionId;
    private static TcpClient? client;
    private static NetworkStream? stream;
    private static bool isConnected = false;
    private static bool isRunning = true; // 프로그램 실행 상태
    private static Vector2 position;
    private static float speed = 40f; // 일정한 속도
    private static int direction;
    private static Stopwatch stopwatch = new();

    static void CalculatePositionSoFar()
    {
        position += speed * stopwatch.ElapsedMilliseconds / 1000f * direction.IntToDirection();
        stopwatch.Restart();
    }

    static async Task SendMovementData()
    {
        try
        {
            var playerDirection = new MovementDataFromClient
                { MovementData = { Direction = direction, Position = position, Speed = speed } };
            await SendPacketAsync(playerDirection);
        }
        catch (Exception ex)
        {
            Logger.log($"이동 패킷 전송 실패: {ex.Message}");
        }
    }

    static void ShowHelp()
    {
        Logger.log($"=== D'Artagnan 테스트 클라이언트 ===");
        Logger.log($"명령어:");
        Logger.log($"  [일반]");
        Logger.log($"  h/help - 명령어 목록 보기");
        Logger.log($"  [로비]");
        Logger.log($"  lg/login [nickname?] [lobbyUrl?] - 로비 로그인 (기본: test http://localhost:3002)");
        Logger.log($"  cr/create-room - 방 생성(로비 WebSocket)");
        Logger.log($"  jr/join-room [roomId?] - 방 참가(로비 WebSocket, roomId 없으면 랜덤 배정/생성)");
        Logger.log($"  sn/set-nickname [nickname] - 닉네임 설정");
        Logger.log($"  cc/change-costume [partType] [costumeId] - 코스튬 변경 (예: cc hair hair_001)");
        Logger.log($"  sbpr/shop-buy-part-roulette [partType] - 파츠 룰렛 구매 (예: sbpr hair)");
        Logger.log($"  sbpd/shop-buy-part-direct [partType] [costumeId] - 파츠 직접 구매 (예: sbpd hair hair_001)");
        Logger.log($"  gpr/get-part-rates [partType] - 파츠 확률 조회 (예: gpr hair)");
        Logger.log($"  [게임]");
        Logger.log($"  c/connect [host?] [port?] - 게임 서버 직접 TCP 연결 (기본: localhost 3002)");
        Logger.log($"  s/start - 게임 시작");
        Logger.log($"  ur/update-room [roomName] - 방 제목 변경 (방장만 가능)");
        Logger.log($"  d/dir [direction] - 플레이어 이동 방향 변경");
        Logger.log($"  sh/shoot [targetId] - 플레이어 공격");
        Logger.log($"  a/accuracy [state] - 정확도 상태 변경 (-1: 감소, 0: 유지, 1: 증가)");
        Logger.log($"  ir/initial-roulette - 이니셜 룰렛 완료 신호");
        Logger.log($"  pi/purchase-item [itemId] - 상점에서 아이템 구매");
        Logger.log($"  sr/shop-roulette - 상점 룰렛 시도");
        Logger.log($"  mi/mining [true/false?] - 채굴 시작/중단 (기본: true)");
        Logger.log($"  m/msg/chat [message] - 채팅 메시지 전송");
        Logger.log($"  l/leave - 게임 나가기");
        Logger.log($"  q/quit - 종료");
        Logger.log($"=====================================");
    }

    static async Task Main(string[] args)
    {
        ShowHelp();

        var receiveTask = Task.Run(ReceiveLoop);
        var lobbyReceiveTask = Task.Run(LobbyReceiveLoop);
        var pingTask = Task.Run(PingLoop);

        while (isRunning) // isConnected 대신 isRunning을 사용하도록 변경
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            await ProcessCommand(input);
        }

        await receiveTask;
        await lobbyReceiveTask;
        await pingTask;
    }

    private static async Task PingLoop()
    {
        while (isRunning)
        {
            if (stream != null && isConnected)
            {
                await SendPacketAsync(new PingPacket());
                await Task.Delay(Constants.CONNECTION_TIMEOUT_SECONDS * 1000 / 2);
            }
        }
    }

    static async Task ProcessCommand(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();

        try
        {
            switch (command)
            {
                case "h":
                case "help":
                    ShowHelp();
                    break;

                case "lg":
                case "login":
                    string nick = "test";
                    string url = "http://localhost:3002";

                    if (parts.Length >= 2)
                        nick = parts[1];
                    if (parts.Length >= 3)
                        url = parts[2];

                    lobbyUrl = url;
                    await LobbyLogin(nick, lobbyUrl);
                    break;

                case "cr":
                case "create-room":
                    await LobbyEnsureConnected();
                    await LobbyCreateRoom();
                    break;

                case "jr":
                case "join-room":
                    await LobbyEnsureConnected();
                    var rid = parts.Length >= 2 ? parts[1] : string.Empty;
                    await LobbyJoinRoom(rid);
                    break;

                case "sn":
                case "set-nickname":
                    await LobbyEnsureConnected();
                    if (parts.Length >= 2)
                    {
                        await LobbySetNickname(parts[1]);
                    }
                    else
                    {
                        Logger.log($"사용법: sn/set-nickname [nickname]");
                    }
                    break;

                case "cc":
                case "change-costume":
                    await LobbyEnsureConnected();
                    if (parts.Length >= 3)
                    {
                        await LobbyChangeCostume(parts[1], parts[2]);
                    }
                    else
                    {
                        Logger.log($"사용법: cc/change-costume [partType] [costumeId]");
                    }
                    break;

                case "sbpr":
                case "shop-buy-part-roulette":
                    await LobbyEnsureConnected();
                    if (parts.Length >= 2)
                    {
                        await LobbyShopBuyPartRoulette(parts[1]);
                    }
                    else
                    {
                        Logger.log($"사용법: sbpr/shop-buy-part-roulette [partType]");
                    }
                    break;

                case "sbpd":
                case "shop-buy-part-direct":
                    await LobbyEnsureConnected();
                    if (parts.Length >= 3)
                    {
                        await LobbyShopBuyPartDirect(parts[1], parts[2]);
                    }
                    else
                    {
                        Logger.log($"사용법: sbpd/shop-buy-part-direct [partType] [costumeId]");
                    }
                    break;

                case "gpr":
                case "get-part-rates":
                    await LobbyEnsureConnected();
                    if (parts.Length >= 2)
                    {
                        await LobbyGetPartRates(parts[1]);
                    }
                    else
                    {
                        Logger.log($"사용법: gpr/get-part-rates [partType]");
                    }
                    break;

                case "c":
                case "connect":
                    string host = "localhost";
                    int port = 7777;

                    if (parts.Length >= 2)
                        host = parts[1];
                    if (parts.Length >= 3)
                        port = int.Parse(parts[2]);

                    await ConnectToServer(host, port);
                    break;

                case "s":
                case "start":
                    await StartGame();
                    break;

                case "ur":
                case "update-room":
                    if (parts.Length >= 2)
                    {
                        var roomName = string.Join(" ", parts.Skip(1)); // 첫 번째 단어(명령어) 제외하고 나머지를 방 제목으로 합치기
                        await SendUpdateRoomName(roomName);
                    }
                    else
                    {
                        Logger.log($"사용법: ur/update-room [roomName]");
                    }
                    break;

                case "d":
                case "dir":
                    if (parts.Length >= 2)
                    {
                        var i = int.Parse(parts[1]);
                        await SendDirection(i);
                    }
                    else
                    {
                        Logger.log($"사용법: d/dir [direction]");
                    }

                    break;

                case "sh":
                case "shoot":
                    if (parts.Length >= 2)
                    {
                        var targetId = int.Parse(parts[1]);
                        await SendShoot(targetId);
                    }
                    else
                    {
                        Logger.log($"사용법: sh/shoot [targetId]");
                    }

                    break;

                case "a":
                case "accuracy":
                    if (parts.Length >= 2)
                    {
                        var state = int.Parse(parts[1]);
                        await SendAccuracyState(state);
                    }
                    else
                    {
                        Logger.log($"사용법: a/accuracy [state] (-1: 감소, 0: 유지, 1: 증가)");
                    }

                    break;

                case "ir":
                case "initial-roulette":
                    await SendInitialRouletteDone();
                    break;

                case "pi":
                case "purchase-item":
                    if (parts.Length >= 2)
                    {
                        var itemId = int.Parse(parts[1]);
                        await SendPurchaseItem(itemId);
                    }
                    else
                    {
                        Logger.log($"사용법: pi/purchase-item [itemId]");
                    }

                    break;

                case "sr":
                case "shop-roulette":
                    await SendShopRoulette();
                    break;

                case "mi":
                case "mining":
                    if (parts.Length >= 2)
                    {
                        var isMining = bool.Parse(parts[1]);
                        await SendMining(isMining);
                    }
                    else
                    {
                        await SendMining(true); // 기본값: 채굴 시작
                    }

                    break;

                case "m":
                case "msg":
                case "chat":
                    if (parts.Length >= 2)
                    {
                        var message = string.Join(" ", parts.Skip(1)); // 첫 번째 단어(명령어) 제외하고 나머지를 메시지로 합치기
                        await SendChat(message);
                    }
                    else
                    {
                        Logger.log($"사용법: m/msg/chat [message]");
                    }

                    break;

                case "l":
                case "leave":
                    await SendLeave();
                    break;

                case "q":
                case "quit":
                    await Disconnect();
                    if (lobbyWs != null && lobbyWs.State == WebSocketState.Open)
                    {
                        await lobbyWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }

                    isRunning = false; // isConnected 대신 isRunning을 사용하도록 변경
                    break;

                default:
                    Logger.log($"알 수 없는 명령어입니다.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.log($"명령어 처리 오류: {ex.Message}");
        }
    }

    static async Task LobbyLogin(string nickname, string lobbyEndpoint)
    {
        // HTTP 로그인
        try
        {
            using var http = new HttpClient();
            var payload = new { providerId = nickname, clientVersion = "1.0.0" };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await http.PostAsync(new Uri(new Uri(lobbyEndpoint), "/login"), content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                Logger.log($"❌ 로그인 실패: {resp.StatusCode} {body}");
                return;
            }

            var doc = JsonDocument.Parse(body);
            sessionId = doc.RootElement.GetProperty("sessionId").GetString();
            var returnedNickname = doc.RootElement.TryGetProperty("nickname", out var nickEl) ? nickEl.GetString() : "";
            Logger.log($"✅ 로그인 성공");
            Logger.log($"  sessionId: {sessionId}");
            Logger.log($"  nickname: {returnedNickname}");
        }
        catch (Exception ex)
        {
            Logger.log($"❌ 로그인 오류: {ex.Message}");
            return;
        }

        await LobbyConnect();
    }

    static async Task LobbyConnect()
    {
        if (lobbyWs != null && lobbyWs.State == WebSocketState.Open) return;
        if (string.IsNullOrWhiteSpace(lobbyUrl) || string.IsNullOrWhiteSpace(sessionId))
        {
            Logger.log($"Url 또는 sessionId가 없습니다. 먼저 login 명령 사용");
            return;
        }

        try
        {
            lobbyWs = new ClientWebSocket();
            var wsUrl = lobbyUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            await lobbyWs.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

            // 인증 메시지 전송
            var authMsg = JsonSerializer.Serialize(new { type = "auth", sessionId });
            await SendWebSocketMessage(lobbyWs, authMsg);

            Logger.log($"WebSocket 연결됨");
        }
        catch (Exception ex)
        {
            Logger.log($" WebSocket 연결 실패: {ex.Message}");
        }
    }

    static async Task LobbyEnsureConnected()
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            await LobbyConnect();
        }
    }

    static async Task SendWebSocketMessage(ClientWebSocket ws, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    static async Task LobbyReceiveLoop()
    {
        while (isRunning)
        {
            if (lobbyWs != null && lobbyWs.State == WebSocketState.Open)
            {
                try
                {
                    var messageBuilder = new List<byte>();
                    WebSocketReceiveResult result;

                    do
                    {
                        var buffer = new ArraySegment<byte>(new byte[4096]);
                        result = await lobbyWs.ReceiveAsync(buffer, CancellationToken.None);

                        for (int i = 0; i < result.Count; i++)
                        {
                            messageBuilder.Add(buffer.Array![i]);
                        }
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(messageBuilder.ToArray());
                        await HandleLobbyMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.log($" WebSocket 수신 오류: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
            else
            {
                await Task.Delay(100);
            }
        }
    }

    static async Task HandleLobbyMessage(string message)
    {
        try
        {
            var doc = JsonDocument.Parse(message);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "auth_success":
                {
                    var nickname = doc.RootElement.TryGetProperty("nickname", out var nickEl) ? nickEl.GetString() : "";
                    var gold = doc.RootElement.TryGetProperty("gold", out var goldEl) ? goldEl.GetInt32() : 0;
                    var silver = doc.RootElement.TryGetProperty("silver", out var silverEl) ? silverEl.GetInt32() : 0;
                    var level = doc.RootElement.TryGetProperty("level", out var levelEl) ? levelEl.GetInt32() : 1;
                    var currentExp = doc.RootElement.TryGetProperty("currentExp", out var expEl) ? expEl.GetInt32() : 0;
                    var expToNextLevel = doc.RootElement.TryGetProperty("expToNextLevel", out var expNextEl) ? expNextEl.GetInt32() : 0;
                    var isNewUser = doc.RootElement.TryGetProperty("isNewUser", out var newUserEl) && newUserEl.GetBoolean();
                    var gameSessionToken = doc.RootElement.TryGetProperty("gameSessionToken", out var gstEl) ? gstEl.GetString() : "";

                    // 게임 세션 토큰을 sessionId에 저장 (게임 서버 연결 시 사용)
                    sessionId = gameSessionToken;

                    Logger.log($"✅ 인증 성공 - {nickname} | Lv.{level} ({currentExp}/{expToNextLevel}) | 💰{gold} 💎{silver}{(isNewUser ? " [신규]" : "")}");
                    break;
                }

                case "error":
                {
                    var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl)
                        ? etEl.GetString()
                        : "unknown";
                    var errorMsg = doc.RootElement.TryGetProperty("message", out var msgEl)
                        ? msgEl.GetString()
                        : "알 수 없는 오류";
                    Logger.log($"❌ 시스템 오류: {errorMsg} (타입: {errorType})");
                    break;
                }

                case "create_room_response":
                {
                    var ok = doc.RootElement.GetProperty("ok").GetBoolean();
                    if (ok)
                    {
                        var roomId = doc.RootElement.GetProperty("roomId").GetString();
                        var roomName = doc.RootElement.TryGetProperty("roomName", out var rnEl) ? rnEl.GetString() : "";
                        var ip = doc.RootElement.GetProperty("ip").GetString();
                        var port = doc.RootElement.GetProperty("port").GetInt32();
                        Logger.log($"🚪 방 생성 성공!");
                        Logger.log($"  방 ID: {roomId}");
                        Logger.log($"  방 이름: {roomName}");
                        Logger.log($"  서버: {ip}:{port}");
                        await ConnectToServer(ip!, port);
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 방 생성 실패: {msg} (타입: {errorType})");
                    }
                    break;
                }

                case "join_room_response":
                {
                    var ok = doc.RootElement.GetProperty("ok").GetBoolean();
                    if (ok)
                    {
                        var roomId = doc.RootElement.TryGetProperty("roomId", out var ridEl) ? ridEl.GetString() : "";
                        var roomName = doc.RootElement.TryGetProperty("roomName", out var rnEl) ? rnEl.GetString() : "";
                        var ip = doc.RootElement.GetProperty("ip").GetString();
                        var port = doc.RootElement.GetProperty("port").GetInt32();
                        Logger.log($"🚪 방 참가 성공!");
                        Logger.log($"  방 ID: {roomId}");
                        Logger.log($"  방 이름: {roomName}");
                        Logger.log($"  서버: {ip}:{port}");
                        await ConnectToServer(ip!, port);
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 방 참가 실패: {msg} (타입: {errorType})");
                    }
                    break;
                }

                case "auth_error":
                {
                    // 하위 호환성을 위해 auth_error도 지원
                    var error = doc.RootElement.TryGetProperty("error", out var errorEl)
                        ? errorEl.GetString()
                        : "인증 실패";
                    Logger.log($" 인증 실패: {error}");
                    break;
                }

                case "nickname_set":
                {
                    var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
                    if (ok)
                    {
                        var nickname = doc.RootElement.TryGetProperty("nickname", out var nickNameEl) ? nickNameEl.GetString() : "";
                        Logger.log($"✅ 닉네임 설정 성공: {nickname}");
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 닉네임 설정 실패: {msg} (타입: {errorType})");
                    }
                    break;
                }

                case "rooms_update":
                {
                    if (doc.RootElement.TryGetProperty("rooms", out var roomsEl))
                    {
                        Logger.log($"=== 방 목록 업데이트 ===");
                        var roomsArray = roomsEl.EnumerateArray();
                        foreach (var room in roomsArray)
                        {
                            var rId = room.TryGetProperty("roomId", out var ridEl) ? ridEl.GetString() : "";
                            var rName = room.TryGetProperty("roomName", out var rnEl) ? rnEl.GetString() : "";
                            var pCount = room.TryGetProperty("playerCount", out var pcEl) ? pcEl.GetInt32() : 0;
                            var maxP = room.TryGetProperty("maxPlayers", out var mpEl) ? mpEl.GetInt32() : 0;
                            var joinable = room.TryGetProperty("joinable", out var jEl) && jEl.GetBoolean();
                            Logger.log($"  [{rId}] {rName} ({pCount}/{maxP}) - {(joinable ? "참가가능" : "게임중")}");
                        }
                    }
                    break;
                }

                case "update_nickname":
                {
                    var nickname = doc.RootElement.TryGetProperty("nickname", out var nickEl) ? nickEl.GetString() : "";
                    Logger.log($"닉네임 업데이트: {nickname}");
                    break;
                }

                case "update_gold":
                {
                    var gold = doc.RootElement.TryGetProperty("gold", out var goldEl) ? goldEl.GetInt32() : 0;
                    Logger.log($"💰 금화 업데이트: {gold}");
                    break;
                }

                case "update_silver":
                {
                    var silver = doc.RootElement.TryGetProperty("silver", out var silverEl) ? silverEl.GetInt32() : 0;
                    Logger.log($"💎 실버 업데이트: {silver}");
                    break;
                }

                case "update_equipped_costumes":
                {
                    if (doc.RootElement.TryGetProperty("equippedCostumes", out var equippedEl))
                    {
                        Logger.log($"👔 장착 코스튬 업데이트:");
                        foreach (var prop in equippedEl.EnumerateObject())
                        {
                            Logger.log($"  {prop.Name}: {prop.Value.GetString()}");
                        }
                    }
                    break;
                }

                case "update_inventory":
                {
                    if (doc.RootElement.TryGetProperty("ownedCostumes", out var ownedEl))
                    {
                        Logger.log($"🎒 인벤토리 업데이트:");
                        foreach (var prop in ownedEl.EnumerateObject())
                        {
                            var costumes = new List<string>();
                            foreach (var c in prop.Value.EnumerateArray())
                                costumes.Add(c.GetString() ?? "");
                            Logger.log($"  {prop.Name}: [{string.Join(", ", costumes)}]");
                        }
                    }
                    break;
                }

                case "update_experience":
                {
                    var level = doc.RootElement.TryGetProperty("level", out var lvlEl) ? lvlEl.GetInt32() : 1;
                    var currentExp = doc.RootElement.TryGetProperty("currentExp", out var currExpEl) ? currExpEl.GetInt32() : 0;
                    var expToNextLevel = doc.RootElement.TryGetProperty("expToNextLevel", out var nextExpEl) ? nextExpEl.GetInt32() : 0;
                    Logger.log($"⭐ 경험치 업데이트: 레벨 {level} ({currentExp}/{expToNextLevel})");
                    break;
                }

                case "shop_buy_part_roulette_response":
                {
                    var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
                    if (ok)
                    {
                        var partType = doc.RootElement.TryGetProperty("partType", out var ptEl) ? ptEl.GetString() : "";
                        var wonCostume = doc.RootElement.TryGetProperty("wonCostume", out var wcEl) ? wcEl.GetString() : "";
                        var isDuplicate = doc.RootElement.TryGetProperty("isDuplicate", out var dupEl) && dupEl.GetBoolean();
                        var silverGained = doc.RootElement.TryGetProperty("silverGained", out var sgEl) ? sgEl.GetInt32() : 0;

                        Logger.log($"🎰 [{partType}] 룰렛 구매 성공!");
                        Logger.log($"  획득: {wonCostume}");
                        if (isDuplicate)
                        {
                            Logger.log($"  중복! 실버 획득: {silverGained}");
                        }

                        if (doc.RootElement.TryGetProperty("roulettePool", out var rpEl))
                        {
                            var pool = new List<string>();
                            foreach (var p in rpEl.EnumerateArray())
                                pool.Add(p.GetString() ?? "");
                            Logger.log($"  룰렛 풀: [{string.Join(", ", pool)}]");
                        }
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 룰렛 구매 실패: {msg} (코드: {errorType})");
                    }
                    break;
                }

                case "shop_buy_part_direct_response":
                {
                    var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
                    if (ok)
                    {
                        var partType = doc.RootElement.TryGetProperty("partType", out var ptEl) ? ptEl.GetString() : "";
                        var costumeId = doc.RootElement.TryGetProperty("costumeId", out var cidEl) ? cidEl.GetString() : "";
                        Logger.log($"💳 [{partType}] {costumeId} 직접 구매 성공!");
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 직접 구매 실패: {msg} (코드: {errorType})");
                    }
                    break;
                }

                case "part_rates_response":
                {
                    var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
                    if (ok && doc.RootElement.TryGetProperty("rates", out var ratesEl))
                    {
                        var partType = doc.RootElement.TryGetProperty("partType", out var ptEl) ? ptEl.GetString() : "";
                        Logger.log($"=== [{partType}] 확률 ===");
                        foreach (var rate in ratesEl.EnumerateArray())
                        {
                            var costumeId = rate.TryGetProperty("costumeId", out var cidEl) ? cidEl.GetString() : "";
                            var rateValue = rate.TryGetProperty("rate", out var rvEl) ? rvEl.GetSingle() : 0f;
                            Logger.log($"  {costumeId}: {rateValue * 100:F2}%");
                        }
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 확률 조회 실패: {msg} (코드: {errorType})");
                    }
                    break;
                }

                case "change_costume_response":
                {
                    var ok = doc.RootElement.TryGetProperty("ok", out var okEl) && okEl.GetBoolean();
                    if (ok)
                    {
                        var partType = doc.RootElement.TryGetProperty("partType", out var ptEl) ? ptEl.GetString() : "";
                        var costumeId = doc.RootElement.TryGetProperty("costumeId", out var cidEl) ? cidEl.GetString() : "";
                        Logger.log($"✅ [{partType}] 코스튬 변경 성공: {costumeId}");
                    }
                    else
                    {
                        var errorType = doc.RootElement.TryGetProperty("errorType", out var etEl) ? etEl.GetString() : "";
                        var msg = doc.RootElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "";
                        Logger.log($"❌ 코스튬 변경 실패: {msg} (코드: {errorType})");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.log($" 메시지 처리 오류: {ex.Message}");
        }
    }

    static async Task LobbyCreateRoom()
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "create_room", roomName = "Test Room" });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbyJoinRoom(string? roomId)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        object data = string.IsNullOrEmpty(roomId)
            ? new { type = "join_room" }
            : new { type = "join_room", roomId };

        var msg = JsonSerializer.Serialize(data);
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbySetNickname(string nickname)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "set_nickname", nickname });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbyChangeCostume(string partType, string costumeId)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "change_costume", partType, costumeId });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbyShopBuyPartRoulette(string partType)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "shop_buy_part_roulette", partType });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbyShopBuyPartDirect(string partType, string costumeId)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "shop_buy_part_direct", partType, costumeId });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task LobbyGetPartRates(string partType)
    {
        if (lobbyWs == null || lobbyWs.State != WebSocketState.Open)
        {
            Logger.log($"Socket이 연결되지 않음");
            return;
        }

        var msg = JsonSerializer.Serialize(new { type = "get_part_rates", partType });
        await SendWebSocketMessage(lobbyWs, msg);
    }

    static async Task ConnectToServer(string host, int port)
    {
        try
        {
            if (isConnected)
            {
                Logger.log($"이미 서버에 연결되어 있습니다.");
                return;
            }

            client = new TcpClient();
            await client.ConnectAsync(host, port);

            // TCP NoDelay 설정 (Nagle's algorithm 비활성화)
            client.NoDelay = true;

            stream = client.GetStream();
            isConnected = true;

            Logger.log($"서버에 연결되었습니다: {host}:{port}");

            // JoinRequest 전송 (로비 세션 ID 사용)
            await SendPacketAsync(new JoinRequest
            {
                SessionId = sessionId ?? "test"
            });

            // 연결 상태 모니터링 시작
            _ = Task.Run(async () => await MonitorConnection());
        }
        catch (Exception ex)
        {
            Logger.log($"서버 연결 실패: {ex.Message}");
        }
    }

    static async Task MonitorConnection()
    {
        try
        {
            while (isConnected && client != null && client.Connected)
            {
                // 연결 상태 확인을 위한 작은 딜레이
                await Task.Delay(1000);

                // 스트림이 읽기 가능한지 확인
                if (stream != null && !stream.CanRead)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // 연결 오류 발생
        }
        finally
        {
            if (isConnected)
            {
                Logger.log($"서버와의 연결이 끊어졌습니다.");
                await Disconnect();
            }
        }
    }

    static async Task SendDirection(int dir)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        CalculatePositionSoFar();
        direction = dir;
        await SendMovementData();
    }

    static async Task SendShoot(int targetId)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new ShootingFromClient
            {
                TargetId = targetId
            });
        }
        catch (Exception ex)
        {
            Logger.log($"공격 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendAccuracyState(int state)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        if (state < -1 || state > 1)
        {
            Logger.log($"정확도 상태는 -1, 0, 1 중 하나여야 합니다.");
            return;
        }

        try
        {
            await SendPacketAsync(new UpdateAccuracyStateFromClient
            {
                AccuracyState = state
            });

            string stateText = state switch
            {
                -1 => "감소",
                0 => "유지",
                1 => "증가",
                _ => "알 수 없음"
            };
        }
        catch (Exception ex)
        {
            Logger.log($"정확도 상태 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendInitialRouletteDone()
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new InitialRouletteDoneFromClient());
        }
        catch (Exception ex)
        {
            Logger.log($"이니셜 룰렛 완료 신호 전송 실패: {ex.Message}");
        }
    }

    static async Task SendPurchaseItem(int itemId)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new PurchaseItemFromClient
            {
                ItemId = (ItemId)itemId
            });
        }
        catch (Exception ex)
        {
            Logger.log($"상점 아이템 구매 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendShopRoulette()
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new ShopRouletteFromClient());
        }
        catch (Exception ex)
        {
            Logger.log($"렛 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendMining(bool isMining)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new UpdateMiningStateFromClient
            {
                IsMining = isMining
            });
        }
        catch (Exception ex)
        {
            Logger.log($"채굴 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendLeave()
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new LeaveFromClient());
        }
        catch (Exception ex)
        {
            Logger.log($"게임 나가기 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendChat(string message)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Logger.log($"메시지를 입력해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new ChatFromClient
            {
                Message = message
            });
        }
        catch (Exception ex)
        {
            Logger.log($"채팅 메시지 전송 실패: {ex.Message}");
        }
    }

    static async Task SendUpdateRoomName(string roomName)
    {
        if (!isConnected || stream == null)
        {
            Logger.log($"먼저 서버에 연결해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(roomName))
        {
            Logger.log($"방 제목을 입력해주세요.");
            return;
        }

        try
        {
            await SendPacketAsync(new UpdateRoomNameFromClient
            {
                RoomName = roomName
            });
        }
        catch (Exception ex)
        {
            Logger.log($"방 제목 변경 요청 전송 실패: {ex.Message}");
        }
    }

    static async Task ReceiveLoop()
    {
        while (isRunning) // isConnected 대신 isRunning을 사용하도록 변경
        {
            if (stream != null && isConnected)
            {
                try
                {
                    var packet = await NetworkUtils.ReceivePacketAsync(stream);
                    Logger.log("⬅️", packet);
                    await HandlePacket(packet);
                }
                catch (Exception)
                {
                    Logger.log($"서버와의 연결이 끊어졌습니다.");
                    isConnected = false;
                    break;
                }
            }
        }
    }

    static async Task HandlePacket(IPacket packet)
    {
        try
        {
            switch (packet)
            {
                case JoinResponseFromServer youAre:
                    Logger.log($"서버에서 플레이어 ID 할당: {youAre.PlayerId}");
                    break;

                case JoinBroadcast joinBroadcast:
                    Logger.log($"플레이어 {joinBroadcast.PlayerInfo.PlayerId} 참가!");
                    Logger.log($"  닉네임: {joinBroadcast.PlayerInfo.Nickname}");
                    Logger.log($"  착용 코스튬:");
                    foreach (var costume in joinBroadcast.PlayerInfo.EquippedCostumes)
                    {
                        Logger.log($"    {costume.Key}: {costume.Value}");
                    }
                    break;

                case MovementDataBroadcast movementDataBroadcast:
                    Logger.log($"{movementDataBroadcast.PlayerId}번 플레이어 이동 데이터 갱신: 방향 {movementDataBroadcast.MovementData.Direction}, 위치 {movementDataBroadcast.MovementData.Position} 속도 {movementDataBroadcast.MovementData.Speed}");
                    break;

                case WaitingStartFromServer gameWaiting:
                    Logger.log($"=== 현재 방 상태 ===");
                    foreach (var info in gameWaiting.PlayersInfo)
                    {
                        Logger.log($"  플레이어 {info.PlayerId}: {info.Nickname}");
                        Logger.log($"    소지금: {info.Balance}달러");
                        Logger.log($"    위치: ({info.MovementData.Position.X:F2}, {info.MovementData.Position.Y:F2})");
                        Logger.log($"    명중률: {info.Accuracy}%");
                        Logger.log($"    정확도 상태: {info.AccuracyState} ({GetAccuracyStateText(info.AccuracyState)})");
                        Logger.log($"    속도: {info.MovementData.Speed:F2}");
                        Logger.log($"    사거리: {info.Range:F2}");
                        Logger.log($"    재장전시간: {info.RemainingReloadTime:F1}/{info.TotalReloadTime:F1}초");
                        Logger.log($"    생존: {(info.Alive ? "생존" : "사망")}");
                        Logger.log($"    정확도 비용: [감소:{info.AccuracyChangeCosts[0]:F1}, 유지:{info.AccuracyChangeCosts[1]:F1}, 증가:{info.AccuracyChangeCosts[2]:F1}]");
                        Logger.log($"    착용 코스튬:");
                        foreach (var costume in info.EquippedCostumes)
                        {
                            Logger.log($"      {costume.Key}: {costume.Value}");
                        }
                        if (info.OwnedItems.Count > 0)
                        {
                            Logger.log($"    아이템: [{string.Join(", ", info.OwnedItems)}]");
                        }
                        if (info.IsMining)
                        {
                            Logger.log($"    채굴 중: {info.MiningRemainingTime:F1}초 남음");
                        }
                    }

                    break;

                case RoundStartFromServer gamePlaying:
                    Logger.log($"=== 게임 진행 중 (라운드 {gamePlaying.Round}) ===");
                    foreach (var info in gamePlaying.PlayersInfo)
                    {
                        Logger.log($"  플레이어 {info.PlayerId}: {info.Nickname}");
                        Logger.log($"    소지금: {info.Balance}달러");
                        Logger.log($"    위치: ({info.MovementData.Position.X:F2}, {info.MovementData.Position.Y:F2})");
                        Logger.log($"    명중률: {info.Accuracy}%");
                        Logger.log($"    정확도 상태: {info.AccuracyState} ({GetAccuracyStateText(info.AccuracyState)})");
                        Logger.log($"    속도: {info.MovementData.Speed:F2}");
                        Logger.log($"    사거리: {info.Range:F2}");
                        Logger.log($"    재장전시간: {info.RemainingReloadTime:F1}/{info.TotalReloadTime:F1}초");
                        Logger.log($"    생존: {(info.Alive ? "생존" : "사망")}");
                        Logger.log($"    정확도 비용: [감소:{info.AccuracyChangeCosts[0]:F1}, 유지:{info.AccuracyChangeCosts[1]:F1}, 증가:{info.AccuracyChangeCosts[2]:F1}]");
                        Logger.log($"    착용 코스튬:");
                        foreach (var costume in info.EquippedCostumes)
                        {
                            Logger.log($"      {costume.Key}: {costume.Value}");
                        }
                        if (info.OwnedItems.Count > 0)
                        {
                            Logger.log($"    아이템: [{string.Join(", ", info.OwnedItems)}]");
                        }
                        if (info.IsMining)
                        {
                            Logger.log($"    채굴 중: {info.MiningRemainingTime:F1}초 남음");
                        }
                    }

                    break;

                case ShootingBroadcast shooting:
                    var hitMsg = shooting.Hit ? "명중!" : "빗나감";
                    Logger.log($"플레이어 {shooting.ShooterId}가 플레이어 {shooting.TargetId}를 공격 - {hitMsg}");
                    break;

                case UpdatePlayerAlive aliveUpdate:
                    var statusMsg = aliveUpdate.Alive ? "부활" : "사망";
                    Logger.log($"플레이어 {aliveUpdate.PlayerId} {statusMsg}");
                    break;

                case NewHostBroadcast newHost:
                    Logger.log($"새로운 방장: {newHost.HostId}");
                    break;

                case LeaveBroadcast leaveBroadcast:
                    Logger.log($"플레이어 {leaveBroadcast.PlayerId}가 게임을 떠났습니다");
                    break;

                case UpdateAccuracyStateBroadcast accuracyStateBroadcast:
                    Logger.log($"플레이어 {accuracyStateBroadcast.PlayerId}의 정확도 상태 변경: {accuracyStateBroadcast.AccuracyState} ({GetAccuracyStateText(accuracyStateBroadcast.AccuracyState)})");
                    break;

                case InitialRouletteStartFromServer initialRoulette:
                    Logger.log($"🎯 [이니셜 룰렛] 이니셜 룰렛 시작!");
                    Logger.log($"현재 내 정확도: {initialRoulette.YourAccuracy}%");
                    Logger.log($"룰렛 풀: [{string.Join(", ", initialRoulette.AccuracyPool)}]");
                    Logger.log($"명령어 'ir' 또는 'initial-roulette'로 완료 신호를 보내세요.");
                    break;

                case ShopStartFromServer shopStart:
                    Logger.log($"🛒 [상점] 상점 시스템 시작! ({shopStart.Duration}초 지속)");
                    Logger.log($"내 전용 아이템:");
                    for (int i = 0; i < shopStart.ShopData.YourItems.Count; i++)
                    {
                        var item = shopStart.ShopData.YourItems[i];
                        Logger.log($"  {item.ItemId}: {item.Name} - {item.Description} ({item.Price}달러)");
                    }
                    Logger.log($"샵룰렛 가격: {shopStart.ShopData.ShopRoulettePrice}달러");
                    Logger.log($"샵룰렛 풀: [{string.Join(", ", shopStart.ShopData.ShopRoulettePool)}]");
                    Logger.log($"명령어: 'pi [itemId]' 또는 'sr'");
                    break;

                case ShopDataUpdateFromServer shopDataUpdate:
                    Logger.log($"🛒 [상점 업데이트] 아이템 목록 갱신:");
                    for (int i = 0; i < shopDataUpdate.ShopData.YourItems.Count; i++)
                    {
                        var item = shopDataUpdate.ShopData.YourItems[i];
                        var status = item.ItemId == (ItemId)(-1) ? "구매 완료" : "구매 가능";
                        Logger.log($"  {i}: {item.Name} - {item.Description} ({item.Price}달러) [{status}]");
                    }
                    break;

                case ShopRouletteResultFromServer shopRouletteResult:
                    Logger.log($"🎲 [샵룰렛 결과] 새로운 정확도: {shopRouletteResult.NewAccuracy}% (비용: {shopRouletteResult.ShopRoulettePrice}달러)");
                    break;

                // case StakeUpdateBroadcast bettingDeduction:
                //     Logger.log($"💰 현재 총 판돈: {bettingDeduction.TotalPrizeMoney}달러");
                //     break;
                //
                // case BalanceUpdateBroadcast balanceUpdate:
                //     Logger.log($"💳 플레이어 {balanceUpdate.PlayerId}의 소지금 업데이트: {balanceUpdate.Balance}달러");
                //     break;

                case RoundWinnerBroadcast roundWinner:
                    if (roundWinner.PlayerIds != null && roundWinner.PlayerIds.Count > 0)
                    {
                        var winnerText = roundWinner.PlayerIds.Count == 1
                            ? $"플레이어 {roundWinner.PlayerIds[0]}"
                            : $"플레이어 [{string.Join(", ", roundWinner.PlayerIds)}]";
                        Logger.log($"🏆 [라운드 {roundWinner.Round} 승리] {winnerText}가 {roundWinner.PrizeMoney}달러 획득!");
                    }
                    else
                    {
                        Logger.log($"🏆 [라운드 {roundWinner.Round}] 승리자 없음!");
                    }

                    break;

                case GameWinnerBroadcast gameWinner:
                    Logger.log($"🎊 ━━━ [게임 종료] 최종 순위 발표 ━━━");
                    // if (gameWinner.WinnerId != null && gameWinner.Rankings.Count > 0)
                    // {
                    //     foreach (var ranking in gameWinner.Rankings)
                    //     {
                    //         var medalEmoji = ranking.Rank switch
                    //         {
                    //             1 => "🥇",
                    //             2 => "🥈",
                    //             3 => "🥉",
                    //             _ => "  "
                    //         };
                    //
                    //         var rewardText = ranking.RewardMoney > 0
                    //             ? $" (+{ranking.RewardMoney}💰)"
                    //             : "";
                    //
                    //         Logger.log($"{medalEmoji} {ranking.Rank}등 - 플레이어 {ranking.PlayerId}{rewardText}");
                    //     }
                    // }
                    // else
                    // {
                    //     Logger.log($"순위 정보 없음");
                    // }
                    // Logger.log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    break;

                case MiningStateBroadcast miningState:
                    var miningAction = miningState.IsMining ? "시작" : "중단";
                    Logger.log($"⛏️ [채굴] 플레이어 {miningState.PlayerId}가 채굴을 {miningAction}했습니다");
                    break;

                // case MiningCompleteBroadcast miningComplete:
                //     Logger.log($"💰 [채굴 완료] 플레이어 {miningComplete.PlayerId}가 채굴로 {miningComplete.RewardAmount}달러를 획득했습니다!");
                //     break;

                case ChatBroadcast chatBroadcast:
                    if (chatBroadcast.PlayerId == -1)
                    {
                        Logger.log($"💬 [시스템] {chatBroadcast.Message}");
                    }
                    else
                    {
                        Logger.log($"💬 [플레이어 {chatBroadcast.PlayerId}] {chatBroadcast.Message}");
                    }

                    break;

                case ReloadTimeBroadcast reloadTime:
                    Logger.log($"🔄 [재장전시간 업데이트] 플레이어 {reloadTime.PlayerId}의 재장전시간: {reloadTime.RemainingReloadTime:F1}/{reloadTime.TotalReloadTime:F1}초");
                    break;

                case AccuracyChangeCostBroadcast accuracyCost:
                    Logger.log($"💰 [정확도 비용 업데이트] 플레이어 {accuracyCost.PlayerId}: 감소 {accuracyCost.AccuracyChangeCosts[0]:F1}, 유지 {accuracyCost.AccuracyChangeCosts[1]:F1}, 증가 {accuracyCost.AccuracyChangeCosts[2]:F1}");
                    break;

                case UpdateAccuracyBroadcast accuracyUpdate:
                    Logger.log($"🎯 [정확도 업데이트] 플레이어 {accuracyUpdate.PlayerId}의 정확도: {accuracyUpdate.Accuracy}%");
                    break;

                case UpdateRangeBroadcast rangeUpdate:
                    Logger.log($"📏 [사거리 업데이트] 플레이어 {rangeUpdate.PlayerId}의 사거리: {rangeUpdate.Range:F2}");
                    break;

                case UpdateSpeedBroadcast speedUpdate:
                    Logger.log($"🏃 [속도 업데이트] 플레이어 {speedUpdate.PlayerId}의 속도: {speedUpdate.Speed:F2}");
                    break;

                case MapData mapData:
                    Logger.log($"🗺️ [맵 데이터] 맵 인덱스: {mapData.MapIndex}");
                    break;

                case PlayerIsTargetingBroadcast targeting:
                    Logger.log($"🎯 [조준] 플레이어 {targeting.ShooterId}가 플레이어 {targeting.TargetId}를 조준 중");
                    break;

                case UpdateOwnedItemsBroadcast ownedItems:
                    Logger.log($"🎒 [아이템 목록 업데이트] 플레이어 {ownedItems.PlayerId}의 아이템: [{string.Join(", ", ownedItems.OwnedItems)}]");
                    break;

                case PingPacket:
                    Logger.log($"🏓 [Ping] 서버로부터 핑 수신");
                    await SendPacketAsync(new PongPacket());
                    break;

                case PongPacket:
                    Logger.log($"🏓 [Pong] 서버로부터 퐁 수신");
                    break;

                case UpdateRoomNameResponse updateRoomNameResponse:
                    if (updateRoomNameResponse.ok)
                    {
                        Logger.log($"✅ [방 제목 변경 성공]");
                    }
                    else
                    {
                        Logger.log($"❌ [방 제목 변경 실패] {updateRoomNameResponse.errorMessage}");
                    }
                    break;

                case UpdateRoomNameBroadcast updateRoomNameBroadcast:
                    Logger.log($"🚪 [방 제목 변경됨] {updateRoomNameBroadcast.RoomName}");
                    break;

                default:
                    Logger.log($"처리되지 않은 패킷 타입: {packet}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.log($"패킷 처리 오류: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    static string GetAccuracyStateText(int state)
    {
        return state switch
        {
            -1 => "감소",
            0 => "유지",
            1 => "증가",
            _ => "알 수 없음"
        };
    }


    static async Task Disconnect()
    {
        try
        {
            isConnected = false;
            stream?.Close();
            client?.Close();
            Logger.log($"서버와의 연결을 해제했습니다.");
        }
        catch (Exception ex)
        {
            Logger.log($"연결 해제 오류: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    static async Task StartGame()
    {
        await SendPacketAsync(new StartGameFromClient());
    }

    static async Task SendPacketAsync(IPacket packet)
    {
        Logger.log("➡️", packet);
        await NetworkUtils.SendPacketAsync(stream!, packet);
    }
}