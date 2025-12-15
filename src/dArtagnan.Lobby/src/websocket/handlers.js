import { logger } from '../logger.js';
import { config } from '../config.js';
import { constants } from '../constants.js';
import fs from 'fs';
import path from 'path';
import {
    checkNicknameDuplicate,
    setUserNickname,
    createUser,
    updateUserGold,
    updateEquippedCostume,
    addCostumeToInventory,
    getUserInventory,
    updateLastLogout,
    calculateLevelAndExp,
    addUserSilver,
    updateUserSilver,
    updateUserCrystal,
    addUserCrystal
} from '../database.js';
import {
    verifyOAuthSession,
    removeOAuthSession,
    getActiveSession,
    setActiveSession,
    removeActiveSession,
    getUserCurrentRoom,
    updateSessionGold,
    updateSessionNickname,
    updateSessionEquippedCostume,
    updateSessionUserId,
    getSessionUser,
    updateSessionSilver,
    updateSessionCrystal
} from '../auth/session.js';
import {
    createRoom,
    getRoom,
    getAllRoomsForClient,
    pickRandomWaitingRoom,
    addPendingRequest,
    RoomState
} from '../rooms/manager.js';
import { validateNickname, validateRoomName } from '../profanityFilter.js';

/**
 * 랜덤 비밀번호 생성 (영어 대문자 + 숫자, 4자리)
 */
function generateRandomPassword() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let password = '';
    for (let i = 0; i < 4; i++) {
        password += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return password;
}

/**
 * 메시지 핸들러 맵
 */
export const handlers = {
    auth: handleAuth,
    set_nickname: handleSetNickname,
    create_room: handleCreateRoom,
    join_room: handleJoinRoom,
    shop_buy_part_roulette: handleShopBuyPartRoulette,
    shop_buy_part_roulette_5x: handleShopBuyPartRoulette5x,
    shop_buy_part_direct: handleShopBuyPartDirect,
    get_part_rates: handleGetPartRates,
    change_costume: handleChangeCostume,
    iap_purchase: handleIAPPurchase
};

/* =========================
   확률/샘플링 유틸 함수들
   ========================= */

/**
 * 티어 엔트리 정규화 및 필터링
 * - 빈 티어 제거(list.length > 0)
 * - 가중치가 양수인 티어만 사용(weight > 0)
 * - 가중치 합이 1이 아니어도 정규화
 * - 합이 0이면 균등 분배
 * 반환: [{ tier, weight(정규화됨), list }]
 */
function getNormalizedNonEmptyTiers(shop) {
    const entries = Object.entries(constants.TIER_WEIGHTS)
        .map(([tier, w]) => ({
            tier,
            weight: Number(w) || 0,
            list: Array.isArray(shop.TIERS[tier]) ? shop.TIERS[tier] : []
        }))
        .filter(x => x.list.length > 0 && x.weight > 0);

    if (entries.length === 0) return [];

    const sum = entries.reduce((a, x) => a + x.weight, 0);
    if (sum <= 0) {
        const equal = 1 / entries.length;
        return entries.map(x => ({ ...x, weight: equal }));
    }
    return entries.map(x => ({ ...x, weight: x.weight / sum }));
}

/**
 * 경계 보정 포함 티어 샘플
 * - r < 누적합 조건으로 못 잡히는 극소 경계값을 위해 기본값을 마지막으로 둠
 */
function sampleTier(normalizedEntries) {
    let r = Math.random();
    let cum = 0;
    let chosen = normalizedEntries[normalizedEntries.length - 1]; // 기본값: 마지막 티어
    for (const x of normalizedEntries) {
        cum += x.weight;
        if (r < cum) {
            chosen = x;
            break;
        }
    }
    return chosen;
}

/**
 * 한 번의 코스튬 추첨
 * - 빈 티어 제외/정규화 → 티어 샘플 → 해당 티어 내 균등 샘플
 */
function drawCostumeOnce(shop) {
    const normalized = getNormalizedNonEmptyTiers(shop);
    if (normalized.length === 0) {
        throw new Error('No available costumes across tiers (all tiers empty or non-positive weights).');
    }
    const chosenTier = sampleTier(normalized);
    const list = chosenTier.list;
    const idx = Math.floor(Math.random() * list.length);
    return { costumeId: list[idx], tier: chosenTier.tier };
}

/**
 * 코스튬의 티어 찾기
 */
function findCostumeTier(shop, costumeId) {
    for (const [tier, costumes] of Object.entries(shop.TIERS)) {
        if (costumes.includes(costumeId)) {
            return tier;
        }
    }
    return null;
}

/* =========================
   인증/기본 핸들러
   ========================= */

async function handleAuth(ws, data) {
    try {
        const { sessionId } = data;
        const sessionData = verifyOAuthSession(sessionId);

        if (!sessionData) {
            sendError(ws, 'SESSION_INVALID', '유효하지 않은 세션입니다. 다시 로그인해주세요.');
            ws.close();
            return;
        }

        const { providerId, user } = sessionData;

        // 기존 연결 강제 종료
        const existingSession = getActiveSession(providerId);
        if (existingSession) {
            logger.info(`[Auth][${existingSession.nickname || providerId}] 기존 세션 강제 종료: 중복 로그인`);
            sendError(existingSession.ws, 'AUTH_DUPLICATE_LOGIN', '다른 기기에서 로그인하여 연결이 종료됩니다.');
            existingSession.ws.close();
        }

        // 활성 세션 등록 (user 객체를 분해하여 저장)
        await setActiveSession(providerId, ws, user);

        // ws 객체에 직접 인증 정보 저장
        ws.authenticated = true;
        ws.providerId = providerId;

        // OAuth 세션 삭제
        removeOAuthSession(providerId);

        // 활성 세션에서 데이터 가져오기
        const session = getActiveSession(providerId);

        if (!session) {
            logger.error(`[Auth][${providerId}] 세션 생성 실패: setActiveSession 직후 세션을 찾을 수 없음`);
            sendError(ws, 'SESSION_INVALID', '세션 생성에 실패했습니다.');
            ws.close();
            return;
        }

        // 사용자 인벤토리 조회
        const ownedCostumes = await getUserInventory(session.userId);

        // 상점 상수 데이터 구성 (클라이언트용)
        const shopConstants = {};
        for (const [partType, shop] of Object.entries(constants.SHOPS)) {
            // 티어별 실버 직접 구매 가격 계산 (룰렛 금화 비용 × 배율)
            const directSilverPrices = {};
            for (const [tier, multiplier] of Object.entries(constants.TIER_DIRECT_SILVER_PRICE_MULTIPLIER)) {
                directSilverPrices[tier] = shop.ROULETTE_GOLD_COST * multiplier;
            }

            // 티어별 크리스탈 직접 구매 가격 계산 (룰렛 크리스탈 비용 × 배율)
            const directCrystalPrices = {};
            for (const [tier, multiplier] of Object.entries(constants.TIER_DIRECT_CRYSTAL_PRICE_MULTIPLIER)) {
                directCrystalPrices[tier] = shop.ROULETTE_CRYSTAL_COST * multiplier;
            }

            shopConstants[partType] = {
                ROULETTE_GOLD_COST: shop.ROULETTE_GOLD_COST,
                ROULETTE_CRYSTAL_COST: shop.ROULETTE_CRYSTAL_COST,
                ROULETTE_5X_GOLD_COST: shop.ROULETTE_GOLD_COST * 5,
                ROULETTE_5X_CRYSTAL_COST: shop.ROULETTE_CRYSTAL_COST * 5,
                ROULETTE_POOL_SIZE: shop.ROULETTE_POOL_SIZE,
                TIERS: shop.TIERS,
                DIRECT_SILVER_PRICES: directSilverPrices,
                DIRECT_CRYSTAL_PRICES: directCrystalPrices
            };
        }

        // 인증 성공 응답
        sendMessage(ws, 'auth_success', {
            ok: true,
            nickname: session.nickname,
            providerId: session.provider_id,
            gold: session.gold,
            silver: session.silver,
            crystal: session.crystal,
            equippedCostumes: session.equipped_costumes,
            ownedCostumes: ownedCostumes,
            isNewUser: session.needs_nickname,
            level: session.level,
            currentExp: session.currentExp,
            expToNextLevel: session.expToNextLevel,
            gameSessionToken: session.gameSessionToken,
            shopConstants: shopConstants  // 상점 UI 구성 데이터
        });

        // 현재 방 목록 전송
        const rooms = getAllRoomsForClient();
        sendMessage(ws, 'rooms_update', { rooms });

        logger.info(`[USER-IN][${user.nickname}] 인증 성공: sessionId=${sessionId.substring(0, 8)}`);

        if (user.receivedDailyReward === true) {
            sendMessage(ws, 'notification', {
                title: '일일 로그인 보상',
                body: `일일 로그인 보상으로\n금화 ${constants.DAILY_REWARD_GOLD}개를 받았습니다!`
            });
            logger.info(`[Auth][${user.nickname}] 일일 보상 알림 전송`);
        }

    } catch (error) {
        logger.error('[Auth][Unknown] 인증 처리 오류:', error);
        sendError(ws, 'SESSION_INVALID', '인증 처리 중 오류가 발생했습니다.');
    }
}

async function handleSetNickname(ws, data) {
    const { nickname: requestedNickname } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    // 닉네임 요청 로그
    logger.info(`[Auth][${session.nickname || providerId}] 닉네임 설정 요청: "${requestedNickname}"`);

    // 닉네임 검증 (형식 + 욕설 + 운영자 사칭)
    const validation = validateNickname(requestedNickname);
    if (!validation.isValid) {
        logger.info(`[Auth][${session.nickname || providerId}] ❌ 필터링됨: ${validation.reason} (${validation.errorType})`);
        sendMessage(ws, 'nickname_set', {
            ok: false,
            nickname: '',
            errorType: validation.errorType,
            message: validation.reason
        });
        return;
    }

    const cleanNickname = requestedNickname.trim();

    try {
        // 중복 체크
        const isDuplicate = await checkNicknameDuplicate(cleanNickname);
        if (isDuplicate) {
            logger.info(`[Auth][${session.nickname || providerId}] ❌ 중복된 닉네임: "${cleanNickname}"`);
            sendMessage(ws, 'nickname_set', {
                ok: false,
                nickname: '',
                errorType: 'NICKNAME_DUPLICATE',
                message: '이미 사용 중인 닉네임입니다.'
            });
            return;
        }

        // DB 닉네임 업데이트 (needs_nickname = 0으로 설정)
        await setUserNickname(session.provider, session.provider_id, cleanNickname);

        // 세션 닉네임 업데이트 (자동으로 update_nickname 전송)
        updateSessionNickname(providerId, cleanNickname);

        sendMessage(ws, 'nickname_set', { ok: true, nickname: cleanNickname, errorType: '', message: '' });

        logger.info(`[Auth][${cleanNickname}] ✅ 닉네임 설정 완료`);

    } catch (error) {
        logger.error('[Auth][Unknown] 닉네임 설정 오류:', error);
        sendMessage(ws, 'nickname_set', {
            ok: false,
            nickname: '',
            errorType: 'NICKNAME_SET_FAILED',
            message: '닉네임 설정에 실패했습니다.'
        });
    }
}

async function handleCreateRoom(ws, data) {
    const { roomName, hasPassword } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    // roomName trim 처리 (빈 문자열/공백만 있으면 null로 변환)
    const trimmedRoomName = roomName?.trim() || null;

    // 비밀번호 생성 (hasPassword가 true이면 랜덤 생성)
    const password = hasPassword ? generateRandomPassword() : null;

    logger.info(`[Room][${session.nickname}] 방 생성 요청: "${trimmedRoomName || '랜덤'}"${password ? ` (비밀방: ${password})` : ''}`);

    // 방 제목 검증 (trimmedRoomName이 제공된 경우에만)
    if (trimmedRoomName) {
        const validation = validateRoomName(trimmedRoomName);
        if (!validation.isValid) {
            logger.info(`[Room][${session.nickname}] ❌ 방 제목 필터링됨: ${validation.reason} (${validation.errorType})`);
            sendMessage(ws, 'create_room_response', {
                ok: false,
                roomId: '',
                roomName: '',
                password: '',
                ip: '',
                port: 0,
                errorType: validation.errorType,
                message: validation.reason
            });
            return;
        }
    }

    try {
        const room = await createRoom(trimmedRoomName, password);
        const responseData = { ok: true, roomId: room.roomId, roomName: room.roomName, password: room.password || '', ip: room.ip, port: room.port, errorType: '', message: '' };

        if (room.state === RoomState.WAITING) {
            logger.info(`[Room][Room-${room.roomId.substring(0, 6)}] 즉시 입장 가능`);
            sendMessage(ws, 'create_room_response', responseData);
        } else {
            logger.info(`[Room][Room-${room.roomId.substring(0, 6)}] 준비 대기 중`);
            addPendingRequest(room.roomId, ws, 'create_room_response', responseData);
        }

    } catch (error) {
        logger.error(`[Room][${session.nickname}] 방 생성 실패:`, error);
        sendMessage(ws, 'create_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_CREATE_FAILED', message: '방 생성에 실패했습니다.' });
    }
}

async function handleJoinRoom(ws, data) {
    const { roomId, password } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    logger.info(`[Room][${session.nickname}] 방 참가 요청:`, roomId ? `Room-${roomId.substring(0, 6)}` : '랜덤 매칭');

    try {
        let targetRoomId = roomId;
        let room = null;
        let responseData = null;

        if (targetRoomId) {
            room = getRoom(targetRoomId);
            if (!room) {
                sendMessage(ws, 'join_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_NOT_FOUND', message: '방을 찾을 수 없습니다.' });
                return;
            }
            if (room.state !== RoomState.WAITING && room.state !== RoomState.INITIALIZING) {
                sendMessage(ws, 'join_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_INVALID_STATE', message: '참가할 수 없는 방 상태입니다.' });
                return;
            }

            // 인원수 검증 (주석 처리 - 관전자 기능 추가로 꽉 찬 방도 참가 가능)
            // if (room.playerCount >= room.maxPlayers) {
            //     logger.info(`[Room][${session.nickname}] ❌ 방 인원 초과: Room-${targetRoomId.substring(0, 6)} (${room.playerCount}/${room.maxPlayers})`);
            //     sendMessage(ws, 'join_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_FULL', message: '방이 가득 찼습니다.' });
            //     return;
            // }

            // 비밀번호 검증
            if (room.password) {
                if (!password || password !== room.password) {
                    logger.info(`[Room][${session.nickname}] ❌ 비밀번호 불일치: Room-${targetRoomId.substring(0, 6)}`);
                    sendMessage(ws, 'join_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_WRONG_PASSWORD', message: '비밀번호가 일치하지 않습니다.' });
                    return;
                }
            }

            responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, password: room.password || '', ip: room.ip, port: room.port, errorType: '', message: '' };
        } else {
            targetRoomId = pickRandomWaitingRoom();
            if (targetRoomId) {
                room = getRoom(targetRoomId);
                responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, password: room.password || '', ip: room.ip, port: room.port, errorType: '', message: '' };
            } else {
                room = await createRoom(); // 랜덤 이름 생성
                targetRoomId = room.roomId;
                responseData = { ok: true, roomId: targetRoomId, roomName: room.roomName, password: room.password || '', ip: room.ip, port: room.port, errorType: '', message: '' };
            }
        }

        if (room.state === RoomState.WAITING) {
            logger.info(`[Room][Room-${targetRoomId.substring(0, 6)}] 즉시 입장 가능`);
            sendMessage(ws, 'join_room_response', responseData);
        } else {
            logger.info(`[Room][Room-${targetRoomId.substring(0, 6)}] 준비 대기 중`);
            addPendingRequest(targetRoomId, ws, 'join_room_response', responseData);
        }

    } catch (error) {
        logger.error(`[Room][${session.nickname}] 방 참가 실패:`, error);
        sendMessage(ws, 'join_room_response', { ok: false, roomId: '', roomName: '', password: '', ip: '', port: 0, errorType: 'ROOM_JOIN_FAILED', message: '방 참가에 실패했습니다.' });
    }
}

/**
 * 연결 해제 처리
 */
export async function handleDisconnection(ws, reason, err = null) {
    if (ws.authenticated && ws.providerId) {
        const session = getActiveSession(ws.providerId);

        // WebSocket 객체가 동일한 경우만 세션 제거 (중복 로그인 시 레이스 컨디션 방지)
        if (session && session.ws === ws) {
            logger.info(`[USER-OUT][${session.nickname || 'Unknown'}] 연결 해제: ${reason}`);

            // 마지막 로그아웃 시간 업데이트
            if (session.userId) {
                await updateLastLogout(session.userId);
            }

            removeActiveSession(ws.providerId);
        } else {
            logger.info(`[USER-OUT][${session?.nickname || ws.providerId}] 연결 해제 무시: 이미 새 세션으로 대체됨`);
        }
    } else {
        logger.info(`[WS][Guest] 연결 해제: ${reason}`);
    }
}

function sendMessage(ws, type, data) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type, ...data }));
    }
}

/**
 * 에러 메시지 전송
 * @param {WebSocket} ws - WebSocket 연결
 * @param {string} errorType - 에러 타입 (LobbyProtocol.cs의 ErrorType 참조)
 * @param {string} message - 에러 메시지
 */
function sendError(ws, errorType, message) {
    sendMessage(ws, 'error', { errorType, message });
    logger.error(`[Error][${errorType}] ${message}`);
}

/**
 * 파츠별 룰렛 뽑기 (금화 또는 크리스탈)
 */
async function handleShopBuyPartRoulette(ws, data) {
    const { partType, currency } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    // 1. currency 검증
    if (currency !== 'gold' && currency !== 'crystal') {
        sendMessage(ws, 'shop_buy_part_roulette_response', { ok: false, partType, roulettePool: [], wonCostume: '', isDuplicate: false, silverGained: 0, errorType: 'INVALID_REQUEST', message: '잘못된 화폐 타입입니다.' });
        return;
    }

    // 2. 파츠 타입 유효성 검증
    const shop = constants.SHOPS[partType];
    if (!shop) {
        sendMessage(ws, 'shop_buy_part_roulette_response', { ok: false, partType, roulettePool: [], wonCostume: '', isDuplicate: false, silverGained: 0, errorType: 'SHOP_INVALID_PART_TYPE', message: '잘못된 파츠 타입입니다.' });
        return;
    }

    // 3. 비용 및 잔액 확인
    const cost = currency === 'gold' ? shop.ROULETTE_GOLD_COST : shop.ROULETTE_CRYSTAL_COST;
    const balance = currency === 'gold' ? (session.gold || 0) : (session.crystal || 0);

    if (balance < cost) {
        const errorType = currency === 'gold' ? 'SHOP_INSUFFICIENT_GOLD' : 'SHOP_INSUFFICIENT_CRYSTAL';
        const message = currency === 'gold' ? '금화가 부족합니다.' : '크리스탈이 부족합니다.';
        sendMessage(ws, 'shop_buy_part_roulette_response', { ok: false, partType, roulettePool: [], wonCostume: '', isDuplicate: false, silverGained: 0, errorType, message });
        return;
    }

    try {
        // 4. 정규화/빈티어 필터 결과 확인
        const normalized = getNormalizedNonEmptyTiers(shop);
        if (normalized.length === 0) {
            sendMessage(ws, 'shop_buy_part_roulette_response', { ok: false, partType, roulettePool: [], wonCostume: '', isDuplicate: false, silverGained: 0, errorType: 'SHOP_NO_COSTUMES', message: '현재 뽑기 가능한 코스튬이 없습니다.' });
            return;
        }

        // 5. 룰렛 풀 생성 (8번 추첨)
        const roulettePool = [];
        for (let i = 0; i < shop.ROULETTE_POOL_SIZE; i++) {
            let result = null;
            // 재시도 루프
            for (let retry = 0; retry < 5; retry++) {
                try {
                    result = drawCostumeOnce(shop);
                    break;
                } catch (e) {
                    logger.warn('[Shop][System] 코스튬 추첨 재시도:', e.message);
                }
            }
            if (result == null) {
                logger.error('[Shop][System] 코스튬 추첨 실패: 기본 코스튬으로 대체');
                result = { costumeId: shop.TIERS.COMMON[0], tier: 'COMMON' };
            }
            roulettePool.push(result.costumeId);
        }

        // 6. 생성된 8개 룰렛 풀에서 랜덤 선택
        const wonCostume = roulettePool[Math.floor(Math.random() * roulettePool.length)];
        const wonTier = findCostumeTier(shop, wonCostume);

        // 7. 중복 확인
        const ownedCostumes = await getUserInventory(session.userId);
        const isDuplicate = (ownedCostumes[partType] || []).includes(wonCostume);

        // 8. 화폐 차감
        const newBalance = balance - cost;
        if (currency === 'gold') {
            await updateUserGold(session.userId, newBalance);
            updateSessionGold(providerId, newBalance);
        } else {
            await updateUserCrystal(session.userId, newBalance);
            updateSessionCrystal(providerId, newBalance);
        }

        // 9. 보상 처리
        let silverGained = 0;
        if (isDuplicate) {
            // 중복: 실버 지급 (룰렛 금화 비용 × 등급별 배율)
            const multiplier = constants.DUP_TIER_SILVER_MULTIPLIERS[wonTier] || 0.1;
            silverGained = Math.floor(shop.ROULETTE_GOLD_COST * multiplier);
            await addUserSilver(session.userId, silverGained);
            const newSilver = (session.silver || 0) + silverGained;
            updateSessionSilver(providerId, newSilver);
        } else {
            // 신규: 인벤토리 추가
            await addCostumeToInventory(session.userId, partType, wonCostume);
        }

        // 10. 업데이트된 인벤토리 조회
        const updatedOwnedCostumes = await getUserInventory(session.userId);

        // 11. 응답 전송
        sendMessage(ws, 'shop_buy_part_roulette_response', {
            ok: true,
            partType,
            roulettePool,
            wonCostume,
            isDuplicate,
            silverGained,
            errorType: '',
            message: ''
        });

        // 인벤토리 업데이트 전송
        sendMessage(ws, 'update_inventory', { ownedCostumes: updatedOwnedCostumes });

        const currencyName = currency === 'gold' ? '금화' : '크리스탈';
        logger.info(
            `[Shop][${session.nickname}] ${partType} ${currencyName} 룰렛: ${wonCostume} (${wonTier}) 획득` +
            (isDuplicate ? ` (중복, +${silverGained}실버)` : '')
        );

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 룰렛 뽑기 오류:`, error);
        sendMessage(ws, 'shop_buy_part_roulette_response', { ok: false, partType, roulettePool: [], wonCostume: '', isDuplicate: false, silverGained: 0, errorType: 'SHOP_ROULETTE_FAILED', message: '룰렛 뽑기에 실패했습니다.' });
    }
}

/**
 * 파츠별 5연뽑 룰렛 (금화 또는 크리스탈)
 */
async function handleShopBuyPartRoulette5x(ws, data) {
    const { partType, currency } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');
        return;
    }

    // 1. currency 검증
    if (currency !== 'gold' && currency !== 'crystal') {
        sendMessage(ws, 'shop_buy_part_roulette_5x_response', { ok: false, partType, roulettePool: [], wonCostumes: [], isDuplicates: [], totalSilverGained: 0, errorType: 'INVALID_REQUEST', message: '잘못된 화폐 타입입니다.' });
        return;
    }

    // 2. 파츠 타입 유효성 검증
    const shop = constants.SHOPS[partType];
    if (!shop) {
        sendMessage(ws, 'shop_buy_part_roulette_5x_response', { ok: false, partType, roulettePool: [], wonCostumes: [], isDuplicates: [], totalSilverGained: 0, errorType: 'SHOP_INVALID_PART_TYPE', message: '잘못된 파츠 타입입니다.' });
        return;
    }

    // 3. 비용 및 잔액 확인 (5배)
    const singleCost = currency === 'gold' ? shop.ROULETTE_GOLD_COST : shop.ROULETTE_CRYSTAL_COST;
    const cost = singleCost * 5;
    const balance = currency === 'gold' ? (session.gold || 0) : (session.crystal || 0);

    if (balance < cost) {
        const errorType = currency === 'gold' ? 'SHOP_INSUFFICIENT_GOLD' : 'SHOP_INSUFFICIENT_CRYSTAL';
        const message = currency === 'gold' ? '금화가 부족합니다.' : '크리스탈이 부족합니다.';
        sendMessage(ws, 'shop_buy_part_roulette_5x_response', { ok: false, partType, roulettePool: [], wonCostumes: [], isDuplicates: [], totalSilverGained: 0, errorType, message });
        return;
    }

    try {
        // 4. 정규화/빈티어 필터 결과 확인
        const normalized = getNormalizedNonEmptyTiers(shop);
        if (normalized.length === 0) {
            sendMessage(ws, 'shop_buy_part_roulette_5x_response', { ok: false, partType, roulettePool: [], wonCostumes: [], isDuplicates: [], totalSilverGained: 0, errorType: 'SHOP_NO_COSTUMES', message: '현재 뽑기 가능한 코스튬이 없습니다.' });
            return;
        }

        // 5. 룰렛 풀 생성 (8번 추첨)
        const roulettePool = [];
        for (let i = 0; i < shop.ROULETTE_POOL_SIZE; i++) {
            let result = null;
            for (let retry = 0; retry < 5; retry++) {
                try {
                    result = drawCostumeOnce(shop);
                    break;
                } catch (e) {
                    logger.warn('[Shop][System] 코스튬 추첨 재시도:', e.message);
                }
            }
            if (result == null) {
                logger.error('[Shop][System] 코스튬 추첨 실패: 기본 코스튬으로 대체');
                result = { costumeId: shop.TIERS.COMMON[0], tier: 'COMMON' };
            }
            roulettePool.push(result.costumeId);
        }

        // 6. 8개 중 5개 랜덤 선택 (중복 가능)
        const wonCostumes = [];
        for (let i = 0; i < 5; i++) {
            const randomIndex = Math.floor(Math.random() * roulettePool.length);
            wonCostumes.push(roulettePool[randomIndex]);
        }

        // 7. 화폐 차감
        const newBalance = balance - cost;
        if (currency === 'gold') {
            await updateUserGold(session.userId, newBalance);
            updateSessionGold(providerId, newBalance);
        } else {
            await updateUserCrystal(session.userId, newBalance);
            updateSessionCrystal(providerId, newBalance);
        }

        // 8. 각 코스튬에 대해 중복 체크 및 보상 처리
        let ownedCostumes = await getUserInventory(session.userId);
        const isDuplicates = [];
        let totalSilverGained = 0;

        for (const wonCostume of wonCostumes) {
            const wonTier = findCostumeTier(shop, wonCostume);
            const isDuplicate = (ownedCostumes[partType] || []).includes(wonCostume);
            isDuplicates.push(isDuplicate);

            if (isDuplicate) {
                // 중복: 실버 지급
                const multiplier = constants.DUP_TIER_SILVER_MULTIPLIERS[wonTier] || 0.1;
                const silverGained = Math.floor(shop.ROULETTE_GOLD_COST * multiplier);
                totalSilverGained += silverGained;
            } else {
                // 신규: 인벤토리 추가
                await addCostumeToInventory(session.userId, partType, wonCostume);
                // 인벤토리 갱신 (다음 루프에서 중복 체크를 위해)
                ownedCostumes = await getUserInventory(session.userId);
            }
        }

        // 9. 총 실버 지급
        if (totalSilverGained > 0) {
            await addUserSilver(session.userId, totalSilverGained);
            const newSilver = (session.silver || 0) + totalSilverGained;
            updateSessionSilver(providerId, newSilver);
        }

        // 10. 최종 인벤토리 조회
        const updatedOwnedCostumes = await getUserInventory(session.userId);

        // 11. 응답 전송
        sendMessage(ws, 'shop_buy_part_roulette_5x_response', {
            ok: true,
            partType,
            roulettePool,
            wonCostumes,
            isDuplicates,
            totalSilverGained,
            errorType: '',
            message: ''
        });

        // 인벤토리 업데이트 전송
        sendMessage(ws, 'update_inventory', { ownedCostumes: updatedOwnedCostumes });

        const currencyName = currency === 'gold' ? '금화' : '크리스탈';
        const duplicateCount = isDuplicates.filter(d => d).length;
        logger.info(
            `[Shop][${session.nickname}] ${partType} ${currencyName} 5연뽑: 총 ${wonCostumes.length}개 획득` +
            (duplicateCount > 0 ? ` (중복 ${duplicateCount}개, +${totalSilverGained}실버)` : '')
        );

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 5연뽑 오류:`, error);
        sendMessage(ws, 'shop_buy_part_roulette_5x_response', { ok: false, partType, roulettePool: [], wonCostumes: [], isDuplicates: [], totalSilverGained: 0, errorType: 'SHOP_ROULETTE_FAILED', message: '5연뽑에 실패했습니다.' });
    }
}

/**
 * 파츠별 직접 구매 (실버 또는 크리스탈)
 */
async function handleShopBuyPartDirect(ws, data) {
    const { partType, costumeId, currency } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    // 1. currency 검증
    if (currency !== 'silver' && currency !== 'crystal') {
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'INVALID_REQUEST', message: '잘못된 화폐 타입입니다.' });
        return;
    }

    // 2. 파츠 타입 유효성 검증
    const shop = constants.SHOPS[partType];
    if (!shop) {
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'SHOP_INVALID_PART_TYPE', message: '잘못된 파츠 타입입니다.' });
        return;
    }

    // 3. 코스튬 티어 확인
    const tier = findCostumeTier(shop, costumeId);
    if (!tier) {
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'SHOP_INVALID_COSTUME', message: '구매할 수 없는 코스튬입니다.' });
        return;
    }

    // 4. 티어별 가격 확인
    const multiplier = currency === 'silver'
        ? constants.TIER_DIRECT_SILVER_PRICE_MULTIPLIER[tier]
        : constants.TIER_DIRECT_CRYSTAL_PRICE_MULTIPLIER[tier];

    if (!multiplier) {
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'SHOP_INVALID_COSTUME', message: '구매할 수 없는 코스튬입니다.' });
        return;
    }

    const baseCost = currency === 'silver' ? shop.ROULETTE_GOLD_COST : shop.ROULETTE_CRYSTAL_COST;
    const price = baseCost * multiplier;

    // 5. 화폐 잔액 확인
    const balance = currency === 'silver' ? (session.silver || 0) : (session.crystal || 0);

    if (balance < price) {
        const errorType = currency === 'silver' ? 'SHOP_INSUFFICIENT_SILVER' : 'SHOP_INSUFFICIENT_CRYSTAL';
        const message = currency === 'silver' ? '실버가 부족합니다.' : '크리스탈이 부족합니다.';
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType, message });
        return;
    }

    // 6. 중복 확인
    const ownedCostumes = await getUserInventory(session.userId);
    if ((ownedCostumes[partType] || []).includes(costumeId)) {
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'SHOP_ALREADY_OWNED', message: '이미 보유한 코스튬입니다.' });
        return;
    }

    try {
        // 7. 화폐 차감
        const newBalance = balance - price;
        if (currency === 'silver') {
            await updateUserSilver(session.userId, newBalance);
            updateSessionSilver(providerId, newBalance);
        } else {
            await updateUserCrystal(session.userId, newBalance);
            updateSessionCrystal(providerId, newBalance);
        }

        // 8. 인벤토리 추가
        await addCostumeToInventory(session.userId, partType, costumeId);

        // 9. 업데이트된 인벤토리 조회
        const updatedOwnedCostumes = await getUserInventory(session.userId);

        // 10. 응답 전송
        sendMessage(ws, 'shop_buy_part_direct_response', {
            ok: true,
            partType,
            costumeId,
            errorType: '',
            message: ''
        });

        // 인벤토리 업데이트 전송
        sendMessage(ws, 'update_inventory', { ownedCostumes: updatedOwnedCostumes });

        const currencyName = currency === 'silver' ? '실버' : '크리스탈';
        logger.info(`[Shop][${session.nickname}] ${partType} ${currencyName} 직접 구매: ${costumeId} (-${price}${currencyName})`);

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 직접 구매 오류:`, error);
        sendMessage(ws, 'shop_buy_part_direct_response', { ok: false, partType, costumeId: '', errorType: 'SHOP_DIRECT_FAILED', message: '직접 구매에 실패했습니다.' });
    }
}

/**
 * 파츠별 확률 정보 조회
 */
async function handleGetPartRates(ws, data) {
    const { partType } = data;

    const shop = constants.SHOPS[partType];
    if (!shop) {
        sendMessage(ws, 'part_rates_response', { ok: false, partType, rates: [], errorType: 'SHOP_INVALID_PART_TYPE', message: '잘못된 파츠 타입입니다.' });
        return;
    }

    const normalized = getNormalizedNonEmptyTiers(shop);

    if (normalized.length === 0) {
        sendMessage(ws, 'part_rates_response', { ok: true, partType, rates: [], errorType: '', message: '' });
        return;
    }

    const rates = [];
    for (const x of normalized) {
        const per = x.weight / x.list.length;
        for (const costumeId of x.list) {
            rates.push({
                costumeId,
                rate: per
            });
        }
    }

    sendMessage(ws, 'part_rates_response', { ok: true, partType, rates, errorType: '', message: '' });
}

/**
 * 코스튬 착용 변경 (파츠별)
 */
async function handleChangeCostume(ws, data) {
    const { partType, costumeId } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');  // 시스템 에러
        return;
    }

    // 착용 슬롯 유효성 검증 (Paint 슬롯 포함)
    if (!(partType in constants.EQUIPPABLE_SLOTS_DEFAULTS)) {
        sendMessage(ws, 'change_costume_response', { ok: false, partType, costumeId: '', errorType: 'COSTUME_INVALID_PART_TYPE', message: '잘못된 파츠 타입입니다.' });
        return;
    }

    const ownedCostumes = await getUserInventory(session.userId);

    // Paint 파츠는 'Paint' 카테고리에서 검색
    const isPaint = partType.endsWith('_Paint');
    const inventoryPartType = isPaint ? 'Paint' : partType;

    // 보유 확인 (디폴트 파츠도 인벤토리에 있음)
    if (!(ownedCostumes[inventoryPartType] || []).includes(costumeId)) {
        sendMessage(ws, 'change_costume_response', { ok: false, partType, costumeId: '', errorType: 'COSTUME_NOT_OWNED', message: '보유하지 않은 코스튬입니다.' });
        return;
    }

    try {
        await updateEquippedCostume(session.userId, partType, costumeId);
        updateSessionEquippedCostume(providerId, partType, costumeId);

        // 성공 응답 전송
        sendMessage(ws, 'change_costume_response', {
            ok: true,
            partType,
            costumeId,
            errorType: '',
            message: ''
        });

        logger.info(`[Shop][${session.nickname}] 코스튬 착용: ${partType} = ${costumeId}`);

    } catch (error) {
        logger.error(`[Shop][${session?.nickname || 'Unknown'}] 코스튬 착용 오류:`, error);
        sendMessage(ws, 'change_costume_response', { ok: false, partType, costumeId: '', errorType: 'COSTUME_CHANGE_FAILED', message: '코스튬 착용에 실패했습니다.' });
    }
}

/**
 * 인앱 결제 처리 (영수증 검증 없이 무조건 지급)
 */
async function handleIAPPurchase(ws, data) {
    const { productId, payload } = data;
    const { providerId } = ws;
    const session = getActiveSession(providerId);

    // 1. 세션 검증
    if (!session) {
        sendError(ws, 'SESSION_INVALID', '세션을 찾을 수 없습니다.');
        return;
    }

    // 2. 상품 유효성 검증
    const product = constants.IAP_PRODUCTS[productId];
    if (!product) {
        sendMessage(ws, 'iap_purchase_response', {
            ok: false,
            productId,
            crystalAmount: 0,
            errorType: 'IAP_INVALID_PRODUCT',
            message: '유효하지 않은 상품입니다.'
        });
        return;
    }

    try {
        // 3. 영수증 payload를 logs/receipt에 저장
        const receiptDir = path.join(process.cwd(), 'logs', 'receipt');
        if (!fs.existsSync(receiptDir)) {
            fs.mkdirSync(receiptDir, { recursive: true });
        }

        const timestamp = new Date().toISOString().replace(/:/g, '-');
        const filename = `${timestamp}_${session.userId}_${productId}.json`;
        const filepath = path.join(receiptDir, filename);

        fs.writeFileSync(filepath, JSON.stringify({
            timestamp: new Date().toISOString(),
            userId: session.userId,
            nickname: session.nickname,
            productId,
            payload
        }, null, 2));

        // 4. 크리스탈 지급 (검증 없이 무조건 지급)
        const crystalAmount = product.crystalAmount;
        await addUserCrystal(session.userId, crystalAmount);

        // 5. 세션 업데이트 (자동으로 클라이언트에 update_crystal 전송)
        const newCrystal = (session.crystal || 0) + crystalAmount;
        updateSessionCrystal(providerId, newCrystal);

        // 6. 성공 응답
        sendMessage(ws, 'iap_purchase_response', {
            ok: true,
            productId,
            crystalAmount,
            errorType: '',
            message: ''
        });

        logger.info(`[IAP][${session.nickname}] 결제 완료: ${productId} (+${crystalAmount} 크리스탈) - 영수증 저장됨: ${filename}`);

    } catch (error) {
        logger.error(`[IAP][${session?.nickname || 'Unknown'}] 결제 처리 오류:`, error);
        sendMessage(ws, 'iap_purchase_response', {
            ok: false,
            productId,
            crystalAmount: 0,
            errorType: 'IAP_VERIFICATION_FAILED',
            message: '결제 처리 중 오류가 발생했습니다.'
        });
    }
}
