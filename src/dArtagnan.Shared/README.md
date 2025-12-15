# dArtagnan.Shared
달타냥 서버와 클라이언트가 공유하는 파일 모음

## 주요 파일
- [GameProtocol.cs](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Shared/src/GameProtocol.cs): 클라이언트와 서버가 공유하는 프로토콜 구조체
- [dArtagnan.Shared.Unity.asmdef](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Shared/dArtagnan.Shared.Unity.asmdef): 클라이언트가 `dArtagnan.Shared` 하위의 C# 코드를 컴파일 시에 참조하기 위한 Assembly Definition
- [package.json](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Shared/package.json): 클라이언트가 `dArtagnan.Shared`를 패키지로서 참조하기 위한 JSON
- [dArtagnan.Shared.csproj](https://github.com/Gunfighters/dArtagnan.Final/blob/108945b654b567504f0d43a1de044413d2f1c7c0/src/dArtagnan.Shared/dArtagnan.Shared.csproj): 서버가 `dArtagnan.Shared`를 참조하는 데, 그리고 클라이언트를 IDE로 열었을 때 `dArtagnan.Shared`의 파일을 에디터에서 찾을 수 있도록 하는 데 필요한 `.csproj` 파일