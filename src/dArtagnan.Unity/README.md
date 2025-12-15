# dArtagnan.Unity
달타냥의 클라이언트 코드입니다.

## 핵심 코드
- [GameModel.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/GameModel.cs): 게임의 전역 상태를 관리하는 컴포넌트
- [NetworkManager.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Networking/NetworkManager.cs): 게임 서버와의 TCP 연결을 관리하는 컴포넌트
- [PacketChannel.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/PacketChannel.cs): TCP 연결에서 받은 패킷을 옵저버 패턴으로 간편하게 구독해줄 수 있게 하는 클래스
- [WebsocketManager.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Networking/WebsocketManager.cs): 로비 서버와의 웹소켓 연결을 관리하는 컴포넌트
- [PlayerModel.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/Player/Components/PlayerModel.cs): 플레이어의 정보를 담은 컴포넌트
- [PlayerPhysics.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/Player/Components/PlayerPhysics.cs): 플레이어의 물리 이동을 관장하는 컴포넌트
- [MapManager.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Editor/MapManager.cs): 타일맵을 JSON으로 변환하는 커스텀 에디터 스크립트

## 로직
1. [GameModel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/GameModel.cs)이 [PacketChannel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/PacketChannel.cs)을 구독
1. [NetworkManager](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Networking/NetworkManager.cs)가 게임 서버와의 TCP 소켓을 열고, 패킷이 들어올 때마다 [PacketChannel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/PacketChannel.cs)에 송신
1. [GameModel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/GameModel.cs)은 [PacketChannel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/PacketChannel.cs)에서 패킷을 받아와서 [PlayerModel](src/dArtagnan.Unity/Assets/Scripts/Game/Player/Components/PlayerModel.cs)을 생성
1. [PacketChannel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/PacketChannel.cs)에서 패킷이 들어올 때마다 [GameModel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/GameModel.cs)이 그에 맞는 함수를 호출하여 [PlayerModel](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Game/Player/Components/PlayerModel.cs) 간의 상호작용을 처리
1. [WebsocketManager](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Unity/Assets/Scripts/Networking/WebsocketManager.cs)는 Websocket 연결을 구독했다가 이벤트가 들어오면 그에 맞는 함수 호출