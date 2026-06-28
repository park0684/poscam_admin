# A01 - 보안 설정과 토큰 오류 정리

## 1. 작업 목적
실제 Secret 노출을 제거할 설정 구조를 만들고 토큰 만료 오류와 역할 문서를 수정한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- 운영 Secret 문서
- A00 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- A00 Completed
- appsettings 바인딩
- 토큰 만료 처리
- payload 역할 주석

## 4. 수정 허용 범위
- 설정 예제·문서
- AccountTokenService 만료 처리
- 역할 주석
- 관련 테스트

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- 운영 Secret 실제 변경
- Token 형식 변경
- 로그인 응답 변경
- 서버 배포

## 6. 구현 요구사항
- 실제 값 placeholder 교체
- 환경변수 문서화
- TokenExpired=5003
- System=0/Admin=1/PartnerUser=2 설명
- 회귀 테스트

## 7. 오류 처리 및 안정성
- Secret을 로그에 포함하지 않음

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 관련 테스트

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 실제 Secret 없음
- 만료 코드 5003
- 정상 토큰 유지
- Release 빌드 성공

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
