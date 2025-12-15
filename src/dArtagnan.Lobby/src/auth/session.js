import { config } from '../config.js';
import { logger } from '../logger.js';
import { calculateLevelAndExp, getEquippedCostumes } from '../database.js';

// OAuth 임시 세션 (HTTP → WebSocket 연결용)
const oauthSessions = new Map(); // providerId -> { sessionId, user, createdAt }

// 활성 WebSocket 세션 (중복 로그인 방지용)
const activeSessions = new Map(); // providerId -> { ws, userId, nickname, gold, silver, equipped_costumes, currentRoomId, gameSessionToken, ... }

// 클라 발행용 게임 서버 접속용 토큰 (gameSessionToken -> providerId)
const gameSessionTokens = new Map(); // gameSessionToken -> providerId

/**
 * OAuth 세션 생성
 */
export function createOAuthSession(providerId, user) {
    const sessionId = generateSessionId();
    oauthSessions.set(providerId, {
        sessionId,
        user,
        createdAt: Date.now()
    });
    return sessionId;
}

/**
 * OAuth 세션 검증
 */
export function verifyOAuthSession(sessionId) {
    for (const [providerId, session] of oauthSessions) {
        if (session.sessionId === sessionId) {
            // 세션 만료 체크
            if (Date.now() - session.createdAt > config.session.oauthTimeout) {
                oauthSessions.delete(providerId);
                return null;
            }
            return { providerId, user: session.user };
        }
    }
    return null;
}

/**
 * OAuth 세션 삭제
 */
export function removeOAuthSession(providerId) {
    oauthSessions.delete(providerId);
}

/**
 * 활성 세션 조회
 */
export function getActiveSession(providerId) {
    return activeSessions.get(providerId);
}

/**
 * 모든 활성 세션 조회 (접속자 목록)
 */
export function getAllActiveSessions() {
    const sessions = [];
    for (const [providerId, session] of activeSessions) {
        sessions.push({
            providerId,
            nickname: session.nickname,
            gold: session.gold,
            silver: session.silver,
            crystal: session.crystal,
            equipped_costumes: session.equipped_costumes,
            currentRoomId: session.currentRoomId,
            provider: session.provider,
            loginAt: session.loginAt
        });
    }
    return sessions;
}

/**
 * 활성 세션 설정
 * @param {string} providerId - Provider ID (키)
 * @param {WebSocket} ws - WebSocket 연결
 * @param {Object} user - DB user 객체 (필드명 그대로)
 * @param {string|null} currentRoomId - 현재 방 ID
 */
export async function setActiveSession(providerId, ws, user, currentRoomId = null) {
    const expData = calculateLevelAndExp(user.experience || 0);

    // 게임 서버 접속용 토큰 생성
    const gameSessionToken = generateGameSessionToken();

    // 착용 코스튬 조회
    const equippedCostumes = await getEquippedCostumes(user.id);

    activeSessions.set(providerId, {
        ws,
        userId: user.id,
        nickname: user.nickname,
        gold: user.gold,
        silver: user.silver || 0,
        crystal: user.crystal || 0,
        equipped_costumes: equippedCostumes,
        provider_id: user.provider_id,                // DB 필드명 그대로
        provider: user.provider,
        needs_nickname: user.needs_nickname === 1,    // DB 필드명 그대로
        level: expData.level,
        currentExp: expData.currentExp,
        expToNextLevel: expData.expToNextLevel,
        currentRoomId,
        loginAt: new Date(),
        gameSessionToken                              // 게임 서버 접속용 토큰
    });

    // 토큰 -> providerId 역매핑
    gameSessionTokens.set(gameSessionToken, providerId);

    logger.info(`[USER-IN][${user.nickname}] 활성 세션 등록: 금화=${user.gold}, 실버=${user.silver || 0}, 크리스탈=${user.crystal || 0}, 레벨=${expData.level}`);
}

/**
 * 세션 금화 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionGold(providerId, newGold) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldGold = session.gold;
        session.gold = newGold;
        logger.info(`[Auth][${session.nickname}] 금화 업데이트: ${oldGold} → ${newGold}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_gold',
                gold: newGold
            }));
        }
    }
}

/**
 * 세션 실버 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionSilver(providerId, newSilver) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldSilver = session.silver;
        session.silver = newSilver;
        logger.info(`[Auth][${session.nickname}] 실버 업데이트: ${oldSilver} → ${newSilver}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_silver',
                silver: newSilver
            }));
        }
    }
}

/**
 * 세션 크리스탈 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionCrystal(providerId, newCrystal) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldCrystal = session.crystal;
        session.crystal = newCrystal;
        logger.info(`[Auth][${session.nickname}] 크리스탈 업데이트: ${oldCrystal} → ${newCrystal}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_crystal',
                crystal: newCrystal
            }));
        }
    }
}

/**
 * 세션 닉네임 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionNickname(providerId, newNickname) {
    const session = activeSessions.get(providerId);
    if (session) {
        const oldNickname = session.nickname;
        session.nickname = newNickname;
        session.needs_nickname = false;  // DB 필드명 그대로
        logger.info(`[Auth][${newNickname}] 닉네임 업데이트: "${oldNickname}" → "${newNickname}"`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_nickname',
                nickname: newNickname
            }));
        }
    }
}

/**
 * 세션 착용 코스튬 업데이트 (파츠별) + 클라이언트에 자동 전송
 */
export function updateSessionEquippedCostume(providerId, partType, costumeId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.equipped_costumes[partType] = costumeId;
        logger.info(`[Auth][${session.nickname}] 착용 코스튬 업데이트: ${partType} = ${costumeId}`);

        // 클라이언트에 자동 업데이트 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_equipped_costumes',
                equippedCostumes: session.equipped_costumes
            }));
        }
    }
}

/**
 * 세션 사용자 ID 업데이트 (신규 사용자 생성 시)
 * - 클라이언트에 전송할 필요 없음 (내부 데이터)
 */
export function updateSessionUserId(providerId, userId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.userId = userId;
        logger.info(`[Auth][${session.nickname}] 사용자 ID 업데이트: userId=${userId}`);
    }
}

/**
 * 세션 경험치 업데이트 + 클라이언트에 자동 전송
 */
export function updateSessionExperience(providerId, newTotalExp) {
    const session = activeSessions.get(providerId);
    if (session) {
        const expData = calculateLevelAndExp(newTotalExp);
        const oldLevel = session.level;

        session.level = expData.level;
        session.currentExp = expData.currentExp;
        session.expToNextLevel = expData.expToNextLevel;

        logger.info(`[Auth][${session.nickname}] 경험치 업데이트: 레벨 ${oldLevel} → ${expData.level}, 경험치 ${expData.currentExp}/${expData.expToNextLevel}`);

        // 클라이언트에 자동 전송
        if (session.ws && session.ws.readyState === session.ws.OPEN) {
            session.ws.send(JSON.stringify({
                type: 'update_experience',
                level: expData.level,
                currentExp: expData.currentExp,
                expToNextLevel: expData.expToNextLevel
            }));
        }
    }
}

/**
 * 세션에서 user 객체 형태로 조회 (호환성용)
 * DB 필드명 형태로 반환
 */
export function getSessionUser(providerId) {
    const session = activeSessions.get(providerId);
    if (!session) return null;

    return {
        id: session.userId,
        nickname: session.nickname,
        gold: session.gold,
        silver: session.silver,
        crystal: session.crystal,
        equipped_costumes: session.equipped_costumes,
        provider_id: session.provider_id,
        provider: session.provider,
        needs_nickname: session.needs_nickname ? 1 : 0
    };
}

/**
 * 사용자의 현재 방 설정
 */
export function setUserCurrentRoom(providerId, roomId) {
    const session = activeSessions.get(providerId);
    if (session) {
        session.currentRoomId = roomId;
    }
}

/**
 * 사용자의 현재 방 조회
 */
export function getUserCurrentRoom(providerId) {
    const session = activeSessions.get(providerId);
    return session?.currentRoomId || null;
}

/**
 * 활성 세션 제거
 */
export function removeActiveSession(providerId) {
    const session = activeSessions.get(providerId);
    if (session) {
        logger.info(`[USER-OUT][${session.nickname}] 활성 세션 제거`);

        // 게임 세션 토큰도 함께 삭제
        if (session.gameSessionToken) {
            gameSessionTokens.delete(session.gameSessionToken);
        }
    }
    activeSessions.delete(providerId);
}

/**
 * 모든 클라이언트에게 메시지 브로드캐스트
 */
export function broadcastToAll(message) {
    const messageStr = JSON.stringify(message);
    let sentCount = 0;

    for (const [providerId, { ws }] of activeSessions) {
        if (ws.readyState === ws.OPEN) {
            ws.send(messageStr);
            sentCount++;
        }
    }

    logger.info(`[WS][All] 브로드캐스트: ${message.type} (${sentCount}명)`);
}

/**
 * 세션 ID 생성
 */
function generateSessionId() {
    return Math.random().toString(36).slice(2) + Date.now().toString(36);
}

/**
 * 게임 세션 토큰 생성
 */
function generateGameSessionToken() {
    return 'game_' + Math.random().toString(36).slice(2) + Date.now().toString(36);
}

/**
 * 게임 세션 토큰으로 사용자 정보 조회
 */
export function getSessionByGameToken(gameSessionToken) {
    const providerId = gameSessionTokens.get(gameSessionToken);
    if (!providerId) return null;

    const session = activeSessions.get(providerId);
    if (!session) {
        // 토큰은 있는데 세션이 없으면 정리
        gameSessionTokens.delete(gameSessionToken);
        return null;
    }

    return {
        providerId: session.provider_id,
        nickname: session.nickname,
        equippedCostumes: session.equipped_costumes,
        userId: session.userId
    };
}

/**
 * 주기적 세션 정리
 */
function cleanupSessions() {
    const now = Date.now();
    let oauthCleaned = 0;

    // OAuth 세션 정리
    for (const [providerId, session] of oauthSessions) {
        if (now - session.createdAt > config.session.oauthTimeout) {
            oauthSessions.delete(providerId);
            oauthCleaned++;
        }
    }

    if (oauthCleaned > 0) {
        logger.info(`[Auth][System] 세션 정리: OAuth 세션 ${oauthCleaned}개 정리 완료`);
    }
}

// 세션 정기 정리 시작
setInterval(cleanupSessions, config.session.cleanupInterval);
logger.info(`[Auth][System] 자동 세션 정리 시작: OAuth TTL=${config.session.oauthTimeout / 60000}분`);
