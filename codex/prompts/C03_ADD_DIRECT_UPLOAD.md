# C03 - 브라우저 직접 업로드

## 1. 작업 목적
ZIP을 AdminWeb 서버를 거치지 않고 브라우저에서 UpdateServer로 전송한다.

## 2. 반드시 먼저 읽을 문서
- 루트 또는 병합된 `AGENTS.md`
- `docs/update-server` 관련 문서
- `codex/update-server/WORK_STATUS.md`
- 현재 저장소의 실제 대상 파일 전체
- Artifact upload 계약
- C02 결과

문서와 현재 코드가 충돌하면 임의 구현하지 말고 `Blocked`로 보고한다.

## 3. 작업 전 현재 상태 확인
- C02 Completed
- JS 로딩 방식
- 토큰 조회
- PublicBaseUrl
- multipart 필드

## 4. 수정 허용 범위
- updateUpload.js
- 업로드 컴포넌트
- Interop DTO
- 정적 참조

목록 밖 파일이 반드시 필요하면 이유와 경로를 먼저 보고한다.

## 5. 금지 사항
- IBrowserFile 서버 중계
- AdminWeb 임시 파일
- AllowAnyOrigin
- 토큰 Query String

## 6. 구현 요구사항
- XMLHttpRequest
- FormData
- Bearer Header
- os/architecture/packageType/file
- 진행률·취소
- 중복·게시 차단
- 이탈 경고
- 성공 후 재조회

## 7. 오류 처리 및 안정성
- 401·403·409·413·415·500·503 처리
- 취소와 네트워크 오류 구분

## 8. 빌드 및 테스트
- `dotnet build poscam.sln -c Release`
- 브라우저 Network 검증

실패 상태에서 다음 단계로 이동하지 않는다.

## 9. 완료 조건
- 파일 바이트 미중계
- 진행률·취소
- Header 토큰
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
