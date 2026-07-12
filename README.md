# 간단 워드 편집기

Windows용 단일 창 DOCX 편집기입니다. 외부 문서 처리 패키지 없이 .NET/WPF 기본 API만 사용합니다.

## 현재 배포 상태

현재 v1.0.0은 개발 및 검증 중이며 일반 사용자용 배포물은 제공하지 않습니다. 저장소에는
소스·테스트·기획서·테스트 문서만 관리하며 EXE, DLL, ZIP, 설치 프로그램은 커밋하지
않습니다. 모든 검증과 배포 승인이 끝난 뒤 설치 EXE를 GitHub Release 자산으로 제공합니다.
그 전에는 `v1.0.0` 태그나 GitHub Release를 만들지 않습니다.

## 저장소 폴더 구조

```text
word-editor/
├─ docs/                    기획서, 버전 이력, 테스트 결과, 작업 규칙
├─ SimpleWordEditor/        실제 워드 편집기 프로그램 소스 코드
├─ SimpleWordEditor.Tests/  프로그램 기능을 검증하는 자동 회귀 테스트 코드
├─ SimpleWordEditor.slnx    두 프로젝트를 묶는 .NET 솔루션 파일
├─ NuGet.Config             빌드에 사용하는 패키지 소스 설정
├─ .gitignore               Git에 올리지 않을 임시 파일 규칙
└─ README.md                다운로드, 실행 방법과 프로젝트 안내
```

- 프로그램을 수정하려는 개발자는 `SimpleWordEditor/`와 `SimpleWordEditor.slnx`를 사용합니다.
- 변경 후 정상 동작을 확인하려면 `SimpleWordEditor.Tests/`의 테스트를 실행합니다.
- 개발 과정과 판단 근거를 확인하려면 `docs/`를 봅니다.
- 빌드할 때 생기는 `bin/`, `obj/`, `.localappdata/`, `dist/`, `artifacts/`와 EXE, DLL,
  ZIP, 설치 프로그램은 Git에 올리지 않습니다.

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

V100-P11 대화상자 자동 확인은 창을 표시할 수 있는 Windows 데스크톱 환경에서 실행합니다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File SimpleWordEditor.Tests\qa_ui_harness.ps1
```

이 UI 하네스 결과는 테스터의 독립 수동 검증을 대체하지 않습니다.

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
- 결함 및 패치 이력: `docs/project/결함_및_패치_이력.md`
- AI 역할 및 인수인계: `docs/workflow/AI_역할_및_작업_절차.md`
- v1.0.0 역할별 작업 메시지: `docs/workflow/handoffs/v1.0.0/`
- 폴더 관리 규칙: `docs/README.md`
- 배포 정책: `docs/project/배포_정책.md`
- 제품 로드맵: `docs/project/제품_로드맵.md`
