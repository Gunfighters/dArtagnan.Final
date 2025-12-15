#pragma warning disable CS8618

using System;
using System.Collections.Generic;

namespace dArtagnan.Shared
{
    /// <summary>
    /// 로비 서버와의 통신 프로토콜
    /// </summary>

    #region  Http 메시지
    // HTTP 메시지
    [Serializable] public class LoginRequest { public string providerId; public string clientVersion; }
    [Serializable] public class LoginResponse { public string sessionId; public string nickname; }
    [Serializable] public class ErrorResponse { public string message; }
    #endregion


    #region WebSocket 메시지
    /// <summary>
    /// 로비 서버 프로토콜 설계 철학:
    /// - 모든 클라이언트 요청은 정확히 하나의 Response를 받는다
    /// - Response.ok = true: 성공, Response.ok = false: 실패
    /// - 실패 시 Response 안에 errorType, message 포함
    /// - ErrorMessage는 요청과 무관한 시스템 에러만 사용 (세션 만료, 알 수 없는 메시지 등)
    /// </summary>

    // WebSocket 메시지
    [Serializable] public class AuthMessage { public string type = "auth"; public string sessionId; }

    // 상점 상수 데이터
    [Serializable] public class ShopConstants {
        public int ROULETTE_GOLD_COST;                      // 룰렛 금화 비용 (1회)
        public int ROULETTE_CRYSTAL_COST;                   // 룰렛 크리스탈 비용 (1회)
        public int ROULETTE_5X_GOLD_COST;                   // 5연뽑 금화 비용
        public int ROULETTE_5X_CRYSTAL_COST;                // 5연뽑 크리스탈 비용
        public int ROULETTE_POOL_SIZE;                      // 룰렛 풀 크기
        public Dictionary<string, string[]> TIERS;          // 티어별 코스튬 목록
        public Dictionary<string, int> DIRECT_SILVER_PRICES;    // 티어별 실버 가격
        public Dictionary<string, int> DIRECT_CRYSTAL_PRICES;   // 티어별 크리스탈 가격
    }

    [Serializable] public class AuthSuccessMessage { public string type = "auth_success"; public bool ok; public string nickname; public string providerId; public int gold; public int silver; public int crystal; public Dictionary<string, string> equippedCostumes; public Dictionary<string, string[]> ownedCostumes; public bool isNewUser; public int level; public int currentExp; public int expToNextLevel; public string gameSessionToken; public Dictionary<string, ShopConstants> shopConstants; }
    [Serializable] public class CreateRoomMessage { public string type = "create_room"; public string roomName; public bool hasPassword; }
    [Serializable] public class CreateRoomResponseMessage { public string type = "create_room_response"; public bool ok; public string roomId; public string roomName; public string password; public string ip; public int port; public string errorType; public string message; }
    [Serializable] public class JoinRoomMessage { public string type = "join_room"; public string roomId; public string password; }
    [Serializable] public class JoinRoomResponseMessage { public string type = "join_room_response"; public bool ok; public string roomId; public string roomName; public string password; public string ip; public int port; public string errorType; public string message; }
    [Serializable] public class MessageType { public string type; }
    [Serializable] public class NicknameSubmission { public string type = "set_nickname"; public string nickname; }
    [Serializable] public class NicknameSetResponse { public string type = "nickname_set"; public bool ok; public string nickname; public string errorType; public string message; }

    // 방 목록 관련 메시지
    [Serializable] public class RoomInfo { public string roomId; public string roomName; public int playerCount; public int maxPlayers; public bool joinable; public bool hasPassword; public string ip; public int port; } //직접 보내는 패킷은 아님
    [Serializable] public class RoomsUpdateMessage { public string type = "rooms_update"; public RoomInfo[] rooms; }

    // 개별 업데이트 메시지들
    [Serializable] public class UpdateNicknameMessage { public string type = "update_nickname"; public string nickname; }
    [Serializable] public class UpdateGoldMessage { public string type = "update_gold"; public int gold; }
    [Serializable] public class UpdateSilverMessage { public string type = "update_silver"; public int silver; }
    [Serializable] public class UpdateCrystalMessage { public string type = "update_crystal"; public int crystal; }
    [Serializable] public class UpdateEquippedCostumesMessage { public string type = "update_equipped_costumes"; public Dictionary<string, string> equippedCostumes; }
    [Serializable] public class UpdateInventoryMessage { public string type = "update_inventory"; public Dictionary<string, string[]> ownedCostumes; }
    [Serializable] public class UpdateExperienceMessage { public string type = "update_experience"; public int level; public int currentExp; public int expToNextLevel; }

    // 알림 메시지
    [Serializable] public class NotificationMessage { public string type = "notification"; public string title; public string body; }

    // 상점 관련 메시지들
    [Serializable] public class ShopBuyPartRouletteMessage { public string type = "shop_buy_part_roulette"; public string partType; public string currency; }  // currency: "gold" | "crystal"
    [Serializable] public class ShopBuyPartRouletteResponse { public string type = "shop_buy_part_roulette_response"; public bool ok; public string partType; public string[] roulettePool; public string wonCostume; public bool isDuplicate; public int silverGained; public string errorType; public string message; }
    [Serializable] public class ShopBuyPartRoulette5xMessage { public string type = "shop_buy_part_roulette_5x"; public string partType; public string currency; }  // currency: "gold" | "crystal"
    [Serializable] public class ShopBuyPartRoulette5xResponse { public string type = "shop_buy_part_roulette_5x_response"; public bool ok; public string partType; public string[] roulettePool; public string[] wonCostumes; public bool[] isDuplicates; public int totalSilverGained; public string errorType; public string message; }
    [Serializable] public class ShopBuyPartDirectMessage { public string type = "shop_buy_part_direct"; public string partType; public string costumeId; public string currency; }  // currency: "silver" | "crystal"
    [Serializable] public class ShopBuyPartDirectResponse { public string type = "shop_buy_part_direct_response"; public bool ok; public string partType; public string costumeId; public string errorType; public string message; }

    [Serializable] public class GetPartRatesMessage { public string type = "get_part_rates"; public string partType; }
    [Serializable] public class PartRatesResponse { public string type = "part_rates_response"; public bool ok; public string partType; public CostumeRate[] rates; public string errorType; public string message; }
    [Serializable] public class ChangeCostumeMessage { public string type = "change_costume"; public string partType; public string costumeId; }
    [Serializable] public class ChangeCostumeResponse { public string type = "change_costume_response"; public bool ok; public string partType; public string costumeId; public string errorType; public string message; }

    // 코스튬 확률 정보
    [Serializable] public class CostumeRate { public string costumeId; public float rate; }

    // 인앱 결제 관련
    [Serializable] public class IAPPurchaseMessage { public string type = "iap_purchase"; public string productId; public string payload; }
    [Serializable] public class IAPPurchaseResponse { public string type = "iap_purchase_response"; public bool ok; public string productId; public int crystalAmount; public string errorType; public string message; }


    /// <summary>
    /// 웹소켓 에러 처리
    /// ErrorMessage는 요청과 무관한 시스템 레벨 에러만 사용
    /// 각 요청의 실패는 해당 Response의 ok=false + errorType으로 처리
    /// </summary>

    // 요청과 무관한 시스템 에러 전용 패킷
    [Serializable] public class ErrorMessage { public string type = "error"; public string errorType; public string message; }

    // 에러 타입 (시스템 에러 + 각 요청별 에러)
    public static class ErrorType
    {
        // 시스템 에러 (요청과 무관, ErrorMessage 전용)
        public const string SESSION_INVALID = "SESSION_INVALID";           // 세션 만료/무효 (치명적)
        public const string AUTH_DUPLICATE_LOGIN = "AUTH_DUPLICATE_LOGIN"; // 중복 로그인
        public const string UNKNOWN_MESSAGE_TYPE = "UNKNOWN_MESSAGE_TYPE"; // 알 수 없는 메시지 타입
        public const string INVALID_REQUEST = "INVALID_REQUEST";           // JSON 파싱 실패 등

        // 닉네임 관련 (Response.errorType)
        public const string NICKNAME_FORMAT_INVALID = "NICKNAME_FORMAT_INVALID"; // 닉네임 형식 오류
        public const string NICKNAME_DUPLICATE = "NICKNAME_DUPLICATE";           // 닉네임 중복
        public const string NICKNAME_PROFANITY = "NICKNAME_PROFANITY";           // 닉네임 금지어 포함
        public const string NICKNAME_SET_FAILED = "NICKNAME_SET_FAILED";         // 닉네임 설정 실패

        // 방 관련 (Response.errorType)
        public const string ROOM_NOT_FOUND = "ROOM_NOT_FOUND";           // 방을 찾을 수 없음
        public const string ROOM_INVALID_STATE = "ROOM_INVALID_STATE";   // 참가 불가능한 방 상태
        public const string ROOM_CREATE_FAILED = "ROOM_CREATE_FAILED";   // 방 생성 실패
        public const string ROOM_JOIN_FAILED = "ROOM_JOIN_FAILED";       // 방 참가 실패
        public const string ROOM_NAME_FORMAT_INVALID = "ROOM_NAME_FORMAT_INVALID"; // 방 제목 형식 오류
        public const string ROOM_NAME_PROFANITY = "ROOM_NAME_PROFANITY";           // 방 제목 금지어 포함
        public const string ROOM_WRONG_PASSWORD = "ROOM_WRONG_PASSWORD";           // 비밀번호 불일치

        // 상점 관련 (Response.errorType)
        public const string SHOP_INVALID_PART_TYPE = "SHOP_INVALID_PART_TYPE";     // 잘못된 파츠 타입
        public const string SHOP_INSUFFICIENT_GOLD = "SHOP_INSUFFICIENT_GOLD";     // 금화 부족
        public const string SHOP_INSUFFICIENT_SILVER = "SHOP_INSUFFICIENT_SILVER"; // 실버 부족
        public const string SHOP_INSUFFICIENT_CRYSTAL = "SHOP_INSUFFICIENT_CRYSTAL"; // 크리스탈 부족
        public const string SHOP_NO_COSTUMES = "SHOP_NO_COSTUMES";                 // 뽑기 가능한 코스튬 없음
        public const string SHOP_ROULETTE_FAILED = "SHOP_ROULETTE_FAILED";         // 룰렛 뽑기 실패
        public const string SHOP_INVALID_COSTUME = "SHOP_INVALID_COSTUME";         // 구매할 수 없는 코스튬
        public const string SHOP_ALREADY_OWNED = "SHOP_ALREADY_OWNED";             // 이미 보유한 코스튬
        public const string SHOP_DIRECT_FAILED = "SHOP_DIRECT_FAILED";             // 직접 구매 실패

        // 코스튬 관련 (Response.errorType)
        public const string COSTUME_INVALID_PART_TYPE = "COSTUME_INVALID_PART_TYPE"; // 잘못된 파츠 타입
        public const string COSTUME_NOT_OWNED = "COSTUME_NOT_OWNED";                 // 보유하지 않은 코스튬
        public const string COSTUME_CHANGE_FAILED = "COSTUME_CHANGE_FAILED";         // 코스튬 변경 실패

        // 인앱 결제 관련 (Response.errorType)
        public const string IAP_INVALID_PRODUCT = "IAP_INVALID_PRODUCT";             // 유효하지 않은 상품
        public const string IAP_DUPLICATE_TRANSACTION = "IAP_DUPLICATE_TRANSACTION"; // 중복 처리된 거래
    }
    #endregion
}

#pragma warning restore CS8618
