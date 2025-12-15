using System;
using System.Collections.Generic;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 게임 서버 → 로비 서버 Internal API 프로토콜
    /// HTTP POST 요청 시 JSON으로 직렬화되어 전송됨
    /// </summary>

    // ===========================================
    // 게임 결과 관련 프로토콜
    // ===========================================

    /// <summary>
    /// 개별 플레이어의 게임 결과 정보
    /// </summary>
    [Serializable]
    public class PlayerGameResult
    {
        /// <summary>
        /// 플레이어 고유 ID (OAuth Provider ID)
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// 최종 순위 (1~8등)
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 순위별 상금 (1등: Balance+50, 2~5등: 고정 보상)
        /// </summary>
        public int RewardMoney { get; set; }
    }

    /// <summary>
    /// 게임 종료 시 전체 플레이어 결과를 한 번에 전송하는 요청
    /// POST /internal/game-results
    /// </summary>
    [Serializable]
    public class BulkGameResultRequest
    {
        /// <summary>
        /// 게임에 참여한 모든 플레이어의 결과 목록 (봇 제외)
        /// </summary>
        public List<PlayerGameResult> Players { get; set; } = new();
    }

    // ===========================================
    // 방 상태 관리 프로토콜
    // ===========================================

    /// <summary>
    /// 방 상태 업데이트 요청
    /// POST /internal/rooms/{roomId}/state
    /// </summary>
    [Serializable]
    public class RoomStateRequest
    {
        /// <summary>
        /// 새로운 방 상태 (0: INITIALIZING, 1: WAITING, 2: IN_GAME, 3: TERMINATED)
        /// </summary>
        public int State { get; set; }
    }

    /// <summary>
    /// 방 인원수 업데이트 요청
    /// POST /internal/rooms/{roomId}/player-count
    /// </summary>
    [Serializable]
    public class RoomPlayerCountRequest
    {
        /// <summary>
        /// 현재 방에 접속한 플레이어 수
        /// </summary>
        public int PlayerCount { get; set; }
    }

    /// <summary>
    /// 방 이름 업데이트 요청
    /// POST /internal/rooms/{roomId}/room-name
    /// </summary>
    [Serializable]
    public class RoomNameRequest
    {
        /// <summary>
        /// 새로운 방 이름
        /// </summary>
        public string RoomName { get; set; } = string.Empty;
    }

    // ===========================================
    // 플레이어 입/퇴장 프로토콜
    // ===========================================

    /// <summary>
    /// 플레이어 게임 입장 알림
    /// POST /internal/player-join
    /// </summary>
    [Serializable]
    public class PlayerJoinRequest
    {
        /// <summary>
        /// 플레이어 고유 ID (OAuth Provider ID)
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;

        /// <summary>
        /// 입장한 방 ID
        /// </summary>
        public string RoomId { get; set; } = string.Empty;
    }

    /// <summary>
    /// 플레이어 게임 퇴장 알림
    /// POST /internal/player-leave
    /// </summary>
    [Serializable]
    public class PlayerLeaveRequest
    {
        /// <summary>
        /// 플레이어 고유 ID (OAuth Provider ID)
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;
    }
}
