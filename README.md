# 간단 워드 편집기

Windows용 단일 창 DOCX 편집기입니다. 외부 문서 처리 패키지 없이 .NET/WPF 기본 API만 사용합니다.

## 일반 사용자: 다운로드 및 실행

사용자는 저장소의 `bin` 폴더나 소스 코드에 있는 파일을 실행하지 않습니다. 아래 배포
ZIP 안에 들어 있는 `SimpleWordEditor.exe`를 사용합니다.

1. [Windows x64 실행본 ZIP 다운로드](https://github.com/ghkdwjdxo99/word-editor/raw/refs/heads/main/dist/SimpleWordEditor-v1.0.0-preview.1-win-x64.zip)를 누릅니다.
2. 내려받은 `SimpleWordEditor-v1.0.0-preview.1-win-x64.zip`을 원하는 폴더에 완전히 압축 해제합니다.
3. 압축을 푼 폴더의 `SimpleWordEditor.exe`를 실행합니다.
4. 같은 폴더의 DLL 파일은 삭제하거나 다른 곳으로 옮기지 않습니다.

이 배포본은 64비트 Windows용 자체 포함 실행본이므로 .NET SDK나 .NET Desktop Runtime을
별도로 설치할 필요가 없습니다. Windows가 처음 실행할 때 보호 경고를 표시하면 파일의
출처가 이 저장소인지 확인한 후 실행 여부를 결정하세요.

현재 배포 ZIP의 SHA-256은 다음과 같습니다.

```text
BDAF05917DA15D4532D87EFBE44E6252E9E1EF28E6AB734E72E9DF1715C4EFBC
```

## 개발자: 소스에서 실행

.NET 10 SDK가 설치된 개발 환경에서는 다음 명령으로 실행합니다.

```powershell
dotnet run --project SimpleWordEditor\SimpleWordEditor.csproj
```

## 빌드 및 검증

사용자 전역 NuGet 설정에 접근할 수 없는 환경에서는 로컬 `APPDATA`를 지정합니다.

```powershell
$env:APPDATA = (Resolve-Path .localappdata).Path
$env:DOTNET_CLI_HOME = $env:APPDATA
dotnet build SimpleWordEditor.slnx --configfile NuGet.Config
dotnet run --project SimpleWordEditor.Tests\SimpleWordEditor.Tests.csproj --no-build
```

## 지원 범위

- DOCX 새 문서, 열기, 저장, 다른 이름으로 저장
- 굵게, 기울임, 밑줄, 8~72pt, 문단 좌/중앙/우 정렬
- 실행 취소/다시 실행, 잘라내기/복사/붙여넣기
- 안전 저장과 미저장 변경 확인
- Word 설치 환경에서 COM을 통한 DOC 열기/저장

표, 이미지, 머리글, 각주 등은 보존하지 않으며 감지 시 저장 전에 경고합니다.

## 프로젝트 문서

- 개발 기획: `docs/plans/`
- 테스트 결과: `docs/test-reports/`
- 버전 이력: `docs/project/버전_이력.md`
- AI 역할 및 인수인계: `docs/workflow/AI_역할_및_작업_절차.md`
- 폴더 관리 규칙: `docs/README.md`
