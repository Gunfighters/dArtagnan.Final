import { WebSocketServer } from 'ws';
import { logger } from '../logger.js';
import { handlers, handleDisconnection } from './handlers.js';
import { cleanupPendingRequests } from '../rooms/manager.js';
import { getActiveSession } from '../auth/session.js';

/**
 * WebSocket 서버 생성
 */
export function createWebSocketServer(httpServer) {
    const wss = new WebSocketServer({
        server: httpServer,
        clientTracking: true
    });

    wss.on('connection', (ws, req) => {
        handleConnection(ws, req);
    });

    // Heartbeat: 30초마다 모든 클라이언트에게 ping 전송
    const heartbeatInterval = setInterval(() => {
        wss.clients.forEach((ws) => {
            if (ws.isAlive === false) {
                logger.info('[WS][Heartbeat] 클라이언트 응답 없음, 연결 종료');
                return ws.terminate();
            }

            ws.isAlive = false;
            ws.ping();
        });
    }, 30000);

    wss.on('close', () => {
        clearInterval(heartbeatInterval);
    });

}

/**
 * 새 WebSocket 연결 처리
 */
function handleConnection(ws, req) {
    // 클라이언트 IP 추출 (프록시 환경 고려)
    const clientIp = req.headers['x-forwarded-for']?.split(',')[0].trim() ||
                     req.socket.remoteAddress;

    logger.info(`[WS][Guest] 새 연결 수립: IP=${clientIp}`);

    // ws 객체에 직접 인증 상태 저장
    ws.authenticated = false;
    ws.providerId = null;
    ws.isAlive = true;

    // Heartbeat: pong 응답 수신 시 isAlive 갱신
    ws.on('pong', () => {
        ws.isAlive = true;
    });

    ws.on('message', async (message) => {
        try {
            const data = JSON.parse(message.toString());

            // 로그용 닉네임 조회
            let nickname = 'Guest';
            if (ws.authenticated && ws.providerId) {
                const session = getActiveSession(ws.providerId);
                nickname = session?.nickname || 'Unknown';
            }

            logger.info(`[WS][${nickname}] 메시지 수신: ${data.type}`);

            // 인증되지 않은 상태에서는 auth만 허용
            if (!ws.authenticated && data.type !== 'auth') {
                sendError(ws, 'SESSION_INVALID', '로그인 토큰이 필요.');
                return;
            }

            // 메시지 핸들러 실행
            const handler = handlers[data.type];
            if (handler) {
                await handler(ws, data);
            } else {
                sendError(ws, 'UNKNOWN_MESSAGE_TYPE', '알 수 없는 메시지 타입입니다.');
            }

        } catch (error) {
            let nickname = 'Guest';
            if (ws.authenticated && ws.providerId) {
                const session = getActiveSession(ws.providerId);
                nickname = session?.nickname || 'Unknown';
            }
            logger.error(`[WS][${nickname}] 메시지 처리 오류:`, error);
            sendError(ws, 'INVALID_REQUEST', '잘못된 요청입니다.');
        }
    });

    ws.on('close', () => {
        handleDisconnection(ws, 'close');
        cleanupPendingRequests(ws);
    });

    ws.on('error', (err) => {
        handleDisconnection(ws, 'error', err);
        cleanupPendingRequests(ws);
    });
}

/**
 * 에러 메시지 전송
 * @param {WebSocket} ws - WebSocket 연결
 * @param {string} errorType - 에러 타입 (LobbyProtocol.cs의 ErrorType 참조)
 * @param {string} message - 에러 메시지
 */
function sendError(ws, errorType, message) {
    if (ws.readyState === ws.OPEN) {
        ws.send(JSON.stringify({ type: 'error', errorType, message }));
    }
}