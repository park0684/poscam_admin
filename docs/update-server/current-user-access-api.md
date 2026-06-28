# 현재 사용자 접근정보 API

```http
GET /api/accounts/me/access
Authorization: Bearer {accountToken}
```

```json
{
  "success": true,
  "message": "현재 사용자 접근정보를 조회했습니다.",
  "errorCode": 0,
  "data": {
    "userCode": 15,
    "userName": "운영 관리자",
    "userRole": 1,
    "permissionCodes": [7, 10, 11, 12]
  }
}
```

- System: 역할 0
- Admin: 현재 DB 권한 목록
- PartnerUser: 역할 2, 빈 목록
- 자신의 정보이므로 `AdminPermissionManage`를 요구하지 않는다.
- 비활성 계정과 잘못된 토큰은 401.
