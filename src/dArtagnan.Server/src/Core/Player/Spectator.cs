namespace dArtagnan.Server;

/// <summary>
/// 관전자 정보 - 읽기 전용 수신자
/// 관전자는 게임에 영향을 주지 않으며 모든 브로드캐스트 패킷만 수신합니다
/// </summary>
public class Spectator
{
    public int Id { get; init; }
    public string Nickname { get; init; }
    public string ProviderId { get; init; }

    public Spectator(int id, string nickname, string providerId)
    {
        Id = id;
        Nickname = nickname;
        ProviderId = providerId;
    }
}
