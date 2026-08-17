# POSCAM 다중 NVR 설정 계약

## 1. 목적

한 매장에 여러 대의 NVR을 등록하고, 각 계산대의 좌/우 영상 채널이 어느 NVR에 속하는지 서버와 CamViewer 전체 구간에서 손실 없이 유지한다.

최종 데이터 흐름은 다음과 같다.

```text
DB
  nvr_configs (store + nvr_no)
  ch_config   (store + pos + screen -> nvr_no + channel)
        ↓
AuthServer Config API
        ↓
CamViewer NvrList / CounterMapList
        ↓
PlayerChannelTarget
        ↓
NvrNo별 Provider / Playback Session
```

같은 계산대의 좌측과 우측이 서로 다른 NVR에 연결되는 구성을 정상 지원해야 한다.

예:

```text
계산대 1 좌측(CCTV) = NVR 1 / CH 3
계산대 1 우측(POS)  = NVR 2 / CH 7
```

## 2. 작업 범위

### 포함

- `nvr_configs`를 매장당 다중 행 구조로 확장
- `ch_config`에 NVR 번호 연결 정보 추가
- 기존 단일 NVR 데이터를 `NvrNo = 1`로 마이그레이션
- AuthServer Entity / Repository / Config API 다중 NVR 지원
- Config 다운로드와 업로드에서 NVR 번호 보존
- CamViewer Config API DTO / Mapper 다중 NVR 지원
- AdminWeb에서 여러 NVR 및 채널의 NVR 번호 조회 표시
- 기존 단일 NVR 매장 호환
- 동일 NVR 좌/우 및 서로 다른 NVR 좌/우에 대한 계약/통합 테스트

### 제외

- NVR Provider 또는 재생 엔진 재설계
- 제조사별 SDK 기능 추가
- V2 다중 NVR의 정밀 시간차 측정 알고리즘 개선
- AdminWeb에서 NVR 추가/수정/삭제 기능 제공
- 운영 DB에 마이그레이션 직접 실행
- 운영 배포

AdminWeb의 기존 정책인 "NVR/채널 수정은 현장 CamViewer에서 수행하고 관리자 페이지는 조회만 수행"을 유지한다.

## 3. 핵심 식별 규칙

### NVR 번호

- `NvrNo`는 매장 내부에서만 유일한 양의 정수이다.
- DB 식별 기준은 `(StoreCode, NvrNo)`이다.
- 기존 데이터는 `NvrNo = 1`로 변환한다.
- 삭제된 NVR 번호를 서버가 임의로 재번호화하지 않는다.
- CamViewer가 보낸 `NvrNo`를 그대로 저장하고 다시 반환한다.

### 채널 매핑

하나의 화면 위치는 다음 조합으로 식별한다.

```text
StoreCode + PosNo + Screen
```

해당 화면이 가리키는 영상은 반드시 다음 두 값을 함께 가진다.

```text
NvrNo + ChannelNo
```

`ChannelNo`만으로 NVR을 추론하지 않는다.

## 4. DB 목표 구조

### nvr_configs

기존 컬럼을 유지하고 다음 컬럼을 추가한다.

```text
nvr_no INT NOT NULL DEFAULT 1
```

기존 `nvr_store` 단일 Primary Key를 다음 복합 Primary Key로 변경한다.

```text
PRIMARY KEY (nvr_store, nvr_no)
```

### ch_config

다음 컬럼을 추가한다.

```text
chn_nvr_no INT NOT NULL DEFAULT 1
```

기존 화면 매핑 식별키 `(chn_store, chn_pos, chn_screen)`는 유지한다. `chn_nvr_no`는 해당 화면이 참조하는 NVR 번호이다.

초기 구현에서는 운영 DB 호환 위험을 줄이기 위해 신규 Foreign Key를 강제로 추가하지 않는다. 대신 서비스 계층에서 동일 매장의 유효한 NVR 번호인지 검증한다.

## 5. Config API 스키마

다중 NVR 계약 버전은 `ConfigSchemaVersion = 2`로 정의한다.

### 신규 응답

```json
{
  "storeCode": 1001,
  "configSchemaVersion": 2,
  "configVersion": "...",
  "nvrs": [
    {
      "nvrNo": 1,
      "nvrProvider": 1,
      "nvrIp": "192.168.0.101",
      "nvrPort": 37777,
      "nvrRtspPort": 554,
      "nvrChannels": 32
    },
    {
      "nvrNo": 2,
      "nvrProvider": 1,
      "nvrIp": "192.168.0.102",
      "nvrPort": 37777,
      "nvrRtspPort": 554,
      "nvrChannels": 16
    }
  ],
  "channels": [
    {
      "posNo": 1,
      "nvrNo": 1,
      "channelNo": 3,
      "screen": 0
    },
    {
      "posNo": 1,
      "nvrNo": 2,
      "channelNo": 7,
      "screen": 1
    }
  ]
}
```

### DTO 필수 변경

`NvrConfigDto`:

```text
NvrNo 추가
```

`ChannelConfigDto`:

```text
NvrNo 추가
```

설정 응답/동기화 요청:

```text
NvrConfig 단일 항목 → Nvrs 목록 지원
ConfigSchemaVersion 추가
```

## 6. 구버전 CamViewer 호환 정책

단순히 `Nvrs` 필드만 추가하면 구버전 CamViewer가 NVR 번호를 무시하고 모든 채널을 NVR 1로 해석할 수 있다. 이는 잘못된 CCTV/POS 영상을 재생할 수 있으므로 허용하지 않는다.

따라서 다음 정책을 적용한다.

- 요청에 `ConfigSchemaVersion`이 없거나 2 미만이면 legacy 클라이언트로 본다.
- 단일 NVR 매장은 legacy 요청을 계속 허용할 수 있다.
- legacy 동기화 요청의 단일 `NvrConfig`와 NVR 번호가 없는 채널은 `NvrNo = 1`로 정규화한다.
- 다중 NVR 매장은 `ConfigSchemaVersion < 2` 클라이언트에 설정을 조용히 단일 NVR로 축약해서 반환하지 않는다.
- 다중 NVR 매장의 legacy 설정 다운로드는 명시적으로 실패시켜 CamViewer 업데이트가 필요함을 알린다.
- Schema 2 응답에서는 전체 `Nvrs`와 각 `Channel.NvrNo`를 반환한다.

전환 기간 동안 단일 NVR 매장의 구버전 호환을 위해 서버 DTO의 legacy `NvrConfig` 필드를 즉시 제거하지 않고 유지할 수 있다. Schema 2 CamViewer는 `Nvrs`를 기준으로 동작한다.

## 7. 설정 버전 정책

현재 `nvr_version` 기반 전체 설정 버전 정책은 유지한다.

- 한 번의 설정 동기화는 NVR 목록과 채널 목록 전체를 하나의 원자적 설정으로 취급한다.
- 동일 매장의 모든 NVR 행에는 같은 `ConfigVersion`을 기록한다.
- 다운로드 시 매장 설정 버전은 NVR 번호가 가장 작은 행의 값에 의존하지 않고, 모든 행이 동일한 버전인지 보장한다.
- 동기화 트랜잭션이 실패하면 NVR/채널 일부만 반영되어서는 안 된다.

초기 구현에서는 별도 설정 버전 테이블을 신설하지 않는다.

## 8. 서버 동기화 정책

Schema 2 설정 업로드는 전체 교체 방식으로 처리한다.

```text
1. 요청 전체 검증
2. Transaction 시작
3. 기존 채널 매핑 삭제
4. 기존 NVR 설정 삭제
5. Nvrs 전체 저장
6. Channels 전체 저장
7. 로그 기록
8. Commit
```

검증 실패 시 DB를 변경하지 않는다.

## 9. 검증 규칙

NVR:

- `NvrNo > 0`
- 한 요청에서 `NvrNo` 중복 금지
- Provider는 정의된 값이며 Unknown 금지
- IP/도메인 필수
- 제어/API 포트 1~65535
- RTSP 포트 1~65535
- ID 필수
- 비밀번호 필수
- 채널 수 1 이상

채널:

- `PosNo > 0`
- `Screen`은 0 또는 1
- `(PosNo, Screen)` 중복 금지
- `NvrNo`가 같은 요청의 `Nvrs`에 존재해야 함
- `ChannelNo > 0`
- 가능하면 `ChannelNo <= 해당 NVR의 NvrChannels` 검증

## 10. 보안 제약

- NVR 비밀번호를 로그에 기록하지 않는다.
- 오류 메시지에 비밀번호를 포함하지 않는다.
- 기존 HTTPS 전송 및 로컬 설정 암호화 정책을 변경하지 않는다.
- TP-Link Cloud 계정/토큰을 추가하지 않는다.
- 매장 인증 토큰으로 다른 매장의 NVR 설정을 조회할 수 없다.

## 11. CamViewer 제약

- 기존 `ViewerConfig.NvrList`, `CounterMap.NvrNo`, `PlayerChannelTarget.NvrNo` 구조를 유지한다.
- 기존 NvrNo별 Provider/Playback Session 그룹화 구조를 재설계하지 않는다.
- API Mapper에서 임의로 `NvrNo = 1`을 할당하지 않는다.
- 설정 저장 후 재생 중인 Provider가 이전 IP/계정 정보를 재사용하지 않도록 재생 정지 및 Runtime/Provider 정리 정책을 검증한다.
- 한쪽 영상만 설정된 계산대도 기존 정책 범위에서 계속 허용한다.

## 12. AdminWeb 제약

AdminWeb은 조회 전용 정책을 유지한다.

표시 항목:

- NVR 번호
- 제조사
- IP
- 제어/API 포트
- RTSP 포트
- 채널 수
- ID
- 비밀번호 저장 여부
- 설정 버전

채널 표에는 다음을 표시한다.

```text
POS 번호 / 화면 / NVR 번호 / 채널 번호
```

## 13. 완료 조건

다음 항목을 모두 만족해야 한다.

1. 기존 단일 NVR 데이터가 NVR 1로 정상 조회된다.
2. NVR 2개 이상을 업로드한 뒤 재다운로드해도 NvrNo가 그대로 유지된다.
3. 계산대 좌측=NVR1, 우측=NVR2 매핑이 서버 왕복 후 유지된다.
4. CamViewer가 해당 매핑으로 서로 다른 NVR Provider/Session을 연다.
5. 같은 NVR의 좌/우 재생도 기존과 동일하게 동작한다.
6. legacy CamViewer가 다중 NVR 설정을 잘못 단일 NVR로 재생하지 못하도록 차단된다.
7. AdminWeb에서 다중 NVR과 채널 소속 NVR을 조회할 수 있다.
8. NVR 비밀번호가 로그/관리자 응답 본문에 평문으로 노출되지 않는다.
9. AuthServer 테스트와 CamViewer 빌드가 통과한다.

## 14. 구현 순서

```text
Phase 1  DB migration + Entity/DTO 계약
Phase 2  AuthServer Repository/ConfigService
Phase 3  AuthServer tests
Phase 4  CamViewer DTO/Mapper
Phase 5  CamViewer 설정 동기화 검증
Phase 6  AdminWeb 조회 화면
Phase 7  다중 NVR 실제 재생 검증
Phase 8  V2 NVR 간 시간 동기화 보완(후속 작업)
```

각 Phase는 빌드/테스트가 가능한 상태를 확인한 뒤 다음 단계로 이동한다.
