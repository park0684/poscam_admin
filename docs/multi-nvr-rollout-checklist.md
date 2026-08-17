# POSCAM 다중 NVR 적용 체크리스트

## 1. 적용 대상

이번 작업은 한 매장에서 여러 NVR을 사용할 수 있도록 설정 계약을 확장한다.

- AuthServer / DB: `park0684/poscam_admin` - `agent/multi-nvr-config`
- AdminWeb: `park0684/poscam_admin` - `agent/multi-nvr-config`
- CamViewer: `park0684/POSCAM.CamViewer` - `agent/multi-nvr-config`

핵심 식별 규칙은 다음과 같다.

- NVR 식별자: `(StoreCode, NvrNo)`
- `NvrNo`는 매장 내부에서만 유일하다.
- 삭제한 `NvrNo`를 임의 재번호화하지 않는다.
- 각 채널 매핑은 반드시 `NvrNo`를 가진다.
- Schema 2에서 존재하지 않는 NVR을 참조하는 채널은 오류로 처리한다.

## 2. 현재 코드 검증 상태

### AuthServer

- [x] 다중 NVR Entity / DTO 계약 반영
- [x] Repository 다중 NVR 조회/저장 반영
- [x] Config 다운로드 `Nvrs[]` 반영
- [x] Config 업로드 전체 NVR 교체 반영
- [x] 채널별 `NvrNo` 저장 반영
- [x] Schema 2 NVR 번호/중복/참조 검증 반영
- [x] 구버전 다운로드가 다중 NVR을 NVR 1로 축약하지 않도록 차단
- [x] 구버전 업로드가 기존 다중 NVR 설정을 NVR 1 한 대로 덮어쓰지 않도록 사전 차단
- [x] 단일 AuthServer 프로세스에서 legacy 사전검사와 실제 Sync 사이 경쟁 조건 방지를 위한 Sync 직렬화
- [x] `/api/config/capabilities` Schema 2 지원 정보 제공
- [x] 매장 상세 API도 전체 `Nvrs` 및 채널별 `NvrNo` 반환
- [x] AuthServer PR CI 통과 이력 있음

### AdminWeb

- [x] 다중 NVR 목록 조회 표시
- [x] 채널별 NVR 번호 표시
- [x] 비밀번호 원문 미표시
- [x] 조회 전용 정책 유지
- [x] 매장 상세 응답 모델에 `Nvrs` 반영
- [x] AdminWeb Release PR CI 통과 이력 있음

### CamViewer

- [x] ConfigLatest 요청에 `ConfigSchemaVersion = 2` 선언
- [x] ConfigSync 요청에 `ConfigSchemaVersion = 2` 선언
- [x] 다운로드 시 `Nvrs[]` 전체를 `ViewerConfig.NvrList`로 복원
- [x] 업로드 시 `ViewerConfig.NvrList` 전체 전송
- [x] 채널별 `NvrNo` 왕복 보존
- [x] Schema 2의 누락/중복/잘못된 NVR 참조를 자동 보정하지 않고 오류 처리
- [x] 설정 변경 후 legacy Provider 캐시 재사용 방지 처리
- [x] 실제 다중 NVR 업로드 전 AuthServer capability 확인
- [x] 구형 AuthServer가 capability를 제공하지 않으면 실제 Sync 전에 다중 NVR 업로드 차단
- [x] NVR 1 한 대만 사용하는 신규 CamViewer는 구형 AuthServer 호환 유지
- [ ] Release x64 CI 통과
  - 현재 GitHub Actions Windows job이 저장소 코드 실행 전 계정 Billing/Spending limit 문제로 시작되지 못한 상태다.

## 3. DB 적용 전 필수 절차

운영 DB에는 아래 순서를 바꾸지 않는다.

1. DB 전체 백업
2. `20260817_preflight_multi_nvr_config.sql` 실행
3. 모든 `BLOCKING` 판정이 0인지 확인
4. 특히 `nvr_configs`를 참조하는 기존 Foreign Key가 0인지 확인
5. 매장별 기존 `nvr_configs` 중복 여부 확인
6. `ch_config` 논리키 중복 여부 확인
7. 이상이 없을 때만 `20260817_add_multi_nvr_config.sql` 실행
8. 즉시 `20260817_verify_multi_nvr_config.sql` 실행
9. 모든 검증값이 정상인지 확인

### 적용 중단 조건

다음 중 하나라도 해당되면 마이그레이션을 실행하지 않는다.

- `nvr_configs`를 참조하는 기존 FK가 존재함
- `nvr_store` 단일 PK가 아닌 예상 밖 구조가 이미 존재함
- 일부 다중 NVR 컬럼만 수동 적용된 상태임
- 기존 `nvr_configs`에 매장별 중복 행이 존재함
- `ch_config`의 `(chn_store, chn_pos, chn_screen)` 중복이 존재함
- 백업이 확보되지 않음

## 4. 배포 순서

DB와 클라이언트의 스키마 전환이 포함되므로 다음 순서를 권장한다.

1. 운영 DB 백업 및 preflight 완료
2. DB migration 적용 및 verify 완료
3. Schema 2 지원 AuthServer 배포
4. AdminWeb 배포
5. 신규 CamViewer Release x64 빌드 검증
6. 신규 CamViewer를 테스트 매장/장비에 우선 적용
7. 단일 NVR 매장 회귀 테스트
8. 다중 NVR 테스트 매장 적용
9. 이상 없을 때 일반 배포

기본 원칙은 **AuthServer 선배포 → CamViewer 배포**다.

순서가 어긋나더라도 다음 보호가 동작해야 한다.

- 구버전 CamViewer → 신규 AuthServer: 다중 NVR 다운로드/업로드를 서버가 차단
- 신규 CamViewer 다중 NVR → 구버전 AuthServer: capability endpoint 확인 실패로 실제 Sync 전에 클라이언트가 차단
- 신규 CamViewer 단일 NVR 1 → 구버전 AuthServer: 기존 호환 유지

## 5. 통합 테스트 시나리오

### A. 기존 단일 NVR 매장

- [ ] 기존 NVR이 `NvrNo = 1`로 조회됨
- [ ] 기존 채널이 `NvrNo = 1`로 조회됨
- [ ] 설정 다운로드 성공
- [ ] 설정 수정 후 업로드 성공
- [ ] 재다운로드 후 동일 설정 유지
- [ ] 기존 영상 재생 정상

### B. 다중 NVR 기본 왕복

테스트 구성 예:

- NVR 1: 계산대 1 좌측 CCTV / CH 3
- NVR 2: 계산대 1 우측 POS / CH 7

확인:

- [ ] CamViewer에서 NVR 1, NVR 2 각각 등록 가능
- [ ] 계산대 좌/우 채널에 서로 다른 `NvrNo` 지정 가능
- [ ] 서버 capability 확인 성공
- [ ] 서버 업로드 후 `nvr_configs`에 2행 저장
- [ ] `ch_config.chn_nvr_no`가 각각 1, 2로 저장
- [ ] 다시 다운로드해도 NVR 번호 및 채널 소속 유지
- [ ] 좌측은 NVR 1 CH 3, 우측은 NVR 2 CH 7에서 실제 재생

### C. 삭제/번호 안정성

- [ ] NVR 2 삭제 후 NVR 1 번호가 변경되지 않음
- [ ] NVR 1을 삭제하고 NVR 2만 남은 경우 NvrNo=2가 유지됨
- [ ] NVR 2만 남은 설정은 Schema 2 capability가 필수로 확인됨
- [ ] 새 NVR 추가 시 기존 번호를 강제로 재정렬하지 않음
- [ ] 삭제된 NVR을 참조하는 채널이 저장되지 않음

### D. 설정 변경 및 Provider 캐시

- [ ] 같은 `NvrNo`의 IP 변경 후 이전 IP로 접속하지 않음
- [ ] 같은 `NvrNo`의 Provider 변경 후 이전 Provider를 재사용하지 않음
- [ ] ID/비밀번호 변경 후 이전 로그인 세션을 재사용하지 않음
- [ ] 설정 변경 직후 새 설정으로 재생 가능

### E. 구버전 CamViewer 보호

다중 NVR 매장에서:

- [ ] Schema 1 최신 설정 다운로드가 명시적으로 거부됨
- [ ] Schema 1 설정 업로드가 명시적으로 거부됨
- [ ] 거부 후 서버의 기존 NVR 1/NVR 2 데이터가 그대로 유지됨
- [ ] 채널별 `NvrNo` 데이터가 그대로 유지됨

### F. 신규 CamViewer + 구형 AuthServer 보호

- [ ] NVR 1 한 대 설정은 기존 sync 가능
- [ ] NVR 1 + NVR 2 설정은 capability 확인 실패 후 sync 미호출
- [ ] NVR 2 한 대만 남은 설정도 capability 확인 실패 후 sync 미호출
- [ ] 차단 메시지에 AuthServer 선업데이트 필요성이 표시됨
- [ ] 서버 기존 설정이 변경되지 않음

### G. 오류 데이터

- [ ] 중복 `NvrNo` 업로드 거부
- [ ] `NvrNo <= 0` 업로드 거부
- [ ] 존재하지 않는 NVR을 참조하는 채널 업로드 거부
- [ ] NVR 최대 채널보다 큰 채널 번호 거부
- [ ] 동일 계산대/화면 위치 중복 매핑 거부

## 6. 동시성 및 운영 제약

현재 `/api/config/sync` 직렬화는 `SemaphoreSlim` 기반으로 **한 AuthServer 프로세스 내부에서만** 보장된다.

현재처럼 AuthServer가 단일 인스턴스로 운영되는 동안에는 legacy precheck와 실제 sync 사이 경쟁 조건을 막을 수 있다.

향후 AuthServer를 여러 컨테이너/인스턴스로 수평 확장할 경우 다음 중 하나로 교체해야 한다.

- DB advisory/distributed lock
- 설정 revision 컬럼 기반 optimistic concurrency
- 별도 분산 lock 서비스

다중 인스턴스 전환 전에 현재 process-local gate를 그대로 신뢰해서는 안 된다.

## 7. 운영 확인 항목

- AuthServer 로그에 NVR 비밀번호가 기록되지 않아야 한다.
- 오류 로그는 매장, NVR 번호, 채널 번호 수준까지만 남긴다.
- AdminWeb은 다중 NVR을 조회할 수 있지만 현장 NVR 접속 설정을 수정하지 않는다.
- 운영 DB migration은 자동 배포 단계에 묶지 않고 사전 점검 결과를 확인한 뒤 별도로 수행한다.
- 최초 운영 적용 시 단일 NVR 매장과 다중 NVR 매장을 각각 한 곳 이상 확인한다.
- 신규 CamViewer 배포 전에 운영 AuthServer의 `/api/config/capabilities` 응답을 확인한다.

## 8. 이번 작업 범위 밖

다음 항목은 다중 NVR 설정 저장/동기화와 분리한다.

- 서로 다른 NVR 간 프레임 단위 정밀 시간 동기화 알고리즘 재설계
- 제조사 SDK 자체 수정
- AdminWeb에서 NVR 접속정보 편집 기능 제공
- NVR 번호 자동 재배열
- 운영 DB migration 자동 실행
- AuthServer 다중 인스턴스용 분산 동시성 제어
