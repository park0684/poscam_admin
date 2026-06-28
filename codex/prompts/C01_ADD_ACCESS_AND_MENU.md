# C01 - 접근정보와 메뉴

## 1. 작업 목적
현재 접근정보 서비스와 UpdateManage=12 메뉴 필터를 추가한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- AdminWeb 연동 문서
- C00 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- C00 Completed
- AuthStateService
- ApiClient
- MenuItem
- NavMenu 생명주기

## 4. 수정 허용 범위
- 접근 DTO·서비스
- MenuItem 권한 필드
- MenuConfiguration
- NavMenu 필터
- DI

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 릴리스 화면
- 권한 sessionStorage 영구 저장
- 메뉴를 보안 경계로 간주
- 기존 메뉴 재설계

## 6. 구현 요구사항
- System 전체 허용
- Admin 12 확인
- PartnerUser 거부
- 빈 부모 숨김
- 업데이트관리/릴리스 관리/감사 로그
- Scoped 캐시와 무효화

## 7. 오류 처리 및 안정성
- 조회 실패 시 업데이트 메뉴 숨김
- 401·403 구분

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 메뉴 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 역할별 메뉴 정확
- 빈 그룹 없음
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
