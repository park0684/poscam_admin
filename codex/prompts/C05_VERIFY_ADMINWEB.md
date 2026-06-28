# C05 - AdminWeb 최종 검증

## 1. 작업 목적
C01~C04를 전체 검증하고 기존 기능 회귀를 확인한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- C01~C04 보고
- UpdateServer 계약

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- C01~C04 Completed
- 전체 diff
- 기존 메뉴·Client
- 권한별 메뉴
- 상태별 화면
- 업로드 Network

## 4. 수정 허용 범위
- 확인된 결함 수정
- 검증 테스트
- WORK_STATUS

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 새 기능
- 기존 메뉴 재구성
- 계약 변경

## 6. 구현 요구사항
- 네 역할·권한 시나리오
- 기존 AuthServer API
- 직접 URL Guard
- 업로드 미중계
- 주요 오류 처리

## 7. 오류 처리 및 안정성
- 확인된 결함만 수정

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 전체 테스트
- 브라우저 체크리스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 회귀 없음
- 권한 정확
- 업로드 정책 일치
- 빌드·테스트 성공

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
