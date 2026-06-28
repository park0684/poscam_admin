# 운영 Secret 정리

추적 파일에는 MariaDB 접속정보, TokenSecret, 내부 서비스 키의 실제 값을 저장하지 않는다.
`appsettings.json`에는 실행 가능한 자격정보 대신 placeholder만 두고, 환경별 실제 값은 배포 환경변수로 주입한다.

환경변수 이름:
```text
ConnectionStrings__DefaultConnection
AuthPolicy__TokenSecret
AuthPolicy__InternalServiceKey
```

- 환경변수 값은 소스, 설정 예제, 로그에 기록하지 않는다.
- 내부 서비스 키는 `AuthPolicy__InternalServiceKey` 환경변수로 바인딩한다.
- TokenSecret 회전은 기존 관리자 토큰을 무효화한다. 운영 회전과 실제 값 주입은 사람이 수행하며 Codex는 placeholder와 문서만 정리한다.
