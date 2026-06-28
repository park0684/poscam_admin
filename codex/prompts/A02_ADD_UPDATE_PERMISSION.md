# A02 - UpdateManage 권한

## 1. 작업 목적
`UpdateManage=12`를 기존 권한 체계와 AdminWeb 권한 UI에 추가한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- A01 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- A01 Completed
- Enum 실제 마지막 값
- AdminWeb 권한 목록
- 권한 검증 방식

## 4. 수정 허용 범위
- AdminPermissionType
- AdminWeb 권한 목록
- 관련 테스트

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 기존 번호 변경
- 별도 조회·게시 권한
- API·메뉴 구현

## 6. 구현 요구사항
- UpdateManage=12
- 표시명 업데이트 관리
- 설명 문구 적용
- 기존 System/Admin 정책 유지

## 7. 오류 처리 및 안정성
- 중복 숫자와 저장 호환 확인

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 권한 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 12번 양쪽 반영
- 1~11 유지
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
