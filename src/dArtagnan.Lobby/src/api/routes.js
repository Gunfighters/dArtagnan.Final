import { logger } from '../logger.js';
import { config } from '../config.js';
import { processDevLogin, processUnityOAuth, processAppleOAuth } from '../auth/oauth.js';
import { updateRoomState, updatePlayerCount, updateRoomName, getAllRoomIds } from '../rooms/manager.js';
import { findUserByProvider, updateUserGold, incrementWinCount, addExperience } from '../database.js';
import { getActiveSession, updateSessionGold, getAllActiveSessions, setUserCurrentRoom, updateSessionExperience, getSessionByGameToken } from '../auth/session.js';
import { constants } from '../constants.js';

/**
 * 내부 API 인증 미들웨어
 * Authorization: Bearer {INTERNAL_API_SECRET} 헤더 검증
 */
function verifyInternalApiKey(req, res, next) {
    const authHeader = req.headers['authorization'];
    const expectedSecret = process.env.INTERNAL_API_SECRET;

    // 시크릿이 설정되지 않은 경우 경고 로그 출력 후 통과
    if (!expectedSecret) {
        logger.warn('[API][Security] INTERNAL_API_SECRET이 설정되지 않음 - 내부 API 보안 비활성화\n Todo: 환경변수에 INTERNAL_API_SECRET 추가');
        return next();
    }

    // Authorization 헤더 검증
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        logger.warn(`[API][Security] 내부 API 인증 실패: Authorization 헤더 없음 (${req.path})`);
        return res.status(401).json({
            message: '인증이 필요합니다.'
        });
    }

    const providedSecret = authHeader.substring(7); // "Bearer " 제거

    if (providedSecret !== expectedSecret) {
        logger.warn(`[API][Security] 내부 API 인증 실패: 잘못된 시크릿 키 (${req.path})`);
        return res.status(401).json({
            message: '인증이 유효하지 않습니다.'
        });
    }

    next();
}

/**
 * 버전 체크 함수 (토큰 검증 전에 먼저 체크)
 * 메이저.마이너 버전만 비교 (패치 버전은 무시)
 * 예: 클라 "1.2.345", 서버 "1.2.0" → 통과
 *
 * 개발 서버 (dartagnan.shop)에서는 버전 체크를 건너뛰기
 */
function checkVersion(clientVersion) {
    // 개발 서버인 경우 버전 체크 건너뛰기
    if (config.publicDomain.includes('dartagnan.shop')) {
        logger.info(`[Auth] 개발 서버 (${config.publicDomain}): 버전 체크 건너뛰기`);
        return true;
    }

    if (!clientVersion) {
        logger.warn(`[Auth] 버전 없음: 클라이언트 버전이 전송되지 않음`);
        return false;
    }

    // "1.2.345" → "1.2"
    const clientMajorMinor = clientVersion.split('.').slice(0, 2).join('.');
    const requiredMajorMinor = config.version.requiredClientVersion.split('.').slice(0, 2).join('.');

    if (clientMajorMinor !== requiredMajorMinor) {
        logger.warn(`[Auth] 버전 불일치: 클라이언트=${clientVersion} (${clientMajorMinor}), 서버=${config.version.requiredClientVersion} (${requiredMajorMinor})`);
        return false;
    }

    return true;
}

/**
 * Express 라우트 설정
 */
export function setupRoutes(app) {
    // 개발용 로그인 (버전 체크 없음)
    app.post('/login', async (req, res) => {
        try {
            const { providerId, clientVersion } = req.body;

            logger.info(`[Auth][Dev] 로그인 요청: ${providerId} (버전 체크 건너뛰기)`);

            const result = await processDevLogin(providerId, clientVersion);
            res.json(result);
        } catch (error) {
            logger.error(`[API][${req.body.providerId || 'Unknown'}] 개발 로그인 실패:`, error);

            let statusCode = 400;
            if (error.message.includes('already exists')) {
                statusCode = 409;
            }

            res.status(statusCode).json({
                message: error.message
            });
        }
    });

    // Google OAuth
    app.post('/auth/google/verify-token', async (req, res) => {
        try {
            const { token, clientVersion } = req.body;

            // 토큰 검증 전 버전 체크
            if (!checkVersion(clientVersion)) {
                return res.status(426).json({
                    message: `버전이 맞지 않습니다. 스토어에서 앱을 업데이트 해주세요 (필요한 버전: ${config.version.requiredClientVersion})`
                });
            }

            const result = await processUnityOAuth(token, clientVersion);
            res.json(result);
        } catch (error) {
            logger.error('[API][Unknown] Google OAuth 실패:', error);

            res.status(401).json({
                message: 'Google 인증에 실패했습니다.'
            });
        }
    });

    // Apple OAuth
    app.post('/auth/apple/verify-identity', async (req, res) => {
        try {
            const authData = req.body;

            // 토큰 검증 전 버전 체크
            if (!checkVersion(authData.clientVersion)) {
                return res.status(426).json({
                    message: `버전이 맞지 않습니다. 스토어에서 앱을 업데이트 해주세요 (필요한 버전: ${config.version.requiredClientVersion})`
                });
            }

            const result = await processAppleOAuth(authData);
            res.json(result);
        } catch (error) {
            logger.error('[API][Unknown] Apple OAuth 실패:', error);

            res.status(401).json({
                message: 'Apple Game Center 인증에 실패했습니다.'
            });
        }
    });

    // 게임서버 상태 업데이트 (내부 API)
    app.post('/internal/rooms/:roomId/state', verifyInternalApiKey, (req, res) => {
        const { roomId } = req.params;
        const { state: newState } = req.body;

        logger.info(`[API][Room-${roomId.substring(0, 6)}] 상태 변경 요청: → ${newState}`);

        const room = updateRoomState(roomId, newState);
        if (!room) {
            const currentRooms = getAllRoomIds();
            logger.error(`[API][Room-${roomId.substring(0, 6)}] 존재하지 않는 방 (현재 방: ${currentRooms.length}개)`);
            return res.status(404).json({
                message: '방을 찾을 수 없습니다.'
            });
        }

        res.json({ ok: true });
    });

    // 게임서버 인원수 업데이트 (내부 API)
    app.post('/internal/rooms/:roomId/player-count', verifyInternalApiKey, (req, res) => {
        const { roomId } = req.params;
        const { playerCount } = req.body;

        if (typeof playerCount !== 'number' || playerCount < 0) {
            return res.status(400).json({
                message: '유효하지 않은 플레이어 수입니다.'
            });
        }

        logger.info(`[API][Room-${roomId.substring(0, 6)}] 인원수 변경 요청: → ${playerCount}명`);

        const room = updatePlayerCount(roomId, playerCount);
        if (!room) {
            const currentRooms = getAllRoomIds();
            logger.error(`[API][Room-${roomId.substring(0, 6)}] 존재하지 않는 방 (현재 방: ${currentRooms.length}개)`);
            return res.status(404).json({
                message: '방을 찾을 수 없습니다.'
            });
        }

        res.json({ ok: true });
    });

    // 현재 접속자 조회 (내부 API)
    app.get('/internal/sessions', verifyInternalApiKey, (req, res) => {
        const sessions = getAllActiveSessions();
        logger.info(`[API][System] 접속자 조회: ${sessions.length}명`);
        res.json({
            count: sessions.length,
            sessions: sessions
        });
    });

    // 게임 세션 토큰 검증 (내부 API)
    app.post('/internal/validate-game-session', verifyInternalApiKey, (req, res) => {
        const { sessionToken } = req.body;

        if (!sessionToken) {
            return res.status(400).json({
                ok: false,
                message: '세션 토큰이 필요합니다.'
            });
        }

        const sessionData = getSessionByGameToken(sessionToken);

        if (!sessionData) {
            logger.warn(`[API][System] 유효하지 않은 게임 세션 토큰: ${sessionToken.substring(0, 10)}...`);
            return res.status(401).json({
                ok: false,
                message: '유효하지 않은 세션입니다.'
            });
        }

        logger.info(`[API][${sessionData.nickname}] 게임 세션 검증 성공`);
        res.json({
            ok: true,
            providerId: sessionData.providerId,
            nickname: sessionData.nickname,
            equippedCostumes: sessionData.equippedCostumes
        });
    });

    // 플레이어 게임 입장 (내부 API)
    app.post('/internal/player-join', verifyInternalApiKey, (req, res) => {
        const { providerId, roomId } = req.body;

        if (!providerId || !roomId) {
            return res.status(400).json({
                message: '유효하지 않은 요청 데이터입니다.'
            });
        }

        const session = getActiveSession(providerId);
        if (!session) {
            logger.warn(`[API][${providerId}] 세션 없음: 플레이어 입장 무시`);
            return res.status(404).json({
                message: '세션을 찾을 수 없습니다.'
            });
        }

        setUserCurrentRoom(providerId, roomId);
        logger.info(`[API][${session.nickname}] 게임 입장: Room-${roomId.substring(0, 6)}`);
        res.json({ ok: true });
    });

    // 플레이어 게임 퇴장 (내부 API)
    app.post('/internal/player-leave', verifyInternalApiKey, (req, res) => {
        const { providerId } = req.body;

        if (!providerId) {
            return res.status(400).json({
                message: '유효하지 않은 요청 데이터입니다.'
            });
        }

        const session = getActiveSession(providerId);
        if (!session) {
            logger.warn(`[API][${providerId}] 세션 없음: 플레이어 퇴장 무시`);
            return res.status(404).json({
                message: '세션을 찾을 수 없습니다.'
            });
        }

        setUserCurrentRoom(providerId, null);
        logger.info(`[API][${session.nickname}] 게임 퇴장: 로비로 복귀`);
        res.json({ ok: true });
    });

    // 게임서버 방 이름 업데이트 (내부 API)
    // 게임 서버가 이미 검증했으므로 로비 서버는 단순히 반영만 함
    app.post('/internal/rooms/:roomId/room-name', verifyInternalApiKey, (req, res) => {
        const { roomId } = req.params;
        const { roomName } = req.body;

        logger.info(`[API][Room-${roomId.substring(0, 6)}] 방 이름 변경 요청: "${roomName}"`);

        const room = updateRoomName(roomId, roomName);
        if (!room) {
            const currentRooms = getAllRoomIds();
            logger.error(`[API][Room-${roomId.substring(0, 6)}] 존재하지 않는 방 (현재 방: ${currentRooms.length}개)`);
            return res.status(404).json({
                message: '방을 찾을 수 없습니다.'
            });
        }

        res.json({ ok: true });
    });

    // 게임 결과 벌크 처리 (내부 API) - 순위별 경험치 및 상금 지급
    app.post('/internal/game-results', verifyInternalApiKey, async (req, res) => {
        const { players } = req.body;

        if (!players || !Array.isArray(players) || players.length === 0) {
            return res.status(400).json({
                message: '유효하지 않은 요청 데이터입니다.'
            });
        }

        logger.info(`[API][System] 게임 결과 벌크 처리 시작: ${players.length}명`);

        const results = [];
        let successCount = 0;
        let errorCount = 0;

        for (const playerResult of players) {
            const { providerId, rank, rewardMoney } = playerResult;

            try {
                const user = await findUserByProvider(providerId);

                if (!user) {
                    logger.error(`[API][${providerId}] 존재하지 않는 사용자`);
                    errorCount++;
                    results.push({ providerId, success: false, error: '사용자를 찾을 수 없음' });
                    continue;
                }

                // 순위별 경험치 지급 (1~7등만)
                const expGain = constants.RANK_EXPERIENCE[rank] || 0;
                let expResult = null;

                if (expGain > 0) {
                    expResult = await addExperience(user.id, expGain);

                    // 활성 세션이 있으면 경험치 업데이트
                    const activeSession = getActiveSession(providerId);
                    if (activeSession && expResult) {
                        updateSessionExperience(providerId, expResult.newExp);
                    }
                }

                // 순위별 상금 지급 (rewardMoney가 0보다 클 때만)
                let goldGain = 0;
                let newGold = user.gold;

                if (rewardMoney > 0) {
                    newGold = user.gold + rewardMoney;
                    await updateUserGold(user.id, newGold);
                    goldGain = rewardMoney;

                    // 1등만 승리 횟수 증가
                    if (rank === 1) {
                        await incrementWinCount(user.id);
                    }

                    // 활성 세션이 있으면 금화 업데이트
                    const activeSession = getActiveSession(providerId);
                    if (activeSession) {
                        updateSessionGold(providerId, newGold);
                    }

                    logger.info(`[API][${user.nickname}] ${rank}등 보상: +${expGain}exp, +${rewardMoney}금화 → ${newGold}${rank === 1 ? ', 승리 +1' : ''}`);
                } else {
                    logger.info(`[API][${user.nickname}] ${rank}등 보상: +${expGain}exp, 상금 없음`);
                }

                results.push({
                    providerId,
                    success: true,
                    rank: rank,
                    expGain: expGain,
                    goldGain: goldGain
                });
                successCount++;

            } catch (error) {
                logger.error(`[API][${providerId}] 게임 결과 처리 실패:`, error);
                errorCount++;
                results.push({ providerId, success: false, error: error.message });
            }
        }

        logger.info(`[API][System] 게임 결과 벌크 처리 완료: 성공 ${successCount}명, 실패 ${errorCount}명`);

        res.json({ ok: true });
    });

}