using System;
using System.Collections.Generic;
using System.Numerics;
using MessagePack;

namespace dArtagnan.Shared
{
    // 연결 및 세션 관리
    [Union(0, typeof(JoinRequest))]
    [Union(1, typeof(JoinResponseFromServer))]
    [Union(2, typeof(JoinBroadcast))]
    [Union(3, typeof(LeaveFromClient))]
    [Union(4, typeof(LeaveBroadcast))]
    [Union(5, typeof(PingPacket))]
    [Union(6, typeof(PongPacket))]

    // 이동 및 위치
    [Union(7, typeof(MovementDataFromClient))]
    [Union(8, typeof(MovementDataBroadcast))]

    // 전투 시스템
    [Union(9, typeof(ShootingFromClient))]
    [Union(10, typeof(ShootingBroadcast))]
    [Union(11, typeof(PlayerIsTargetingFromClient))]
    [Union(12, typeof(PlayerIsTargetingBroadcast))]

    // 게임 상태 관리
    [Union(13, typeof(StartGameFromClient))]
    [Union(14, typeof(WaitingStartFromServer))]
    [Union(15, typeof(RoundStartFromServer))]
    [Union(16, typeof(NewHostBroadcast))]
    [Union(17, typeof(RoundWinnerBroadcast))]
    [Union(18, typeof(GameWinnerBroadcast))]
    [Union(59, typeof(GameResultsBroadcast))]

    // 플레이어 스탯
    [Union(20, typeof(UpdatePlayerAlive))]
    [Union(22, typeof(UpdateAccuracyStateFromClient))]
    [Union(23, typeof(UpdateAccuracyStateBroadcast))]
    [Union(24, typeof(UpdateAccuracyBroadcast))]
    [Union(25, typeof(UpdateRangeBroadcast))]
    [Union(26, typeof(UpdateSpeedBroadcast))]
    [Union(27, typeof(UpdateOwnedItemsBroadcast))]
    [Union(28, typeof(ReloadTimeBroadcast))]
    [Union(29, typeof(MiningStateBroadcast))]
    [Union(30, typeof(AccuracyChangeCostBroadcast))]
    [Union(47, typeof(FuryStateBroadcast))]
    [Union(50, typeof(BankruptBroadcast))]

    // 초기 룰렛 시스템
    [Union(31, typeof(InitialRouletteStartFromServer))]
    [Union(32, typeof(InitialRouletteDoneFromClient))]

    // 상점 시스템
    [Union(33, typeof(ShopStartFromServer))]
    [Union(34, typeof(PurchaseItemFromClient))]
    [Union(35, typeof(ShopDataUpdateFromServer))]
    [Union(36, typeof(ShopRouletteFromClient))]
    [Union(37, typeof(ShopRouletteResultFromServer))]
    [Union(38, typeof(ItemUsedBroadcast))]
    [Union(45, typeof(ShopTimeElapsed))]

    // 채굴 시스템
    [Union(39, typeof(UpdateMiningStateFromClient))]

    // 채팅 시스템
    [Union(41, typeof(ChatFromClient))]
    [Union(42, typeof(ChatBroadcast))]
    [Union(43, typeof(MapData))]

    // 방 관리
    [Union(48, typeof(UpdateRoomNameFromClient))]
    [Union(49, typeof(UpdateRoomNameResponse))]
    [Union(51, typeof(UpdateRoomNameBroadcast))]
    
    // 소지금 관련 애니메이션 패킷
    [Union(52, typeof(LootBroadcast2))]
    [Union(53, typeof(MineResultBroadcast))]
    [Union(54, typeof(TaxBroadcast))]
    [Union(56, typeof(StakesUpdateBroadcast))]
    [Union(57, typeof(UpdatePlayerNextTaxAmount))]
    [Union(58, typeof(PlayerBalanceUpdate))]
    public interface IPacket
    {
    }

    #region 공통 데이터 구조

    /// <summary>
    /// [서버 => 클라이언트]
    /// MapIndex번 맵을 사용하겠다.
    /// </summary>
    [MessagePackObject]
    public struct MapData : IPacket
    {
        [Key(0)] public int MapIndex;
    }

    /// <summary>
    /// 상점 아이템 정보
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public struct ShopItem
    {
        [Key(0)] public ItemId ItemId; // 아이템 ID
        [Key(1)] public int Price; // 가격
        [Key(2)] public string Name; // 아이템 이름
        [Key(3)] public string Description; // 아이템 설명
    }

    [MessagePackObject]
    public struct ShopData
    {
        [Key(0)] public List<ShopItem> YourItems; // 당신만의 3개 아이템
        [Key(1)] public int ShopRoulettePrice; // 샵룰렛 가격
        [Key(2)] public List<int> ShopRoulettePool; // 당신만의 샵룰렛 풀 8개 (25~75%)
    }

    /// <summary>
    /// 특정 플레이어의 정보를 담은 구조체
    /// </summary>
    [MessagePackObject]
    public struct PlayerInformation
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public string Nickname;
        [Key(2)] public int Accuracy; // 명중률 (1~70%, 아이템으로 확장 가능)
        [Key(3)] public bool Alive;
        [Key(4)] public int Targeting; // 겨누는 플레이어의 번호. 겨누는 플레이어가 없을 경우 -1
        [Key(5)] public float Range; // 사거리 (명중률에 반비례)
        [Key(6)] public MovementData MovementData;
        [Key(7)] public int Balance; // 소지금
        [Key(8)] public int AccuracyState; // -1: 감소, 0: 유지, 1: 증가
        [Key(9)] public List<int> AccuracyChangeCosts; // 명중률 증감 비용 [감소, 유지, 증가] 순서
        [Key(10)] public float TotalReloadTime; // 총 재장전시간 (사격 시 설정)
        [Key(11)] public float RemainingReloadTime; // 현재 남은 재장전시간
        [Key(12)] public Dictionary<string, string> EquippedCostumes; // 착용 중인 코스튬 (파츠별)
        [Key(13)] public bool IsMining; // 채굴 중 여부
        [Key(14)] public float MiningRemainingTime; // 채굴 남은 시간
        [Key(15)] public List<ItemId> OwnedItems; // 소유 아이템 ID 리스트
        [Key(16)] public int NextDeductionAmount;
        [Key(17)] public bool Fury; // 폭주 모드 여부 (다음 징수금 >= 소지금)
    }

    /// <summary>
    /// 이동 정보. 단독으로는 쓰이지 않고 다른 패킷에 담겨서 쓰인다
    /// </summary>
    [MessagePackObject]
    public struct MovementData : IEquatable<MovementData>
    {
        [Key(0)] public int Direction;
        [Key(1)] public Vector2 Position;
        [Key(2)] public float Speed; //시뮬되어야 하는 최종 계산된 speed

        public bool Equals(MovementData other)
        {
            return Direction == other.Direction && Position.Equals(other.Position) && Speed.Equals(other.Speed);
        }

        public override bool Equals(object? obj)
        {
            return obj is MovementData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Direction, Position, Speed);
        }

        public static bool operator ==(MovementData left, MovementData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MovementData left, MovementData right)
        {
            return !left.Equals(right);
        }
    }


    #endregion

    #region 연결 및 세션 관리 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 게임에 접속하고 싶을 때 보내는 패킷
    /// 로비 서버에서 발급받은 게임 세션 토큰을 전송
    /// </summary>
    [MessagePackObject]
    public struct JoinRequest : IPacket
    {
        [Key(0)] public string SessionId;
        [Key(1)] public bool AsSpectator; // 관전자로 참가 여부
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// JorinRequest의 응답 패킷
    /// 플레이어가 자신의 번호가 몇 번인지를 알 수 있도록 보내주는 패킷
    /// </summary>
    [MessagePackObject]
    public struct JoinResponseFromServer : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public string RoomName;
        [Key(2)] public string RoomPassword;
        [Key(3)] public bool IsSpectator; // 관전자 여부
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 특정 플레이어가 접속했음을 알려주는 패킷
    /// </summary>
    [MessagePackObject]
    public struct JoinBroadcast : IPacket
    {
        [Key(0)] public PlayerInformation PlayerInfo;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어 퇴장 통보
    /// </summary>
    [MessagePackObject]
    public struct LeaveFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 플레이어 퇴장 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct LeaveBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 연결 상태 확인을 위한 핑 패킷
    /// </summary>
    [MessagePackObject]
    public struct PingPacket : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 핑에 대한 응답 패킷
    /// </summary>
    [MessagePackObject]
    public struct PongPacket : IPacket
    {
    }

    #endregion

    #region 이동 및 위치 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 자신의 이동 방향, 위치를 서버에 보내줄 때 쓰는 패킷
    /// </summary>
    [MessagePackObject]
    public struct MovementDataFromClient : IPacket
    {
        [Key(1)] public MovementData MovementData;
    }

    /// <summary>
    /// [서버 => 브로드캐스트]
    /// 특정 플레이어의 이동 정보를 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct MovementDataBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public MovementData MovementData;
    }

    #endregion

    #region 전투 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어가 TargetId번을 쏘겠다고 요청
    /// </summary>
    [MessagePackObject]
    public struct ShootingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번이 TargetId번을 사격했으며 그 결과는 Hit이고 방어됐는지 여부는 Guarded다.
    /// </summary>
    [MessagePackObject]
    public struct ShootingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
        [Key(2)] public bool Hit;
        [Key(3)] public bool Guarded;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// TargetId번 플레이어를 겨누고 있음
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingFromClient : IPacket
    {
        [Key(0)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// ShooterId번 플레이어가 TargetId번 플레이어를 겨누고 있음
    /// </summary>
    [MessagePackObject]
    public struct PlayerIsTargetingBroadcast : IPacket
    {
        [Key(0)] public int ShooterId;
        [Key(1)] public int TargetId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 생존 여부를 보낸다
    /// </summary>
    [MessagePackObject]
    public struct UpdatePlayerAlive : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool Alive;
    }

    #endregion

    #region 돈 관련 패킷들

    [MessagePackObject]
    public struct LootBroadcast2 : IPacket
    {
        [Key(0)] public int FromPlayerId;
        [Key(1)] public int ToPlayerId;
        [Key(2)] public int LootAmount;
        [Key(3)] public int VanishedAmount;
        [Key(4)] public int FromPlayerBalanceBefore;
        [Key(5)] public int FromPlayerBalanceAfter;
        [Key(6)] public int ToPlayerBalanceBefore;
        [Key(7)] public int ToPlayerBalanceAfter;
    }

    [MessagePackObject]
    public struct MineResultBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int RewardAmount;
        [Key(2)] public int PlayerBalanceBefore;
        [Key(3)] public int PlayerBalanceAfter;
    }

    [MessagePackObject]
    public struct TaxBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int TaxAmount;
        [Key(2)] public int PlayerBalanceBefore;
        [Key(3)] public int PlayerBalanceAfter;
    }

    [MessagePackObject]
    public struct StakesUpdateBroadcast : IPacket
    {
        [Key(0)] public int Delta;
        [Key(1)] public int StakeBefore;
        [Key(2)] public int StakeAfter;
    }

    [MessagePackObject]
    public struct UpdatePlayerNextTaxAmount : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int TaxAmount;
    }

    [MessagePackObject]
    public struct PlayerBalanceUpdate : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Delta;
        [Key(2)] public int BalanceBefore;
        [Key(3)] public int BalanceAfter;
    }
    #endregion
    
    #region 게임 상태 관리 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 게임을 시작하겠다는 패킷. 방장만 전송 가능
    /// </summary>
    [MessagePackObject]
    public struct StartGameFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Waiting' 상태를 시작한다. 플레이어들의 상태는 PlayersInfo와 같다
    /// 클라이언트가 방에 처음 입장하거나 게임이 종료된 후 대기 상태로 돌아갈 때만 보내진다
    /// </summary>
    [MessagePackObject]
    public struct WaitingStartFromServer : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 'Round' 상태를 시작한다. 플레이어들의 상태는 PlayersInfo와 같고 Round번째 라운드를 진행 중이다
    /// 게임이 시작되거나 각 라운드가 시작될 때만 보내진다
    /// 각 플레이어의 차감 금액은 PlayerInformation.NextDeductionAmount에 포함되어 있다
    /// </summary>
    [MessagePackObject]
    public struct RoundStartFromServer : IPacket
    {
        [Key(0)] public List<PlayerInformation> PlayersInfo;
        [Key(1)] public int Round;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// HostId번 플레이어가 새로 방장이 되었다
    /// </summary>
    [MessagePackObject]
    public struct NewHostBroadcast : IPacket
    {
        [Key(0)] public int HostId;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어들이 라운드에서 승리했다
    /// 이 패킷을 받으면 단순히 게임승리 UI만 띄우면 된다
    /// </summary>
    [MessagePackObject]
    public struct RoundWinnerBroadcast : IPacket
    {
        [Key(0)] public List<int> PlayerIds; // 라운드 승자들
        [Key(1)] public int Round; // 라운드 번호
        [Key(2)] public int PrizeMoney; // 획득한 판돈
        [Key(3)] public int WinnerBalanceBefore;
        [Key(4)] public int WinnerBalanceAfter;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 게임 최종 승자 발표 (1등만, 없으면 -1)
    /// </summary>
    [MessagePackObject]
    public struct GameWinnerBroadcast : IPacket
    {
        [Key(0)] public int WinnerId; // 최종 승자 플레이어 ID (없으면 -1)
        [Key(1)] public int RewardMoney; // 최종 승자의 보상금 (승자가 없으면 0)
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 게임 최종 순위 및 보상 종합 정보
    /// </summary>
    [MessagePackObject]
    public struct GameResultsBroadcast : IPacket
    {
        [Key(0)] public List<PlayerRanking> Rankings; // 플레이어별 순위 및 보상 정보
    }

    /// <summary>
    /// 플레이어 개별 순위 정보
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public struct PlayerRanking
    {
        [Key(0)] public int Rank; // 순위 (1~8)
        [Key(1)] public int PlayerId; // 플레이어 ID    //클라에서 안쓰고 있는 필드
        [Key(2)] public int RewardMoney; // 받는 상금
        [Key(3)] public string Nickname; // 플레이어 닉네임
    }

    #endregion

    #region 플레이어 스탯 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어가 파산했다.
    /// </summary>
    [MessagePackObject]
    public struct BankruptBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 플레이어의 정확도 증감 상태를 변경하겠다고 요청
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyStateFromClient : IPacket
    {
        [Key(0)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 정확도 상태가 AccuracyState로 변경되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int AccuracyState; // -1: 정확도 감소, 0: 정확도 유지, 1: 정확도 증가
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 정확도가 Accuracy로 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateAccuracyBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public int Accuracy;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 사거리가 Range로 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateRangeBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float Range;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 속도가 Speed로 업데이트 되었다
    /// Deprecated: UpdateMovementDataBroadcast로 통합
    /// </summary>
    [MessagePackObject]
    public struct UpdateSpeedBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float Speed;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 소유 아이템 목록이 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct UpdateOwnedItemsBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public List<ItemId> OwnedItems; // 소유 아이템 ID 목록
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 폭주 모드 상태가 변경되었다
    /// </summary>
    [MessagePackObject]
    public struct FuryStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool Fury; // 폭주 모드 여부
    }

    #endregion

    #region 장전시간 및 비용 시스템 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 재장전시간이 업데이트 되었다 (최소 사격 시마다 발생)
    /// </summary>
    [MessagePackObject]
    public struct ReloadTimeBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public float TotalReloadTime; // 총 재장전시간
        [Key(2)] public float RemainingReloadTime; // 현재 남은 재장전시간
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// PlayerId번 플레이어의 명중률 증감 비용이 업데이트 되었다
    /// </summary>
    [MessagePackObject]
    public struct AccuracyChangeCostBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public List<int> AccuracyChangeCosts; // [감소, 유지, 증가] 비용
    }

    #endregion

    #region 초기 룰렛 시스템 패킷들

    /// <summary>
    /// [서버 => 클라이언트]
    /// 초기 룰렛 시작 - 정확도 풀 제공, 플레이어가 룰렛을 돌릴 때까지 대기
    /// YourAccuracy: 현재 할당된 정확도 (디스플레이용)
    /// AccuracyPool: 룰렛 풀 (25~75% 범위 8개)
    /// </summary>
    [MessagePackObject]
    public struct InitialRouletteStartFromServer : IPacket
    {
        [Key(0)] public int YourAccuracy; // 현재 정확도
        [Key(1)] public List<int> AccuracyPool; // 룰렛 풀
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 초기 룰렛 돌렸다는 신호 (빈 패킷)
    /// 서버는 모든 클라이언트가 이 패킷을 보낼 때까지 대기 후 라운드 시작
    /// </summary>
    [MessagePackObject]
    public struct InitialRouletteDoneFromClient : IPacket
    {
    }

    #endregion

    #region 상점 시스템 패킷들

    /// <summary>
    /// [서버 => 개별 클라이언트]
    /// 상점 시작 - 각 플레이어마다 다른 아이템과 샵룰렛 풀 제공
    /// 비어있는 아이템칸은 ItemId가 -1
    /// Duration은 상점 지속 시간
    /// </summary>
    [MessagePackObject]
    public struct ShopStartFromServer : IPacket
    {
        [Key(0)] public int Duration; // 상점 지속 시간
        [Key(1)] public ShopData ShopData; // 상점 데이터
        [Key(2)] public bool YouAreBankrupt;
    }

    /// <summary>
    /// [서버 => 개별 클라이언트]
    /// 상점 데이터 업데이트
    /// </summary>
    [MessagePackObject]
    public struct ShopDataUpdateFromServer : IPacket
    {
        [Key(0)] public ShopData ShopData; // 상점 데이터
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 상점에서 아이템 구매 요청
    /// </summary>
    [MessagePackObject]
    public struct PurchaseItemFromClient : IPacket
    {
        [Key(0)] public ItemId ItemId; // 구매할 아이템 ID
    }

    /// <summary>
    /// [클라이언트 => 서버]
    /// 상점에서 샵룰렛 요청 (빈 패킷)
    /// </summary>
    [MessagePackObject]
    public struct ShopRouletteFromClient : IPacket
    {
    }

    /// <summary>
    /// [서버 => 개별 클라이언트]
    /// 샵룰렛 결과 - 어떤 정확도가 나왔는지와 비용을 알려줌
    /// </summary>
    [MessagePackObject]
    public struct ShopRouletteResultFromServer : IPacket
    {
        [Key(0)] public int NewAccuracy; // 샵룰렛 결과로 나온 새로운 정확도
        [Key(1)] public int ShopRoulettePrice; // 샵룰렛 비용
    }
    
    /// <summary>
    /// [서버 => 클라이언트]
    /// 아이템 사용 알림 (단순 클라이언트 UI업데이트 용)
    /// </summary>
    [MessagePackObject]
    public struct ItemUsedBroadcast : IPacket
    {
        [Key(0)] public int PlayerId; // 사용한 플레이어 ID
        [Key(1)] public ItemId ItemId; // 사용한 아이템 ID
        [Key(2)] public bool Success; // 성공 여부 (성패가 갈리는 아이템인 경우)
    }

    [MessagePackObject]
    public struct ShopTimeElapsed : IPacket
    {
        [Key(0)] public float ElapsedTime;
    }

    #endregion

    #region 채굴 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 채굴 상태 변경 요청
    /// </summary>
    [MessagePackObject]
    public struct UpdateMiningStateFromClient : IPacket
    {
        [Key(0)] public bool IsMining; // true: 채굴 시작, false: 채굴 중단
    }

    /// <summary>
    /// [서버 => 브로드캐스트]
    /// 플레이어의 채굴 상태 변경 알림
    /// </summary>
    [MessagePackObject]
    public struct MiningStateBroadcast : IPacket
    {
        [Key(0)] public int PlayerId;
        [Key(1)] public bool IsMining; // 채굴 중인지 여부
    }

    #endregion

    #region 채팅 시스템 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 채팅 메시지를 보내는 패킷
    /// </summary>
    [MessagePackObject]
    public struct ChatFromClient : IPacket
    {
        [Key(0)] public string Message;
    }

    /// <summary>
    /// [서버 => 클라이언트]
    /// 채팅 메시지 브로드캐스트
    /// </summary>
    [MessagePackObject]
    public struct ChatBroadcast : IPacket
    {
        [Key(0)] public int PlayerId; // 시스템 메시지일 경우 -1
        [Key(1)] public string Message;
    }

    #endregion

    #region 방 관리 패킷들

    /// <summary>
    /// [클라이언트 => 서버]
    /// 방 제목 변경 요청 (방장만 가능)
    /// </summary>
    [MessagePackObject]
    public struct UpdateRoomNameFromClient : IPacket
    {
        [Key(0)] public string RoomName;
    }

    /// <summary>
    /// [서버 => 요청한 클라이언트]
    /// 방 제목 변경 응답 (성공/실패)
    /// </summary>
    [MessagePackObject]
    public struct UpdateRoomNameResponse : IPacket
    {
        [Key(0)] public bool ok;             // 성공 여부
        [Key(1)] public string errorMessage; // 에러 메시지 (실패 시)
    }

    /// <summary>
    /// [서버 => 모든 클라이언트]
    /// 방 제목 변경 브로드캐스트 (성공 시에만 전송)
    /// </summary>
    [MessagePackObject]
    public struct UpdateRoomNameBroadcast : IPacket
    {
        [Key(0)] public string RoomName;
    }

    #endregion
}