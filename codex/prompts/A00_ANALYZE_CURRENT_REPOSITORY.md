# A00 - 현재 저장소 전체 분석

## 1. 작업 목적
AuthServer와 AdminWeb의 실제 구조를 기준으로 UpdateServer 연동에 필요한 문제 목록만 작성한다. 코드는 수정하지 않는다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- 최종 DECISIONS 문서

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- 솔루션 구조와 Framework
- AccountTokenService 전체
- AccountService 로그인 사용자 조회
- AdminPermissionService·Enum·Repository
- ApiResponse·AuthErrorCode
- AdminWeb ApiClient·AuthStateService·메뉴·권한 UI
- Secret 노출
- 현재 Release 빌드

## 4. 수정 허용 범위
- 코드 수정 금지
- 분석 보고서와 A00 상태만 수정

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- JWT 전환
- 임시 클래스 생성
- A01 이후 기능 선행 구현
- 추정만으로 결함 확정

## 6. 구현 요구사항
- 컴파일 오류, 실제 동작 오류, 보안 선행 수정, 연동 필수 수정, 범위 밖 개선으로 분류
- 각 항목에 정확한 경로와 근거
- A01 수정 목록 확정

## 7. 오류 처리 및 안정성
- 불확실한 사항은 확인 필요로 기록

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`로 현재 상태 기록

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 코드 변경 없음
- A01 선행 목록 작성
- 현재 빌드 결과 기록

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
