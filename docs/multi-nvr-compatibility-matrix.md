# POSCAM 다중 NVR 버전 호환성 매트릭스

## 목적

다중 NVR 도입 기간에 구버전 CamViewer와 신규 AuthServer, 신규 CamViewer와 구버전 AuthServer가 혼재할 수 있다.

이 문서는 어떤 조합을 허용하고 어떤 조합을 명시적으로 차단하는지 고정한다.

다중 NVR 설정 스키마 버전은 `ConfigSchemaVersion = 2`이다.

## 용어

### Legacy 표현 가능 설정

Schema 1 CamViewer가 설정 손실 없이 표현할 수 있는 구성은 정확히 다음뿐이다.

```text
NVR 개수 = 1
NvrNo = 1
모든 채널의 NvrNo = 1
```

따라서 다음 구성은 NVR 물리 대수가 1대여도 legacy 표현 불가다.

```text
NVR 1 삭제
NVR 2만 유지
채널 NvrNo = 2
```

NVR 번호는 의미가 있는 식별자이므로 서버나 클라이언트가 NVR 2를 NVR 1로 자동 재번호화하지 않는다.

## 호환성 표

| CamViewer | AuthServer | 서버 설정 | 결과 |
|---|---|---|---|
| Schema 1 | 구버전 | NVR 1 한 대 | 기존 동작 |
| Schema 1 | 신규 Schema 2 | NVR 1 한 대 / 채널 NVR 1 | 다운로드·업로드 허용 |
| Schema 1 | 신규 Schema 2 | NVR 1 + NVR 2 | 다운로드·업로드 차단 |
| Schema 1 | 신규 Schema 2 | NVR 2 한 대만 유지 | 다운로드·업로드 차단 |
| Schema 2 | 구버전 | 로컬 NVR 1 한 대 | 기존 sync 호환 허용 |
| Schema 2 | 구버전 | 로컬 NVR 1 + NVR 2 | capability 확인 실패 후 sync 미호출 |
| Schema 2 | 구버전 | 로컬 NVR 2 한 대만 유지 | capability 확인 실패 후 sync 미호출 |
| Schema 2 | 신규 Schema 2 | NVR 1 한 대 | 정상 |
| Schema 2 | 신규 Schema 2 | NVR 여러 대 | 정상 |
| Schema 2 | 신규 Schema 2 | NVR 2 등 비연속 번호 | 번호 유지 후 정상 |

## 구버전 CamViewer → 신규 AuthServer

### 다운로드

Schema 1 요청은 서버 설정을 조회한 뒤 다음 조건을 만족할 때만 반환한다.

```text
Nvrs.Count == 1
Nvrs[0].NvrNo == 1
Channels.All(NvrNo == 1)
```

그 외에는 `ConfigSchemaNotSupported`로 실패한다.

### 업로드

Schema 1 업로드 전에 현재 서버 설정을 확인한다.

허용:

```text
현재 서버 설정 없음
또는
현재 설정이 legacy 표현 가능
```

차단:

```text
다중 NVR
NVR 2 한 대만 유지
NVR 1 이외의 NVR을 참조하는 채널
설정 버전 불일치
토큰/인증 실패
```

현재 단일 AuthServer 프로세스에서는 legacy 사전검사와 실제 Sync 사이에 다른 Sync가 끼지 못하도록 `/api/config/sync`를 `SemaphoreSlim`으로 직렬화한다.

이 보호는 프로세스 로컬이다. AuthServer 다중 인스턴스 환경에서는 DB/distributed lock 또는 설정 revision 방식으로 교체해야 한다.

## 신규 CamViewer → 구버전 AuthServer

신규 CamViewer는 다음 조건에서 `POST /api/config/capabilities`를 먼저 호출한다.

```text
Nvrs.Count > 1
또는
Nvrs 중 NvrNo != 1
또는
Channels 중 NvrNo != 1
```

신규 AuthServer 응답:

```text
MaxConfigSchemaVersion = 2
SupportsMultiNvr = true
```

구버전 AuthServer에는 endpoint가 없으므로 capability 호출이 실패한다. 이 경우 CamViewer는 `/api/config/sync`를 호출하지 않는다.

NVR 1 한 대 / 채널 NVR 1 구성만 구버전 AuthServer와 기존 sync 호환을 유지한다.

## 설정 버전과 구버전 실행

CamViewer 설정 화면에서 설정을 수정하면 로컬 설정은 `LocalModified` 상태가 된다.

Schema 2 Sync Mapper는 `LocalModified` 설정의 기존 `ConfigVersion`을 서버에 재사용하지 않고 빈 값으로 전송한다.

AuthServer는 빈 `ConfigVersion` 요청에 대해 새 설정 버전을 발급한다.

따라서 신규 CamViewer가 NVR 번호/채널 구성을 변경한 정상 Sync 이후에는 구버전 CamViewer의 이전 로컬 설정 버전과 서버 버전이 달라지고, 구버전 CamViewer는 최신 설정 조회 단계로 진입한 뒤 Schema 2 전용 구성에서 차단된다.

## 배포 원칙

호환 보호는 잘못된 순서에서 데이터 유실을 막기 위한 방어장치다. 정상 배포 순서는 변경하지 않는다.

```text
1. DB preflight / backup
2. DB migration / verify
3. 신규 AuthServer
4. AdminWeb
5. 신규 CamViewer Release x64 검증
6. 테스트 장비 배포
7. 일반 배포
```

신규 CamViewer를 구버전 AuthServer보다 먼저 일반 배포하는 방식을 정상 운영 절차로 사용하지 않는다.
