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
