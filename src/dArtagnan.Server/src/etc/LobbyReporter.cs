using System.Net.Http;
using System.Text;
using System.Text.Json;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public static class LobbyReporter
{
    private static readonly HttpClient http = new HttpClient();
    private static string? internalApiSecret = null;
    private static bool isInitialized = false;

    private static void EnsureInitialized()
    {
        if (!isInitialized)
        {
            internalApiSecret = Environment.GetEnvironmentVariable("INTERNAL_API_SECRET");

            // INTERNAL_API_SECRET이 설정되어 있으면 Authorization 헤더 추가
            if (!string.IsNullOrWhiteSpace(internalApiSecret))
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {internalApiSecret}");
                Logger.log("[Lobby][System] 내부 API 인증 활성화");
            }
            else
            {
                Logger.log("[Lobby][System] INTERNAL_API_SECRET 미설정 - 로컬 모드");
            }

            isInitialized = true;
        }
    }

    public static async void ReportState(int state)
    {
        try
        {
            EnsureInitialized();

            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 상태 리포트 호출: state={state}, roomId={roomId?.Substring(0, Math.Min(6, roomId.Length))}");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/rooms/{roomId}/state";
            var payload = JsonSerializer.Serialize(new { state });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 상태 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 상태 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 상태 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportPlayerCount(int playerCount)
    {
        try
        {
            EnsureInitialized();

            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 인원수 리포트 호출: {playerCount}명");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/rooms/{roomId}/player-count";
            var payload = JsonSerializer.Serialize(new { playerCount });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 인원수 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 인원수 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 인원수 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportBulkGameResults(List<PlayerGameResult> players)
    {
        try
        {
            EnsureInitialized();

            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 벌크 게임 결과 리포트 호출: {players.Count}명");

            if (string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/game-results";
            var request = new BulkGameResultRequest { Players = players };
            var payload = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 벌크 게임 결과 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 벌크 게임 결과 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 벌크 게임 결과 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportPlayerJoin(string providerId)
    {
        try
        {
            EnsureInitialized();

            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 플레이어 입장 리포트: providerId={providerId}");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return;
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/player-join";
            var payload = JsonSerializer.Serialize(new { providerId, roomId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 플레이어 입장 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 플레이어 입장 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 플레이어 입장 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportPlayerLeave(string providerId)
    {
        try
        {
            EnsureInitialized();

            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 플레이어 퇴장 리포트: providerId={providerId}");

            if (string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return;
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/player-leave";
            var payload = JsonSerializer.Serialize(new { providerId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 플레이어 퇴장 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 플레이어 퇴장 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 플레이어 퇴장 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportRoomName(string newRoomName)
    {
        try
        {
            EnsureInitialized();

            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 방 이름 리포트 호출: newRoomName=\"{newRoomName}\"");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return;
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/rooms/{roomId}/room-name";
            var payload = JsonSerializer.Serialize(new { roomName = newRoomName });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 방 이름 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 방 이름 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 방 이름 리포트 예외: {e.Message}");
        }
    }

    public static async Task<SessionValidationResult?> ValidateSessionAsync(string sessionToken)
    {
        try
        {
            EnsureInitialized();

            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 세션 검증 호출: sessionToken={sessionToken?.Substring(0, Math.Min(10, sessionToken?.Length ?? 0))}...");

            if (string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 검증 스킵 (로컬 모드)");
                return null; // 로컬/개발 모드
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/validate-game-session";
            var payload = JsonSerializer.Serialize(new { sessionToken });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 세션 검증 실패: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SessionValidationResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null && result.Ok)
            {
                Logger.log($"[Lobby][System] 세션 검증 성공: {result.Nickname}");
                return result;
            }

            Logger.log("[Lobby][System] 세션 검증 실패: 응답 데이터 없음");
            return null;
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 세션 검증 예외: {e.Message}");
            return null;
        }
    }
}

public class SessionValidationResult
{
    public bool Ok { get; set; }
    public string ProviderId { get; set; } = "";
    public string Nickname { get; set; } = "";
    public Dictionary<string, string> EquippedCostumes { get; set; } = new();
}


