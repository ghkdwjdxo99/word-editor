# 프로젝트 문서 및 폴더 관리 규칙

## 폴더 구조

```text
워드 편집기/
├─ SimpleWordEditor/          프로그램 소스 프로젝트
├─ SimpleWordEditor.Tests/    자동 회귀 테스트 프로젝트
├─ docs/
│  ├─ plans/                  버전별 개발 기획서
│  ├─ test-reports/           버전별 테스트 결과
│  ├─ project/                버전 이력과 프로젝트 정책
│  └─ workflow/               AI 역할과 버전별 작업·인수인계 메시지
├─ README.md                  프로젝트 소개와 실행 방법
├─ SimpleWordEditor.slnx      솔루션 진입점
├─ NuGet.Config               패키지 소스 설정
└─ .gitignore                 Git 제외 규칙
```

빌드 중 생성되는 `bin/`, `obj/`, `.localappdata/`, `dist/`, `artifacts/`와 EXE, DLL, ZIP, 설치 프로그램은 Git에서 제외한다. 최종 설치 파일과 체크섬은 배포 승인 후 GitHub Release 자산으로만 관리한다.

## 파일 배치 규칙

- 개발 기획서는 `docs/plans/개발_기획서_vX.Y.Z.md`에 둔다.
- 패치도 별도 파일명 체계를 만들지 않고 해당 버전 개발 기획서에 누적한다.
- 테스트 결과는 `docs/test-reports/테스트_결과_vX.Y.Z.md`에 둔다.
- 버전과 패치 상태는 `docs/project/버전_이력.md`에 누적한다.
- 역할, 승인 절차, 인수인계 규칙은 `docs/workflow/`에 둔다.
- 역할별 작업 요청은 `docs/workflow/handoffs/vX.Y.Z/`에 둔다.
- 배포 정책은 `docs/project/배포_정책.md`에 둔다.
- 프로그램 코드는 `SimpleWordEditor/`, 테스트 코드는 `SimpleWordEditor.Tests/` 밖에 두지 않는다.
- 루트에는 프로젝트 실행과 빌드에 직접 필요한 파일만 둔다.
- 동일 내용을 가진 요약 문서를 여러 폴더에 복제하지 않고 링크로 참조한다.

## 버전 문서 생성 순서

1. 기획자가 `docs/plans/개발_기획서_vX.Y.Z.md`를 만든다.
2. 기획자가 `docs/project/버전_이력.md`에 예정 버전을 등록한다.
3. 개발자가 패치 ID 단위로 구현하고 버전 이력을 갱신한다.
4. 테스터가 `docs/test-reports/테스트_결과_vX.Y.Z.md`를 작성한다.
5. 기획자가 완료 조건을 대조해 승인 또는 재작업을 결정한다.
6. Git 관리자가 승인된 상태만 커밋·태그·배포 브랜치로 관리한다.

## 문서 상태 규칙

모든 버전 문서는 `개발 예정`, `개발 중`, `검증 중`, `배포 승인`, `배포 완료`, `보류` 중 하나의 상태를 표시한다. 실제 검증 전에는 “완료”로 기록하지 않는다.
