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

### ZIP으로 배포하는 이유

프로그램은 `SimpleWordEditor.exe` 외에도 WPF 실행에 필요한 네이티브 DLL 파일을 함께
사용합니다. EXE 하나만 올리면 약 131MB로 GitHub의 단일 파일 제한을 넘고, DLL을 따로
받게 하면 누락하기 쉽습니다. 따라서 실행에 필요한 파일을 하나의 ZIP으로 묶습니다.
ZIP은 설치 프로그램이 아니며, 압축을 풀고 EXE를 실행하는 휴대용 배포본입니다.

## 저장소 폴더 구조

```text
word-editor/
├─ dist/                    일반 사용자가 받는 배포 ZIP
├─ docs/                    기획서, 버전 이력, 테스트 결과, 작업 규칙
├─ SimpleWordEditor/        실제 워드 편집기 프로그램 소스 코드
├─ SimpleWordEditor.Tests/  프로그램 기능을 검증하는 자동 회귀 테스트 코드
├─ SimpleWordEditor.slnx    두 프로젝트를 묶는 .NET 솔루션 파일
├─ NuGet.Config             빌드에 사용하는 패키지 소스 설정
├─ .gitignore               Git에 올리지 않을 임시 파일 규칙
└─ README.md                다운로드, 실행 방법과 프로젝트 안내
```

- 일반 사용자는 `dist/`의 ZIP과 이 README만 보면 됩니다.
- 프로그램을 수정하려는 개발자는 `SimpleWordEditor/`와 `SimpleWordEditor.slnx`를 사용합니다.
- 변경 후 정상 동작을 확인하려면 `SimpleWordEditor.Tests/`의 테스트를 실행합니다.
- 개발 과정과 판단 근거를 확인하려면 `docs/`를 봅니다.
- 빌드할 때 생기는 `bin/`, `obj/`, `.localappdata/`, `dist/win-x64/`는 재생성 가능한
  로컬 파일이므로 Git에 올리지 않습니다.

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
