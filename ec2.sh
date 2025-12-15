#!/bin/bash
# 배포용 ec2 스크립트
echo "=== D'Artagnan Server (EC2) ==="
echo

# Docker 이미지 빌드
echo "[1/2] Building Docker image..."
docker build -t dartagnan-gameserver:v2 -f Dockerfile.server .
if [ $? -ne 0 ]; then
    echo "Failed to build Docker image"
    exit 1
fi

# 로비 서버 시작
echo "[2/2] Starting lobby server..."
cd src/dArtagnan.Lobby
if [ ! -d "node_modules" ]; then
    echo "Installing dependencies..."
    npm install
fi

# EC2에서는 도메인 사용 (systemd 환경변수 필수)
if [ -z "$PUBLIC_DOMAIN" ]; then
    echo "ERROR: PUBLIC_DOMAIN 환경변수가 설정되지 않았습니다."
    echo "systemd 서비스 파일에 Environment=\"PUBLIC_DOMAIN=your.domain\" 추가 필요"
    exit 1
fi

echo
echo "=== Server started ==="
echo "Lobby: http://${PUBLIC_DOMAIN}"
echo

node server.js