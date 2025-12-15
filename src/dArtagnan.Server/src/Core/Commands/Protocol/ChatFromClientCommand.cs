using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 플레이어 채팅 명령
/// </summary>
public class ChatFromClientCommand : IGameCommand
{
    required public int PlayerId;
    required public string Message;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null)
        {
            Logger.log($"[Chat] 존재하지 않는 플레이어 ID: {PlayerId}");
            return;
        }

        // 채팅 요청 로그
        Logger.log($"[Chat][{player.Nickname}] 채팅 요청: \"{Message}\"");

        // 메시지 검증 (형식 + 길이 + 욕설)
        var (isValid, reason) = ProfanityFilter.ValidateChatMessage(Message);
        if (!isValid)
        {
            Logger.log($"[Chat][{player.Nickname}] ❌ 필터링됨: {reason}");
            await gameManager.SendToPlayer(PlayerId, new ChatBroadcast
            {
                PlayerId = -1,
                Message = $"채팅 전송 실패: {reason}"
            });
            return;
        }

        // 정상 메시지 브로드캐스트
        Logger.log($"[Chat][{player.Nickname}] ✅ 전송 성공");
        await gameManager.BroadcastToAll(new ChatBroadcast
        {
            PlayerId = PlayerId,
            Message = Message.Trim()
        });
    }
}