# UpdateServer 내부 권한 확인 API

```http
POST /api/internal/update-management/authorize
Authorization: Bearer {accountToken}
X-POSCAM-Service-Key: {internalServiceKey}
```

Body는 사용하지 않는다.

처리:
1. 서비스 키
2. Bearer 토큰
3. 현재 사용자 재조회
4. Active 확인
5. System 자동 허용
6. Admin은 UpdateManage=12
7. PartnerUser 거부

성공 Data: `userCode`, `userName`, `userRole`.

금지:
- PermissionCode 입력
- 범용 권한 API
- TokenSecret 공유
- 외부 Nginx 노출
