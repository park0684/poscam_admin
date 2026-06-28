# AuthServer 연동 개요

관리자 AccountToken은 JWT가 아닌 2-part HMAC 사용자 정의 토큰이다. UpdateServer는 형식과 Secret을 복제하지 않는다.

```text
AdminWeb → UpdateServer 관리자 API
→ AuthServer 내부 권한 API
→ 토큰 검증 → 사용자 현재 상태 → 역할 → UpdateManage=12
```

AuthServer 장애 시 관리자 API는 503이지만 공개 Update Check와 기존 package 다운로드는 계속 동작한다.
