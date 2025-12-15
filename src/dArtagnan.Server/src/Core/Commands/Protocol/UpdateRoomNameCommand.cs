using dArtagnan.Shared;

namespace dArtagnan.Server;

public class UpdateRoomNameCommand : IGameCommand
{
    required public int PlayerId;
    required public string RoomName;

    public async Task ExecuteAsync(GameManager gameManager)
    {
        var player = gameManager.GetPlayerById(PlayerId);
        if (player == null)
        {
            Logger.log($"[Room][Unknown] 방 이름 변경 실패: 플레이어를 찾을 수 없음 (Client-{PlayerId})");
            return;
        }

        // 방장 권한 확인
        if (gameManager.Host?.Id != PlayerId)
        {
            Logger.log($"[Room][{player.Nickname}] 방 이름 변경 실패: 방장 권한 없음");
            await gameManager.SendToPlayer(PlayerId, new UpdateRoomNameResponse
            {
                ok = false,
                errorMessage = "방장만 방 제목을 변경할 수 있습니다."
            });
            return;
        }

        // 방 제목 검증 (형식 + 길이 + 욕설)
        var (isValid, reason) = ProfanityFilter.ValidateRoomName(RoomName);
        if (!isValid)
        {
            Logger.log($"[Room][{player.Nickname}] 방 이름 변경 실패: {reason}");
            await gameManager.SendToPlayer(PlayerId, new UpdateRoomNameResponse
            {
                ok = false,
                errorMessage = reason
            });
            return;
        }

        var newRoomName = RoomName!.Trim();

        // 방 이름 업데이트 (로비 서버 리포트 포함)
        Logger.log($"[Room][{player.Nickname}] 방 이름 변경 성공: \"{newRoomName}\"");
        await gameManager.UpdateRoomName(newRoomName);

        // 성공 응답 전송 (요청한 클라이언트에게만)
        await gameManager.SendToPlayer(PlayerId, new UpdateRoomNameResponse
        {
            ok = true,
            errorMessage = ""
        });

        // 브로드캐스트 전송 (모든 클라이언트에게)
        await gameManager.BroadcastToAll(new UpdateRoomNameBroadcast
        {
            RoomName = newRoomName
        });
    }
}
