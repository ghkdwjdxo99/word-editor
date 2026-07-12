# 간단 워드 편집기

Windows용 단일 창 DOCX 편집기입니다. 외부 문서 처리 패키지 없이 .NET/WPF 기본 API만 사용합니다.

## 실행

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

## Windows 실행 파일

`dist/SimpleWordEditor-v1.0.0-preview.1-win-x64.zip`을 내려받아 압축을 푼 뒤
`SimpleWordEditor.exe`를 실행하세요. Windows x64용 자체 포함 배포본이므로 .NET을
별도로 설치할 필요가 없습니다. 압축 파일에 포함된 DLL은 실행 파일과 같은 폴더에
두어야 합니다.

## 프로젝트 문서

- 개발 기획: `docs/plans/`
- 테스트 결과: `docs/test-reports/`
- 버전 이력: `docs/project/버전_이력.md`
- AI 역할 및 인수인계: `docs/workflow/AI_역할_및_작업_절차.md`
- 폴더 관리 규칙: `docs/README.md`
