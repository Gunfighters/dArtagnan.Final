import 'dotenv/config';
import express from 'express';
import http from 'http';
import { config } from './src/config.js';
import { logger } from './src/logger.js';
import { initDatabase } from './src/database.js';
import { setupRoutes } from './src/api/routes.js';
import { createWebSocketServer } from './src/websocket/server.js';

// Express 앱 생성
const app = express();
const httpServer = http.createServer(app);

// 미들웨어 설정
app.use(express.json());

// HTTP 라우트 설정
setupRoutes(app);

// WebSocket 서버 생성
createWebSocketServer(httpServer);

// 서버 시작
async function startServer() {
    try {
        // 데이터베이스 초기화
        await initDatabase();

        // HTTP 서버 시작
        httpServer.listen(config.port, () => {
            logger.info(`🚀 로비 서버 시작`);
            logger.info(`   포트: ${config.port}`);
            logger.info(`   주소: ${config.publicDomain}`);
            logger.info(`   버전: ${config.version.requiredClientVersion}`);
        });
    } catch (error) {
        logger.error('서버 시작 실패:', error);
        process.exit(1);
    }
}

startServer();