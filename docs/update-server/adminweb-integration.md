# AdminWeb 연동

- 기존 `ApiClient`: AuthServer 전용
- 신규 `UpdateApiClient`: UpdateServer JSON API
- 기존 AuthStateService 토큰 재사용

```json
{
  "UpdateApiSettings": {
    "InternalBaseUrl": "http://poscam-update-api:8080",
    "PublicBaseUrl": "https://update.poscam.co.kr"
  }
}
```

System 또는 UpdateManage=12 Admin에게만 `업데이트관리` 메뉴를 표시한다.

하위 메뉴:
- 릴리스 관리
- 감사 로그

1GB ZIP은 Blazor Server를 거치지 않고 JavaScript `XMLHttpRequest`가 UpdateServer 공개 URL로 직접 전송한다.

오류:
- 401 로그인 제거
- 403 로그인 유지, 권한 없음 및 접근 캐시 무효화
- 409 충돌
- 413 크기 초과
- 503 AuthServer 관리자 인증 장애
