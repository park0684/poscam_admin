# A03 - 현재 사용자 접근정보 API

## 1. 작업 목적
`GET /api/accounts/me/access`를 추가해 자신의 현재 역할과 권한을 반환한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- 현재 접근정보 계약
- A02 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- A02 Completed
- 적합 Controller
- AccountService
- 권한 Repository
- HTTP 처리 관례

## 4. 수정 허용 범위
- DTO
- Controller
- Service
- Repository 최소 추가
- 테스트

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 다른 사용자 코드 입력
- AdminPermissionManage 요구
- AdminWeb 수정
- 내부 API 선행

## 6. 구현 요구사항
- Bearer 검증
- 현재 사용자 DB 재조회
- System 역할
- Admin 현재 권한
- PartnerUser 빈 목록
- 필드 UserCode/UserName/UserRole/PermissionCodes

## 7. 오류 처리 및 안정성
- 토큰·비활성 계정 401
- DB 오류 공통 처리

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 역할·토큰 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 자기 정보만 조회
- 별도 권한 불필요
- 계약 일치
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
