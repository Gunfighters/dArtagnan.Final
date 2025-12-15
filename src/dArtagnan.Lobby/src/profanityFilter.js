/**
 * 금지어 필터링 시스템
 */

// 욕설 금지어 목록 (한글 + 영어)
const PROFANITY_WORDS = [
    // 한글 일반 욕설
    '시발', '씨발', '시벌', 'ㅅㅂ', 'ㅆㅂ', '씨바', '시바',
    '개새끼', '개색', '개새', 'ㄱㅅㄲ', '개세끼', '개쉐끼',
    '병신', '븅신', 'ㅂㅅ', '병쉰',
    '좆', '존나', 'ㅈㄴ', '졸라',
    '지랄', 'ㅈㄹ', '지럴',
    '닥쳐', '닥치',
    '죽어', '뒤져', '디져',
    '엿먹', '엿이나',
    '개같', '개가튼',
    '꺼져', '꺼지',
    '새끼', 'ㅅㄲ', '쉐끼',
    '쌍놈', '쌍년',
    '육시', '육시럴',
    '개년', '걸레',
    '쳐먹', '처먹', '쳐쳐',
    '개소리', '헛소리',
    '염병', '옘병',
    '호로', '호로새끼',

    // 한글 성적 욕설
    '보지', 'ㅂㅈ', '보빨', '보짓',
    '자지', '좆', '좃',
    '불알', '부랄',
    '후장', '항문',
    '섹스', '섹', '섹쓰', '쎅스', '쎅쓰',
    '야동', '야사', '음란',
    '변태', '변태새끼',
    '창녀', '매춘', '성매매',
    '강간', '성폭행', '성폭',
    '따먹', '따묵', '박아', '쑤셔',
    '씹', '씹년', '씹새', '씹할',
    '유두', '음부',
    '고자', '불구',
    '쩌리', '쩔어',
    '꼴려', '꼴리',
    '딸딸', '딸치', '자위',

    // 영어 일반 욕설
    'fuck', 'fuk', 'fck', 'fuckin', 'fucking', 'fucker', 'fcker',
    'shit', 'sht', 'shyt', 'shiet',
    'bitch', 'btch', 'biatch', 'beetch',
    'ass', 'asshole', 'asswhole', 'arse',
    'damn', 'dammit', 'damm',
    'bastard', 'bstrd',
    'cunt', 'kunt',
    'dick', 'dck', 'dik',
    'retard', 'retarded', 'tard',
    'idiot', 'idiotic',
    'stupid', 'stupd',
    'dumb', 'dumbass',
    'nigga', 'nigger', 'nigr',
    'fag', 'faggot', 'fagot',
    'piss', 'pissed',
    'hell', 'wtf',

    // 영어 성적 욕설
    'pussy', 'pusy', 'pusssy',
    'cock', 'cck', 'cok',
    'penis', 'pnis', 'dick',
    'vagina', 'vgina', 'vagin',
    'sex', 'sexy', 'sexual',
    'porn', 'porno', 'pornography',
    'whore', 'hore', 'slut', 'slt',
    'rape', 'raping', 'raped',
    'dildo', 'dildo',
    'cum', 'cumming', 'jizz',
    'orgasm', 'masturbate',
    'boobs', 'tits', 'titties',
    'anal', 'anus',
    'horny', 'erotic'
];

// 운영자 사칭 금지어 (닉네임 전용)
const ADMIN_IMPERSONATION_WORDS = [
    // 한글
    '운영자', '운영진', '관리자', '매니저', '어드민',

    // 영어
    'admin', 'administrator',
    'gm'
];

// 화이트리스트 (욕설이 포함되어 있지만 정상적인 단어)
const WHITELIST_WORDS = [
    // 영어 (ass 포함)
    'classic', 'classical', 'classics',
    'class', 'classes', 'classroom',
    'assemble', 'assembly', 'assassin',
    'pass', 'passage', 'passenger', 'password',
    'mass', 'massage', 'massive',
    'bass', 'bassist',
    'grass', 'grassland',
    'glass', 'glasses',
    'harass', 'harassment',

    // 영어 (sex 포함)
    'sexually', 'asexual', 'bisexual', 'homosexual', 'heterosexual',
    'intersex', 'unisex', 'sexist', 'sexism'
];

/**
 * 문자열 정규화 (특수문자 제거, 소문자 변환)
 */
function normalizeString(str) {
    return str
        .toLowerCase()
        .replace(/[^a-z0-9가-힣ㄱ-ㅎㅏ-ㅣ]/g, ''); // 특수문자, 공백 제거
}

/**
 * 욕설 검사
 * @param {string} text - 검사할 텍스트
 * @returns {{ isValid: boolean, reason: string }} - 검사 결과
 */
export function checkProfanity(text) {
    const normalized = normalizeString(text);

    // 화이트리스트 체크 (허용 단어면 통과)
    for (const whiteWord of WHITELIST_WORDS) {
        const normalizedWhite = normalizeString(whiteWord);
        if (normalized === normalizedWhite) {
            return { isValid: true, reason: '' };
        }
    }

    // 욕설 체크
    for (const word of PROFANITY_WORDS) {
        const normalizedWord = normalizeString(word);
        if (normalized.includes(normalizedWord)) {
            return {
                isValid: false,
                reason: '부적절한 언어가 포함되어 있습니다.'
            };
        }
    }

    return { isValid: true, reason: '' };
}

/**
 * 운영자 사칭 검사
 * @param {string} text - 검사할 텍스트
 * @returns {{ isValid: boolean, reason: string }} - 검사 결과
 */
export function checkAdminImpersonation(text) {
    const normalized = normalizeString(text);

    for (const word of ADMIN_IMPERSONATION_WORDS) {
        const normalizedWord = normalizeString(word);
        if (normalized.includes(normalizedWord)) {
            return {
                isValid: false,
                reason: '운영자 사칭 단어는 사용할 수 없습니다.'
            };
        }
    }

    return { isValid: true, reason: '' };
}

/**
 * 닉네임 전체 검증 (형식 + 욕설 + 운영자 사칭)
 * @param {string} nickname - 검증할 닉네임
 * @returns {{ isValid: boolean, errorType: string, reason: string }} - 검증 결과
 */
export function validateNickname(nickname) {
    // 1. null/undefined/빈 문자열 체크
    if (!nickname) {
        return {
            isValid: false,
            errorType: 'NICKNAME_FORMAT_INVALID',
            reason: '닉네임을 입력해주세요.'
        };
    }

    const cleanNickname = nickname.trim();

    // 2. 길이 체크 (2-8자)
    if (cleanNickname.length < 2 || cleanNickname.length > 6) {
        return {
            isValid: false,
            errorType: 'NICKNAME_FORMAT_INVALID',
            reason: '닉네임은 2-6자여야 합니다.'
        };
    }

    // 3. 허용된 문자만 사용 (한글, 영어, 숫자)
    const validCharPattern = /^[a-zA-Z0-9가-힣]+$/;
    if (!validCharPattern.test(cleanNickname)) {
        return {
            isValid: false,
            errorType: 'NICKNAME_FORMAT_INVALID',
            reason: '닉네임은 한글, 영어, 숫자만 사용할 수 있습니다.'
        };
    }

    // 4. 욕설 검사
    const profanityCheck = checkProfanity(cleanNickname);
    if (!profanityCheck.isValid) {
        return {
            isValid: false,
            errorType: 'NICKNAME_PROFANITY',
            reason: profanityCheck.reason
        };
    }

    // 5. 운영자 사칭 검사
    const adminCheck = checkAdminImpersonation(cleanNickname);
    if (!adminCheck.isValid) {
        return {
            isValid: false,
            errorType: 'NICKNAME_PROFANITY',
            reason: adminCheck.reason
        };
    }

    return { isValid: true, errorType: '', reason: '' };
}

/**
 * 방 제목 전체 검증 (형식 + 욕설)
 * @param {string} roomName - 검증할 방 제목
 * @returns {{ isValid: boolean, errorType: string, reason: string }} - 검증 결과
 */
export function validateRoomName(roomName) {
    // 1. null/undefined/빈 문자열 체크
    if (!roomName) {
        return {
            isValid: false,
            errorType: 'ROOM_NAME_FORMAT_INVALID',
            reason: '방 제목을 입력해주세요.'
        };
    }

    const cleanRoomName = roomName.trim();

    // 2. 길이 체크 (1-15자)
    if (cleanRoomName.length < 1 || cleanRoomName.length > 15) {
        return {
            isValid: false,
            errorType: 'ROOM_NAME_FORMAT_INVALID',
            reason: '방 제목은 1-15자여야 합니다.'
        };
    }

    // 3. 욕설 검사
    const profanityCheck = checkProfanity(cleanRoomName);
    if (!profanityCheck.isValid) {
        return {
            isValid: false,
            errorType: 'ROOM_NAME_PROFANITY',
            reason: profanityCheck.reason
        };
    }

    return { isValid: true, errorType: '', reason: '' };
}
