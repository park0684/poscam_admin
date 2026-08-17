# POSCAM 다중 NVR 검증 상태 - 2026-08-17

## 기준 브랜치

- `park0684/poscam_admin` - `agent/multi-nvr-config`
- `park0684/POSCAM.CamViewer` - `agent/multi-nvr-config`

## 자동 검증 완료

### AuthServer Tests

최신 다중 NVR 서버 코드 기준 성공.

검증 범위에 다음 계약 테스트가 포함된다.

- Schema 2 DTO / NvrNo 계약
- legacy sync overwrite 보호 정책
- NVR 2 한 대만 남은 설정의 legacy 표현 불가 판정
- NVR 1이지만 채널이 다른 NVR 번호를 참조하는 legacy 표현 불가 판정
- 매장 상세 응답의 `Nvrs` / 채널 `NvrNo`
- `/api/config/capabilities` Schema 2 계약

확인된 성공 run:

```text
AuthServer Tests
run 32018491147
conclusion: success
```

### AdminWeb Release Build

다중 NVR 조회 모델/화면 변경을 포함한 Release 빌드 성공.

```text
AdminWeb Build
run 32018491063
conclusion: success
```

### MariaDB 정상 migration 경로

임시 MariaDB 11.4에서 다음 전체 흐름 성공.

```text
legacy 단일 NVR schema/data 생성
→ preflight
→ BLOCKING 조건 0 확인
→ migration apply
→ 기존 NVR/채널 NvrNo=1 확인
→ NVR 2 추가
→ 우측 채널 NVR2 / CH7 매핑
→ orphan 0 확인
→ verify SQL
→ migration 재실행
→ idempotency 확인
```

확인된 성공 run:

```text
Multi-NVR Migration Tests
run 32018491180
conclusion: success
```

### Preflight FK 차단 경로

`nvr_configs(nvr_store)`를 참조하는 inbound Foreign Key가 있는 legacy fixture를 생성한 뒤 preflight가 다음을 수행하는지 확인했다.

```text
FK_REFERENCING:nvr_configs 감지
BLOCKING 출력
구체적인 FK 이름 출력
migration apply 미실행
```

확인된 성공 run:

```text
Multi-NVR Preflight Guard Tests
run 32018491168
conclusion: success
```

## 코드 리뷰 완료 항목

### 서버

- `(StoreCode, NvrNo)` 식별 유지
- 채널별 `NvrNo` 저장/조회
- 전체 NVR/채널 원자적 교체 transaction
- 모든 NVR 행 동일 ConfigVersion 적용
- legacy 다운로드/업로드 손실 방지
- NVR 2 한 대만 남은 구성도 Schema 2 필수 처리
- single-process Sync race 보호
- 매장 상세 API 다중 NVR 조회
- AdminWeb 비밀번호 평문 미노출

### CamViewer

- `ViewerConfig.NvrList` 전체 API 왕복
- `CounterMap.NvrNo` 왕복
- SettingsPresenter 다중 NVR 추가/수정/삭제 및 `NextNvrNo` 유지
- CounterEditPresenter 선택 NVR 기준 채널 매핑
- PlayerPresenter가 `CounterMap.NvrNo`로 NVR 설정 조회
- `PlayerChannelTarget.NvrNo` 보존
- NvrPlayerPlaybackService가 `request.Channels.GroupBy(channel.NvrNo)`로 NVR별 재생 그룹 생성
- 실제 다중 NVR 설정 sync 전 AuthServer capability 확인
- 구형 AuthServer에 다중 NVR sync 미호출
- 설정 변경 후 legacy Provider 캐시 재사용 방지 경로 검토
- 신규 source file의 old-style csproj Compile Include 등록 확인

## 아직 자동 검증하지 못한 항목

### CamViewer Release x64

GitHub Actions Windows runner가 source checkout/build 전에 계정 Billing/Spending limit으로 차단된다.

GitHub annotation:

```text
The job was not started because recent account payments have failed
or your spending limit needs to be increased.
```

따라서 CamViewer는 코드 정적 검토까지 완료했지만 Release x64 컴파일 성공으로 표시하지 않는다.

### 운영 DB

운영 DB에는 migration을 실행하지 않았다.

실제 적용 전 필수:

```text
DB backup
→ 20260817_preflight_multi_nvr_config.sql
→ BLOCKING 0 확인
→ migration
→ verify
```

CI의 임시 MariaDB 성공 결과를 운영 DB의 실제 FK/인덱스/데이터 상태 확인으로 대체해서는 안 된다.

### 실제 NVR 장비

아직 남은 현장 검증:

- 기존 단일 NVR 매장 회귀
- NVR 1 / NVR 2 설정 upload/download 왕복
- 좌 NVR1 / 우 NVR2 실제 동시 재생
- NVR 2 한 대만 남은 번호 유지
- 같은 NvrNo의 IP/Provider/ID/Password 변경 후 이전 연결 미재사용
- 서로 다른 NVR 간 실제 시간축 차이 및 동기화 상태

## 현재 판단

AuthServer / AdminWeb / DB migration 코드는 운영 DB 적용 전 검증 단계까지 통과했다.

아직 완료로 처리할 수 없는 핵심 두 항목은 다음이다.

```text
1. CamViewer Release x64 실제 빌드
2. 운영/스테이징 DB 및 실제 NVR 장비 통합 검증
```

두 항목이 끝나기 전까지 관련 PR은 Draft 상태를 유지한다.
