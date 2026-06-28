# C00 - AdminWeb 분석

## 1. 작업 목적
B10 이후 AdminWeb UI·Client·메뉴·JS 관례를 분석하고 수정 목록만 작성한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- AdminWeb 연동 문서
- UpdateServer 최종 API 계약

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- A05와 B10 완료
- Program DI
- ApiClient
- AuthStateService
- MenuConfiguration·NavMenu
- 화면 패턴
- JS Interop
- 오류 처리

## 4. 수정 허용 범위
- 코드 수정 금지
- 분석 보고서와 C00 상태

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 화면 구현
- API 계약 변경
- 공통 UI 대규모 개편

## 6. 구현 요구사항
- 수정 파일을 접근정보·메뉴·Client·화면·업로드·오류로 분류
- 재사용 요소와 위험 기록

## 7. 오류 처리 및 안정성
- 추정 이름 확정 금지

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 코드 변경 없음
- C01~C04 목록 확정
- 빌드 상태 기록

## 10. 완료 보고 형식
```text
작업 결과
- Completed / Blocked / Failed

변경 파일
- 경로: 변경 목적

검증 결과
- 실행 명령:
- 결과:

남은 문제
- 컴파일 오류:
- 실제 동작 오류:
- 불필요한 중복:
- 다음 단계 선행조건:

정책 이탈 여부
- 없음 / 상세 내용
```

완료 시 `codex/update-server/WORK_STATUS.md`의 해당 단계만 갱신한다.
