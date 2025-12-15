using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace dArtagnan.Shared
{
    public enum GameState
    {
        Waiting, // 대기 중 (Ready 단계 포함)
        InitialRoulette, // 초기 룰렛 (정확도 배정)
        Round, // 라운드 진행 중
        Shop, // 상점 (아이템 구매 및 샵룰렛)
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 방향.
    /// </summary>
    public static class DirectionHelper
    {
        public static readonly List<Vector2> Directions = new()
        {
            Vector2.Zero,
            Vector2.UnitY,
            Vector2.Normalize(Vector2.UnitY + Vector2.UnitX),
            Vector2.UnitX,
            Vector2.Normalize(Vector2.UnitX - Vector2.UnitY),
            -Vector2.UnitY,
            Vector2.Normalize(-Vector2.UnitY - Vector2.UnitX),
            -Vector2.UnitX,
            Vector2.Normalize(-Vector2.UnitX + Vector2.UnitY),
        };

        public static Vector2 IntToDirection(this int direction)
        {
            return Directions[direction];
        }
    }

    public enum MapTileType
    {
        Collision,
        SpawnPointOnRoomEntry,
        SpawnPointOnRoundStart
    }

    /// <summary>
    /// 클라이언트와 서버가 공유하는 상수. (공유 안할수도 있음 낄낄)
    /// </summary>
    public static class Constants
    {
        // ===== 게임 기본 설정 =====
        public const int MAX_PLAYER_COUNT = 8;
        public const int MAX_ROUNDS = 50;
        public const int CONNECTION_TIMEOUT_SECONDS = 30; // 서버가 클라이언트 끊김을 감지하는 시간

        // ===== 정확도 시스템 =====
        public const int ACCURACY_STATE_RATE = 2; // 정확도 상태 변경 비율
        public const float ACCURACY_UPDATE_INTERVAL = 1.0f; // 정확도 업데이트 주기

        // ===== 이동 시스템 =====
        public const float MOVEMENT_SPEED = 1.75f; // 기본 이동속도
        public const float MIN_SPEED = 0.5f; // 최소 이동속도
        public const float MAX_SPEED = 5.0f; // 최대 이동속도
        public const int BALANCE_PER_SPEED_PENALTY = 10; // N원당 속도 감소
        public const float SPEED_PENALTY_PER_BALANCE = 0f; // 위 금액당 감소되는 속도 //일단 비활성화(기획적으로 애매)
        public const float FURY_SPEED_MULTIPLIER = 1.8f; // 폭주 모드 속도 배율

        // ===== 전투 시스템 =====
        public const float DEFAULT_RANGE = 4f; // 기본 사거리
        public const float BASE_RELOAD_TIME = 2f; // 기본 재장전시간

        // ===== 경제 시스템 =====
        public const int INITIAL_BALANCE = 20; // 시작 소지금
        public const float BETTING_PERIOD = 10.0f; // 징수 주기
        public const int DEDUCTION_MULTIPLY_PERIOD = 3; // N회 징수마다 차감액 2배 증가
        public const int MAX_REWARD_TO_LOBBY = 500; // 로비 서버로 보내는 최대 보상

        // ===== 이니셜 룰렛 =====
        public const int INITIAL_ROULETTE_MIN_ACCURACY = 20; // 최소 정확도
        public const int INITIAL_ROULETTE_MAX_ACCURACY = 85; // 최대 정확도
        public const float INITIAL_ROULETTE_DURATION = 7.5f; // 강제 종료 시간

        // ===== 상점 시스템 =====
        public const float SHOP_DURATION = 20.0f; // 상점 지속시간
        public const int SHOP_ITEM_COUNT = 3; // 상점에서 제공하는 아이템 개수
        public const int SHOP_ROULETTE_PRICE = 3; // 샵룰렛 가격

        // ===== 채굴 시스템 =====
        public const float MINING_DURATION = 4.0f; // 채굴 지속시간
        public const int MINING_MIN_REWARD = 0; // 채굴 최소 보상
        public const int MINING_MAX_REWARD = 30; // 채굴 최대 보상

        // ===== 보상 시스템 =====
        public static readonly int[] RANK_REWARDS = { 0, 50, 50, 30, 20, 10, 0, 0, 0 }; // 순위별 보상 (1~8등, 인덱스 0 미사용)
        public static readonly int[] BETTING_AMOUNTS = GenerateBettingAmounts(); // 라운드별 베팅금

        private static int[] GenerateBettingAmounts()
        {
            var amounts = new int[MAX_ROUNDS];
            for (int i = 0; i < MAX_ROUNDS; i++)
            {
                amounts[i] = (int)Math.Pow(2, i+1);
            }
            return amounts;
        }
    }
}   