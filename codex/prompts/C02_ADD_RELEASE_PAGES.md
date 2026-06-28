# C02 - 릴리스 관리 화면

## 1. 작업 목적
UpdateApiClient와 릴리스 목록·등록·상세 화면을 구현한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- UpdateServer API 계약
- C01 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- C01 Completed
- endpoint·DTO
- 기존 화면 패턴
- HttpClient 등록

## 4. 수정 허용 범위
- UpdateApiSettings
- UpdateApiClient
- DTO
- 목록·신규·상세
- 권한 Guard
- 스타일

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- Artifact 업로드
- UpdateServer 프로젝트 참조
- 기존 ApiClient BaseAddress 변경
- Published 수정 UI

## 6. 구현 요구사항
- Internal/Public URL 분리
- 필터·페이징
- Draft 등록·수정
- 상태별 읽기전용
- 게시·중지 확인

## 7. 오류 처리 및 안정성
- 401·403·404·409·503 처리

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- Client·컴포넌트 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- Client 독립
- 상태별 UI 정확
- 직접 URL Guard
- 빌드 성공

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
