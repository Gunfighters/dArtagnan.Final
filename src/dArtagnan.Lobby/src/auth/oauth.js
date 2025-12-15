import { OAuth2Client } from 'google-auth-library';
import crypto from 'crypto';
import { config } from '../config.js';
import { logger } from '../logger.js';
import { findUserByProvider, createUser, generateTempNickname, updateLastLogin, updateUserGold, updateLastDailyReward } from '../database.js';
import { createOAuthSession } from './session.js';
import { constants } from '../constants.js';

// Google OAuth 클라이언트
const googleClient = new OAuth2Client(
    config.oauth.google.clientId,
    config.oauth.google.clientSecret
);

/**
 * 개발용 로그인 처리
 */
export async function processDevLogin(providerId, clientVersion) {
    try {
        if (!providerId || providerId.trim().length === 0) {
            throw new Error('ProviderId is required.');
        }

        const cleanProviderId = providerId.trim();
        if (cleanProviderId.length < 1 || cleanProviderId.length > 16) {
            throw new Error('ProviderId must be 1-16 characters.');
        }

        logger.info(`[Auth][Dev] 로그인 요청: ${cleanProviderId} (버전: ${clientVersion || 'unknown'})`);

        return await processLogin('dev', cleanProviderId, cleanProviderId);

    } catch (error) {
        logger.error(`[Auth][Dev] 실패:`, error);
        throw error;
    }
}

/**
 * Google OAuth 처리
 */
export async function processUnityOAuth(authCode, clientVersion) {
    try {
        if (!authCode) {
            throw new Error('Authorization Code is required.');
        }

        logger.info(`[Auth][Google] Auth Code 수신: ${authCode.substring(0, 10)}... (버전: ${clientVersion || 'unknown'})`);

        const tokens = await exchangeAuthCodeForToken(authCode);
        const playerInfo = await getPlayerInfo(tokens.access_token);

        return await processLogin('google', playerInfo.playerId, playerInfo.displayName);

    } catch (error) {
        logger.error(`[Auth][Google] 실패:`, error);
        throw error;
    }
}

/**
 * Apple OAuth 처리
 */
export async function processAppleOAuth(authData) {
    try {
        logger.info(`[Auth][Apple] 인증 요청 수신 (버전: ${authData.clientVersion || 'unknown'})`);

        if (!authData.teamPlayerId) {
            throw new Error('Apple teamPlayerId is required');
        }

        const playerId = authData.teamPlayerId;
        const displayName = authData.alias || generateTempNickname();

        logger.info(`[Auth][Apple] Player ID: ${playerId}, Display Name: ${displayName}`);

        return await processLogin('apple', playerId, displayName);

    } catch (error) {
        logger.error(`[Auth][Apple] 실패:`, error);
        throw error;
    }
}

/**
 * Authorization Code를 Access Token으로 교환 (Google)
 */
async function exchangeAuthCodeForToken(authCode) {
    try {
        const { tokens } = await googleClient.getToken(authCode);
        return tokens;
    } catch (error) {
        logger.error('[OAuth] Token 교환 실패:', error);
        throw new Error('Failed to exchange authorization code for token');
    }
}

/**
 * Google Play Games 플레이어 정보 조회
 */
async function getPlayerInfo(accessToken) {
    try {
        const response = await fetch('https://games.googleapis.com/games/v1/players/me', {
            headers: {
                'Authorization': `Bearer ${accessToken}`
            }
        });

        if (!response.ok) {
            throw new Error(`Google API 호출 실패: ${response.status}`);
        }

        const data = await response.json();
        return {
            playerId: data.playerId,
            displayName: data.displayName
        };
    } catch (error) {
        logger.error('[OAuth] 플레이어 정보 조회 실패:', error);
        throw new Error('Failed to get player info from Google');
    }
}

/**
 * 지정된 시간 기준으로 날짜가 다른지 확인
 */
function isDifferentDay(lastDate, currentDate, resetHour) {
    if (!lastDate) return true;

    const adjust = (date) => {
        const d = new Date(date);
        // 기준 시간 이전이면 전날로 계산
        if (d.getHours() < resetHour) {
            d.setDate(d.getDate() - 1);
        }
        return d.toISOString().split('T')[0]; // YYYY-MM-DD
    };

    return adjust(lastDate) !== adjust(currentDate);
}

/**
 * 공통 로그인 처리 로직
 */
async function processLogin(provider, providerId, displayName) {
    let user = await findUserByProvider(providerId);

    if (!user) {
        const nickname = displayName || generateTempNickname();
        await createUser(provider, providerId, nickname);
        user = await findUserByProvider(providerId);
        logger.info(`[Auth][${provider.toUpperCase()}] 신규 사용자 생성: ${providerId}`);
    } else {
        logger.info(`[Auth][${provider.toUpperCase()}] 기존 사용자 로그인: ${providerId}`);
    }

    // 일일 로그인 보상 체크 (마지막 로그인 시간 업데이트 전)
    const now = new Date();
    const lastReward = user.last_daily_reward_at;

    if (isDifferentDay(lastReward, now, constants.DAILY_REWARD_RESET_HOUR)) {
        // 일일 보상 금화 지급
        const rewardGold = constants.DAILY_REWARD_GOLD;
        const newGold = user.gold + rewardGold;
        await updateUserGold(user.id, newGold);
        await updateLastDailyReward(user.id);

        // user 객체 갱신
        user.gold = newGold;
        user.last_daily_reward_at = now;
        user.receivedDailyReward = true;

        logger.info(`[Auth][${provider.toUpperCase()}] 일일 보상 지급: ${user.nickname} (+${rewardGold}금화 → ${newGold})`);
    }

    // 마지막 로그인 시간 업데이트
    await updateLastLogin(user.id);

    // OAuth 세션 생성 (DB user 객체 그대로 저장)
    const sessionId = createOAuthSession(providerId, user);

    logger.info(`[Auth][${provider.toUpperCase()}] 로그인 완료: ${user.nickname} (${sessionId})`);

    return {
        success: true,
        sessionId,
        nickname: user.nickname,
        isNewUser: user.needs_nickname === 1
    };
}