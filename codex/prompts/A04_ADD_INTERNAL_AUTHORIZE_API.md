# A04 - 내부 업데이트 권한 API

## 1. 작업 목적
UpdateServer 전용 현재 권한 확인 API를 추가한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- 내부 권한 API 계약
- A03 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- A03 Completed
- AuthPolicy
- AccountService
- AdminPermissionService
- Swagger 정책

## 4. 수정 허용 범위
- 서비스 키 설정
- Controller
- 응답 DTO
- 고정시간 비교 Helper
- 테스트

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- PermissionCode 입력
- 범용 권한 API
- UpdateServer 코드
- TokenSecret 공유
- PartnerUser 허용

## 6. 구현 요구사항
- POST endpoint
- 서비스 키와 Bearer 검증
- 현재 사용자 재조회
- System 허용
- Admin UpdateManage 확인
- Actor 반환

## 7. 오류 처리 및 안정성
- 잘못된 키·토큰 401
- 권한 없음 403
- 민감정보 비노출

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 키·역할·권한·토큰 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- UpdateManage 고정
- 역할별 결과 정확
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
