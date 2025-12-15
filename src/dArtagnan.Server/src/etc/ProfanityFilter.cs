using System.Linq;
using System.Text.RegularExpressions;

namespace dArtagnan.Server;

/// <summary>
/// 금지어 필터링 시스템
/// </summary>
public static class ProfanityFilter
{
    /// <summary>
    /// 욕설 금지어 목록 (한글 + 영어)
    /// </summary>
    private static readonly string[] ProfanityWords = {
        // 한글 일반 욕설
        "시발", "씨발", "시벌", "ㅅㅂ", "ㅆㅂ", "씨바", "시바",
        "개새끼", "개색", "개새", "ㄱㅅㄲ", "개세끼", "개쉐끼",
        "병신", "븅신", "ㅂㅅ", "병쉰",
        "좆", "존나", "ㅈㄴ", "졸라",
        "지랄", "ㅈㄹ", "지럴",
        "닥쳐", "닥치",
        "죽어", "뒤져", "디져",
        "엿먹", "엿이나",
        "개같", "개가튼",
        "꺼져", "꺼지",
        "새끼", "ㅅㄲ", "쉐끼",
        "쌍놈", "쌍년",
        "육시", "육시럴",
        "개년", "걸레",
        "쳐먹", "처먹", "쳐쳐",
        "개소리", "헛소리",
        "염병", "옘병",
        "호로", "호로새끼",

        // 한글 성적 욕설
        "보지", "ㅂㅊ", "ㅂㅈ", "보빨", "보짓",
        "자지", "좆", "좃",
        "불알", "부랄",
        "후장", "항문",
        "섹스", "섹", "ㅅㅅ", "섹쓰", "쎅스", "쎅쓰",
        "야동", "야사", "음란", "야한",
        "변태", "변태새끼",
        "창녀", "매춘", "성매매",
        "강간", "성폭행", "성폭",
        "따먹", "따묵", "박아", "쑤셔",
        "씹", "씹년", "씹새", "씹할",
        "빨다", "빨아", "빨어",
        "유두", "음부",
        "고자", "불구",
        "쩌리", "쩔어",
        "꼴려", "꼴리",
        "딸딸", "딸치", "자위",

        // 영어 일반 욕설
        "fuck", "fuk", "fck", "fuckin", "fucking", "fucker", "fcker",
        "shit", "sht", "shyt", "shiet",
        "bitch", "btch", "biatch", "beetch",
        "ass", "asshole", "asswhole", "arse",
        "damn", "dammit", "damm",
        "bastard", "bstrd",
        "cunt", "kunt",
        "dick", "dck", "dik",
        "retard", "retarded", "tard",
        "idiot", "idiotic",
        "stupid", "stupd",
        "dumb", "dumbass",
        "nigga", "nigger", "nigr",
        "fag", "faggot", "fagot",
        "piss", "pissed",
        "hell", "wtf",

        // 영어 성적 욕설
        "pussy", "pusy", "pusssy",
        "cock", "cck", "cok",
        "penis", "pnis", "dick",
        "vagina", "vgina", "vagin",
        "sex", "sexy", "sexual",
        "porn", "porno", "pornography",
        "whore", "hore", "slut", "slt",
        "rape", "raping", "raped",
        "dildo", "dildo",
        "cum", "cumming", "jizz",
        "orgasm", "masturbate",
        "boobs", "tits", "titties",
        "anal", "anus",
        "horny", "erotic"
    };

    /// <summary>
    /// 화이트리스트 (욕설이 포함되어 있지만 정상적인 단어)
    /// </summary>
    private static readonly string[] WhitelistWords = {
        // 영어 (ass 포함)
        "classic", "classical", "classics",
        "class", "classes", "classroom",
        "assemble", "assembly", "assassin",
        "pass", "passage", "passenger", "password",
        "mass", "massage", "massive",
        "bass", "bassist",
        "grass", "grassland",
        "glass", "glasses",
        "harass", "harassment",

        // 영어 (sex 포함)
        "sexually", "asexual", "bisexual", "homosexual", "heterosexual",
        "intersex", "unisex", "sexist", "sexism"
    };

    /// <summary>
    /// 문자열 정규화 (특수문자 제거, 소문자 변환)
    /// </summary>
    private static string NormalizeString(string str)
    {
        // 특수문자, 공백 제거 (한글, 영어, 숫자만 남김)
        var normalized = Regex.Replace(str.ToLower(), @"[^a-z0-9가-힣ㄱ-ㅎㅏ-ㅣ]", "");
        return normalized;
    }

    /// <summary>
    /// 욕설 검사
    /// </summary>
    /// <param name="text">검사할 텍스트</param>
    /// <returns>검사 결과 (통과 여부, 실패 사유)</returns>
    public static (bool IsValid, string Reason) CheckProfanity(string text)
    {
        var normalized = NormalizeString(text);

        // 화이트리스트 체크 (허용 단어면 통과)
        foreach (var whiteWord in WhitelistWords)
        {
            var normalizedWhite = NormalizeString(whiteWord);
            if (normalized == normalizedWhite)
            {
                return (true, "");
            }
        }

        // 욕설 체크
        foreach (var word in ProfanityWords)
        {
            var normalizedWord = NormalizeString(word);
            if (normalized.Contains(normalizedWord))
            {
                return (false, "부적절한 언어가 포함되어 있습니다.");
            }
        }

        return (true, "");
    }

    /// <summary>
    /// 방 제목 검증 (형식 + 욕설)
    /// </summary>
    /// <param name="roomName">방 제목</param>
    /// <returns>검증 결과 (통과 여부, 실패 사유)</returns>
    public static (bool IsValid, string Reason) ValidateRoomName(string? roomName)
    {
        // 1. null/빈 문자열 체크
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return (false, "방 제목을 입력해주세요.");
        }

        var cleanRoomName = roomName.Trim();

        // 2. 길이 체크 (1-15자)
        if (cleanRoomName.Length < 1 || cleanRoomName.Length > 15)
        {
            return (false, "방 제목은 1-15자여야 합니다.");
        }

        // 3. 욕설 검사
        return CheckProfanity(cleanRoomName);
    }

    /// <summary>
    /// 채팅 메시지 검증 (형식 + 욕설)
    /// </summary>
    /// <param name="message">채팅 메시지</param>
    /// <returns>검증 결과 (통과 여부, 실패 사유)</returns>
    public static (bool IsValid, string Reason) ValidateChatMessage(string? message)
    {
        // 1. null/빈 문자열 체크
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, "메시지를 입력해주세요.");
        }

        var cleanMessage = message.Trim();

        // 2. 길이 체크 (1-100자)
        if (cleanMessage.Length < 1 || cleanMessage.Length > 100)
        {
            return (false, "메시지는 1-100자여야 합니다.");
        }

        // 3. 욕설 검사
        return CheckProfanity(cleanMessage);
    }
}
