/**
 * 로비 서버 설정 - 모든 설정값 중앙화
 */

import { execSync } from 'child_process';
import { logger } from './logger.js';

/**
 * Git 태그에서 요구 클라이언트 버전 가져오기
 * 최근 태그를 읽어서 "메이저.마이너.x" 형식으로 변환
 * 예: "v1.2.345" → "1.2.x"
 */
function getRequiredClientVersion() {
    return '1.2.x';

    // git 태그 기반 버전 체크
    // try {
    //     // 가장 최근 Git 태그 가져오기
    //     const tag = execSync('git describe --tags --abbrev=0', {
    //         encoding: 'utf8',
    //         cwd: process.cwd(),
    //         stdio: ['pipe', 'pipe', 'ignore']  // stderr 무시
    //     }).trim();

    //     // "v1.2.345" → "1.2.345"
    //     const versionStr = tag.replace(/^v/, '');

    //     // "1.2.345" → ["1", "2", "345"]
    //     const parts = versionStr.split('.');

    //     if (parts.length < 2) {
    //         throw new Error(`Invalid version format: ${tag}`);
    //     }

    //     // "1.2.x" 형식으로 변환
    //     const majorMinor = `${parts[0]}.${parts[1]}.x`;

    //     return majorMinor;

    // } catch (error) {
    //     logger.error(`[Config] Git 태그를 찾을 수 없습니다, fallback 사용: ${error.message}`);
    //     return '0.0.x';  // fallback 버전
    // }
}

export const config = {
    // 서버 설정
    port: 3002,
    publicDomain: process.env.PUBLIC_DOMAIN || '127.0.0.1',

    // 세션 관리
    session: {
        oauthTimeout: 5 * 60 * 1000,    // 5분
        cleanupInterval: 60 * 1000       // 1분마다 체크
    },

    // Docker 설정
    docker: {
        image: 'dartagnan-gameserver:v2',
        internalPort: 7777,
        // 플랫폼별 로비 URL
        getLobbyUrl() {
            const platform = process.platform;
            if (platform === 'win32' || platform === 'darwin') {
                return `http://host.docker.internal:${config.port}`;
            }
            return `http://172.17.0.1:${config.port}`;
        }
    },

    // 데이터베이스 설정
    database: {
        host: process.env.DB_HOST || 'localhost',
        user: process.env.DB_USER || 'root',
        password: process.env.DB_PASSWORD || '',
        name: process.env.DB_NAME || 'dartagnan'
    },

    // OAuth 설정
    oauth: {
        google: {
            clientId: process.env.GOOGLE_CLIENT_ID,
            clientSecret: process.env.GOOGLE_CLIENT_SECRET
        }
    },

    // 버전 관리
    version: {
        // Git 태그에서 자동으로 로드됨
        // 메이저.마이너만 체크, 패치는 무시 ('x'로 표시)
        // 예: Git 태그 "v1.2.345" → "1.2.x" (1.2.0, 1.2.15, 1.2.345 모두 허용)
        requiredClientVersion: getRequiredClientVersion()
    },

    // 방 상태 상수
    roomState: {
        INITIALIZING: -1,
        WAITING: 0,
        ROUND: 1
    }
};