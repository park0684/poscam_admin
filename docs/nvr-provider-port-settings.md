# NVR Provider 및 포트 설정 확장

## 목적

CamViewer의 NVR 설정 구조 변경에 맞춰 AuthServer가 다음 값을 저장하고 반환하도록 확장한다.

- 제조사/Provider: 고정 정수 코드
- 제어/API 포트: 기존 `nvr_port`
- RTSP 포트: 신규 `nvr_rtsp_port`

TP-Link VIGI는 클라우드 계정 연동을 사용하지 않는다. 매장에 등록된 특정 NVR의 로컬 OpenAPI와 RTSP 직접 연결만 지원 대상으로 한다.

## Provider 고정 코드

| 값 | Provider | 연결 방식 |
|---:|---|---|
| 0 | Unknown | 미지정 |
| 1 | Dahua | NetSDK 직접 연결 |
| 2 | TP-Link VIGI | 로컬 OpenAPI + RTSP |
| 3 | KT Telecop | 제조사 SDK |

한 번 배정한 코드는 변경하거나 다른 제조사에 재사용하지 않는다.
UI의 `SelectedIndex`가 아니라 명시적인 Enum 값을 저장해야 한다.

## DB 변경

적용 파일:

```text
AuthServer/Database/Migrations/20260713_add_nvr_provider_and_rtsp_port.sql
```

추가 컬럼:

```text
nvr_provider  INT NOT NULL DEFAULT 1
nvr_rtsp_port INT NOT NULL DEFAULT 554
```

기존 `nvr_port`는 삭제하거나 이름을 변경하지 않는다.

```text
Dahua        nvr_port = NetSDK 포트
TP-Link VIGI nvr_port = 로컬 OpenAPI HTTPS 포트
```

## Config API 계약

`NvrConfigDto` JSON 예시:

```json
{
  "nvrProvider": 2,
  "nvrId": "admin",
  "nvrPassword": "encrypted-value",
  "nvrIp": "192.168.0.100",
  "nvrPort": 20443,
  "nvrRtspPort": 554,
  "nvrChannels": 16,
  "nvrVersion": "20260713001"
}
```

검증 범위:

- Provider가 정의된 코드인지 확인
- NVR IP 필수
- 제어/API 포트 1~65535
- RTSP 포트 1~65535
- NVR ID 필수
- NVR 비밀번호 필수
- 채널 수 1 이상

## 구버전 CamViewer 호환

AuthServer를 CamViewer보다 먼저 배포할 수 있도록 다음 값을 보정한다.

```text
NvrProvider = 0 또는 누락 → Dahua(1)
NvrRtspPort = 0 또는 누락 → 554
```

신규 CamViewer는 Provider와 RTSP 포트를 항상 명시적으로 전송한다.

## 보안 및 계약 격리 정책

AuthServer에는 매장 NVR 로컬 접속 정보만 저장한다.

저장하지 않는 값:

- TP-Link 클라우드 계정
- TP-Link 클라우드 비밀번호
- Access Token / Refresh Token
- VMS ID / Site ID / Cloud Device ID
- Relay Token / Relay Session ID

하나의 CamViewer는 인증 토큰에 연결된 매장의 NVR 설정만 내려받는다. 다른 매장이나 다른 계약의 NVR을 선택하거나 조회하는 기능을 제공하지 않는다.

## 배포 순서

1. 운영 DB 백업
2. `20260713_add_nvr_provider_and_rtsp_port.sql` 실행
3. 기존 행이 `nvr_provider=1`, `nvr_rtsp_port=554`인지 확인
4. AuthServer 배포
5. `/api/config/latest` 응답에 `nvrProvider`, `nvrRtspPort`가 포함되는지 확인
6. 기존 Dahua CamViewer 설정 업로드/다운로드 확인
7. 신규 CamViewer 배포
8. TP-Link VIGI 설정 동기화 확인

DB 마이그레이션보다 AuthServer를 먼저 배포하면 Repository 조회 시 존재하지 않는 컬럼 오류가 발생하므로 순서를 바꾸면 안 된다.

## 확인 항목

- 기존 Dahua 데이터 조회 시 Provider 1, RTSP 554 반환
- 구버전 CamViewer 동기화 시 Dahua/554 자동 보정
- TP-Link 설정 저장 시 Provider 2 유지
- 사용자 지정 RTSP 포트 저장 후 재조회 시 동일 값 반환
- 관리자 화면에서 제조사, 제어/API 포트, RTSP 포트 조회
- 비밀번호가 로그나 관리자 응답에 노출되지 않음
