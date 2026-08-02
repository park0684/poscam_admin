# CamViewer 토큰 자동 갱신 검증

## 자동 테스트

```bash
dotnet test AuthServer.Tests/poscam.AuthServer.Tests.csproj --configuration Release
```

검증 항목:

- 일반 토큰 검증은 만료 토큰을 `TokenExpired`로 거부
- Viewer 갱신 검증은 `OfflineUntil` 이내의 만료 토큰 Payload 복원
- `OfflineUntil` 경과 시 `OfflineExpired` 반환
- 서명 위변조 토큰 거부
- Viewer 계약 유형과 관계없이 7일 오프라인 기간 발급
- PC CAM 오프라인 기간은 기존 설정 유지

## 통합 테스트

1. 정상 Viewer 로그인으로 토큰을 발급한다.
2. 테스트 환경에서 `TokenExpireHours`를 짧게 설정하거나 만료 토큰을 준비한다.
3. `OfflineUntil` 이내에 `/api/viewer/verify-token`을 호출한다.
4. 응답 `Success=true`, `IsValid=true`, 새 `Token` 존재를 확인한다.
5. 새 토큰의 `ExpiresAt`과 `OfflineUntil`이 현재 시각 기준으로 연장됐는지 확인한다.
6. 동일 요청에서 HWID를 바꾸면 갱신이 차단되는지 확인한다.
7. 장비를 해제한 뒤 기존 토큰으로 호출하면 `DeviceNotFound`가 반환되는지 확인한다.
8. 계약을 비활성 또는 만료 상태로 바꾼 뒤 갱신이 차단되는지 확인한다.
9. `OfflineUntil`이 지난 토큰은 `OfflineExpired`로 차단되는지 확인한다.
10. CamViewer에서 새 응답 토큰이 `viewer_token.dat`에 저장되고 다음 실행 때 로그인 화면이 나타나지 않는지 확인한다.
