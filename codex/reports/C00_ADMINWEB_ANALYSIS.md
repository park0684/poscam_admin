# C00 AdminWeb 분석 보고

## 작업 결과

- 상태: InProgress
- AdminWeb 코드·문서·UpdateServer API 계약 분석: 완료
- 코드 변경: 없음
- 로컬 Release 빌드: 확인 필요
- 차단 계약: 없음

## 기준 저장소와 브랜치

- 저장소: `park0684/poscam_admin`
- 로컬 경로: `D:\_work\poscam`
- 브랜치: `feature/update-server-auth-contract`
- AuthServer 선행 작업: A00~A05 Completed
- UpdateServer 선행 작업: B00~B10 Completed

## 확인한 현재 구조

### Program과 HTTP Client

현재 `AdminWeb/Program.cs`는 다음 서비스만 등록한다.

- `ApiSettings`
- `AuthStateService`
- AuthServer 전용 Typed `ApiClient`

기존 `ApiClient`의 BaseAddress는 `ApiSettings.BaseUrl` 하나이다. UpdateServer 연결을 위해 이 값을 변경하거나 기존 Client를 재사용해서는 안 된다.

기존 `ApiClient` 특성:

- AuthServer JSON API 전용
- `AuthStateService`의 AccountToken을 Bearer Header에 설정
- GET·POST·PUT·DELETE 지원
- 응답 HTTP 상태를 호출자에게 반환하지 않음
- 빈 Body와 JSON 역직렬화 실패를 예외로 변환
- `DefaultRequestHeaders.Authorization`을 호출마다 변경
- multipart와 업로드 진행률·취소 기능 없음

결론:

- C02에서 독립된 `UpdateApiClient`가 필요하다.
- `UpdateApiClient`는 `HttpRequestMessage` 단위로 Authorization Header를 설정해 동시 호출과 오래된 Header 잔존을 방지한다.
- HTTP StatusCode, ErrorCode, Message, Data, X-Request-ID를 함께 보존하는 호출 결과 형식이 필요하다.
- 기존 `ApiClient`는 변경하지 않는다.

### 로그인 상태

`AuthStateService`는 다음 값을 browser `sessionStorage`에 저장한다.

- AccountToken
- UserCode
- UserName
- UserRole
- PartnerCode

현재 관리자 세부 권한은 저장하지 않는다. 이는 확정 정책과 일치한다.

C01 접근정보 캐시는 다음 원칙으로 구성한다.

- Scoped 메모리 캐시
- `GET /api/accounts/me/access` 결과만 캐시
- permissionCodes를 sessionStorage에 저장하지 않음
- 캐시와 함께 사용한 Token 값을 보관
- Token이 바뀌거나 없어지면 자동 무효화
- 403 발생 시 명시적 무효화
- 로그아웃 시 명시적 무효화

Token 기반 캐시 키를 사용하면 `AuthStateService`와 접근정보 서비스 간 순환 DI 없이 로그인 변경을 감지할 수 있다.

### 메뉴

현재 구조:

- `MenuConfiguration.GetMenus()`가 전체 메뉴를 정적으로 생성
- `MenuItem`에 `Roles`가 있으나 실제 필터에서 사용하지 않음
- `NavMenu`는 `OnInitialized()`에서 메뉴를 그대로 로드
- 역할·권한 필터 없음
- 자식이 모두 제거된 빈 부모 그룹 처리 없음

C01에서는 기존 메뉴의 표시 정책을 재설계하지 않는다. 새 업데이트 메뉴에만 확정 권한을 적용한다.

새 메뉴:

```text
업데이트관리
├─ 릴리스 관리  /updates/releases
└─ 감사 로그    /updates/audit-logs
```

표시 정책:

- System(0): 허용
- Admin(1): permissionCodes에 12가 있을 때 허용
- PartnerUser(2): 거부
- User 또는 기타 역할: 거부
- 접근정보 조회 실패: 거부
- 권한이 없는 자식만 남은 부모: 숨김

초기 렌더에서 권한 없는 업데이트 메뉴가 잠시 노출되지 않도록, 접근정보 확인 전에는 권한 필요 메뉴를 제외한다.

### Blazor Server 생명주기

AdminWeb은 Interactive Server 방식이며 token 조회는 JavaScript `sessionStorage`에 의존한다.

따라서 다음 작업은 `OnInitialized` 또는 prerender 단계에서 실행하면 안 된다.

- AccountToken 조회
- 현재 접근정보 API 호출
- UpdateServer 관리자 API 호출
- 직접 업로드 시작

기존 페이지와 동일하게 첫 `OnAfterRenderAsync(firstRender)` 이후 실행해야 한다.

### 기존 화면 패턴

재사용 가능한 패턴:

- Bootstrap card, alert, table, badge
- `_isLoading`, `_message`, 성공·경고 CSS
- 검색 조건 영역과 목록 테이블
- popup.js의 전역 JS 함수 로딩 방식
- `PageTitle`, InteractiveServer page

주의할 기존 패턴:

- 일부 페이지는 역할만 확인하고 세부 권한을 확인하지 않음
- 일부 오류 처리는 Message에 `토큰`, `로그인` 문자열이 포함되는지 검사
- ApiClient가 HTTP StatusCode를 노출하지 않음
- 공통 접근 Guard와 공통 API 오류 Handler가 없음

업데이트 영역은 기존 화면의 시각 패턴만 재사용하고, 문자열 기반 오류 분기는 복사하지 않는다.

### JavaScript Interop

현재 `App.razor`는 `_framework/blazor.web.js`와 `/js/popup.js`를 전역 로드한다.

C03은 현재 관례에 맞춰 다음 파일을 추가한다.

```text
AdminWeb/wwwroot/js/updateUpload.js
```

그리고 `App.razor`에 정적 script 참조를 추가한다.

업로드 정책:

- browser 파일 input의 실제 File 객체를 JavaScript가 직접 사용
- `XMLHttpRequest` + `FormData`
- URL은 `UpdateApiSettings.PublicBaseUrl`
- Header는 `Authorization: Bearer {token}`
- Form 필드는 `os`, `architecture`, `packageType`, `file`
- 진행률, 취소, 네트워크 오류, 서버 오류를 구분
- 업로드 중 페이지 이탈 경고
- 성공 후 Release 상세 재조회
- `IBrowserFile.OpenReadStream`, AdminWeb 임시파일, AdminWeb 서버 중계 금지

### 테스트 구조

현재 Solution에는 다음 프로젝트만 있다.

- AuthServer
- AdminWeb
- AuthServer.Tests

AdminWeb 테스트 프로젝트는 없다. C01의 메뉴 테스트와 C02~C04의 Client·상태·오류 테스트를 위해 다음 프로젝트가 필요하다.

```text
AdminWeb.Tests/poscam.AdminWeb.Tests.csproj
```

기본 방향:

- xUnit
- AdminWeb 프로젝트 참조
- pure menu filter와 접근 판정 테스트
- UpdateApiClient는 Stub HttpMessageHandler로 테스트
- 필요한 경우 C02에서 bUnit을 추가해 권한 Guard·상태별 화면을 검증
- `poscam.sln`에 프로젝트 추가

## C01 확정 수정 목록 — 접근정보와 메뉴

### 접근정보

신규:

- `AdminWeb/Models/Account/CurrentUserAccessDto.cs`
  - `CurrentUserAccessResponse`
  - 필요 시 접근 조회 상태 Result
- `AdminWeb/Services/CurrentUserAccessService.cs`
  - AuthServer `/api/accounts/me/access` 호출
  - Token 연결 Scoped 캐시
  - `CanManageUpdatesAsync`
  - 명시적 `Invalidate`

수정:

- `AdminWeb/Program.cs`
  - CurrentUserAccessService Scoped 등록

### 메뉴

수정:

- `AdminWeb/Models/Navigation/MenuItem.cs`
  - 단일 또는 nullable RequiredPermissionCode 필드 추가
  - System 허용 여부를 필터에서 역할과 함께 판단
- `AdminWeb/Models/Navigation/MenuConfiguration.cs`
  - 업데이트관리 부모와 두 하위 메뉴 추가
- `AdminWeb/Components/Layout/NavMenu.razor`
  - 첫 렌더 후 접근정보 조회
  - fail-closed 필터
  - 빈 부모 제거
  - 로그아웃 시 접근 캐시 무효화

신규:

- `AdminWeb/Services/MenuAccessFilter.cs`
  - UI와 분리된 순수 필터
  - 기존 MenuItem 원본을 변경하지 않고 복사·필터

### 테스트

신규·수정:

- `AdminWeb.Tests/poscam.AdminWeb.Tests.csproj`
- `AdminWeb.Tests/Navigation/MenuAccessFilterTests.cs`
- `AdminWeb.Tests/Services/CurrentUserAccessPolicyTests.cs`
- `poscam.sln`

필수 시나리오:

- System: 업데이트 메뉴 표시
- Admin + 12: 표시
- Admin - 12: 숨김
- PartnerUser: 숨김
- 기타 역할: 숨김
- 접근 조회 실패: 숨김
- 빈 부모 그룹 미표시
- 기존 메뉴 순서·구조 유지

## C02 확정 수정 목록 — 릴리스 관리 화면

### 설정과 Client

신규:

- `AdminWeb/Models/UpdateApiSettings.cs`
  - `InternalBaseUrl`
  - `PublicBaseUrl`
- `AdminWeb/Services/UpdateApiClient.cs`
  - UpdateServer JSON API 전용
  - InternalBaseUrl 사용
  - Request별 Bearer Header
  - HTTP Status·ErrorCode·RequestId 보존
- `AdminWeb/Models/Updates/UpdateApiCallResult.cs`
- `AdminWeb/Models/Updates/PagedResponse.cs`
- `AdminWeb/Models/Updates/ReleaseDtos.cs`
- `AdminWeb/Models/Updates/ArtifactDtos.cs`
- `AdminWeb/Models/Updates/UpdateErrorCode.cs`

수정:

- `AdminWeb/Program.cs`
  - UpdateApiSettings와 UpdateApiClient 등록
- `AdminWeb/appsettings.json`
  - UpdateApiSettings 기본 구조 추가

중요:

- 기존 ApiSettings와 ApiClient BaseAddress는 변경하지 않는다.
- AdminWeb은 UpdateServer 프로젝트나 DLL을 참조하지 않는다.
- DTO는 JSON 계약만 독립 복제한다.

### 권한 Guard

신규:

- `AdminWeb/Components/Pages/Updates/UpdateManagementGuard.razor`
  또는 동일 목적의 재사용 가능한 Guard

정책:

- 메뉴 숨김과 별개로 모든 Update page 직접 URL에서 다시 확인
- 확인 중 Loading
- 401: 로그인 상태 제거 후 `/login`
- 권한 없음·조회 실패: 내용 렌더링 금지
- Guard는 보조 UI 경계이며 실제 보안 경계는 UpdateServer

### 화면

신규:

- `AdminWeb/Components/Pages/Updates/ReleaseList.razor`
  - route `/updates/releases`
  - product, channel, status, keyword, page, pageSize
  - 서버 페이징
- `AdminWeb/Components/Pages/Updates/ReleaseCreate.razor`
  - route `/updates/releases/new`
  - Draft 생성
- `AdminWeb/Components/Pages/Updates/ReleaseDetail.razor`
  - route `/updates/releases/{ReleaseCode:long}`
  - Draft 수정·삭제
  - Publish 확인
  - Published Disable 확인
  - Published·Disabled 핵심 필드 읽기전용
  - Artifact 목록 표시
- 필요 시 `AdminWeb/Components/Pages/Updates/ReleaseForm.razor`
  - 생성·Draft 수정 입력 중복 제거

API:

```text
GET    /api/v1/admin/products/active
GET    /api/v1/admin/releases
POST   /api/v1/admin/releases
GET    /api/v1/admin/releases/{releaseCode}
PUT    /api/v1/admin/releases/{releaseCode}
DELETE /api/v1/admin/releases/{releaseCode}
POST   /api/v1/admin/releases/{releaseCode}/publish
POST   /api/v1/admin/releases/{releaseCode}/disable
```

상태:

- 0 Draft
- 1 Published
- 9 Disabled

입력 정책:

- `isMandatory=true`와 `forceUpdateBelowVersion` 동시 입력 금지
- version은 3자리 또는 4자리 숫자 형식
- channel은 stable, beta, internal
- product는 활성 제품 API 결과

### 테스트

- `AdminWeb.Tests/Services/UpdateApiClientTests.cs`
- `AdminWeb.Tests/Updates/ReleaseUiPolicyTests.cs`
- 필요 시 Guard component 테스트

## C03 확정 수정 목록 — 브라우저 직접 업로드

신규:

- `AdminWeb/wwwroot/js/updateUpload.js`
- `AdminWeb/Components/Pages/Updates/ArtifactUploadPanel.razor`
- `AdminWeb/Models/Updates/ArtifactUploadInteropDtos.cs`

수정:

- `AdminWeb/Components/App.razor`
  - updateUpload.js 정적 참조
- `AdminWeb/Components/Pages/Updates/ReleaseDetail.razor`
  - Draft 상태에서만 Upload panel 렌더링
  - 성공 후 상세 재조회

호출 URL:

```text
{PublicBaseUrl}/api/v1/admin/releases/{releaseCode}/artifacts
```

multipart:

```text
os
architecture
packageType
file
```

UI 정책:

- Draft만 업로드
- x86, x64, any
- 현재 초기 OS는 windows
- 현재 packageType은 full
- 동일 Target 업로드는 Draft Artifact 교체 확인
- 진행률 표시
- 취소 버튼
- 업로드 중 저장·게시·페이지 이탈 방지
- 401, 403, 409, 413, 415, 500, 503, 네트워크 오류, 사용자 취소 구분

검증:

- Browser Network에서 request payload가 UpdateServer로 직접 전송됨
- AdminWeb request/SignalR payload에 ZIP bytes 없음
- Authorization Header 사용
- Query String에 token 없음

## C04 확정 수정 목록 — 감사와 공통 오류

### 감사 화면

신규:

- `AdminWeb/Models/Updates/AuditDtos.cs`
- `AdminWeb/Components/Pages/Updates/AuditLogList.razor`
  - route `/updates/audit-logs`
  - action, targetType, targetCode, actorUserCode, requestId, fromUtc, toUtc
  - 서버 페이징
  - 읽기전용
- `AdminWeb/Components/Pages/Updates/ReleaseAuditPanel.razor`
  - Release 상세에서 해당 Release와 Artifact 이력 표시

API:

```text
GET /api/v1/admin/audit-logs
GET /api/v1/admin/releases/{releaseCode}/audit-logs
```

### 오류 처리

신규:

- `AdminWeb/Services/UpdateApiErrorHandler.cs`
  또는 동일 목적의 Helper

정책:

- 401 또는 5001·5003·5004
  - AuthStateService.ClearAsync
  - CurrentUserAccessService.Invalidate
  - `/login` 이동
- 403 또는 7001
  - 로그인 유지
  - 접근 캐시 무효화
  - 권한 없음 표시
  - 업데이트 화면 내용 제거
- 404·8010·8020
  - 대상 없음
- 409·8011·8012·8022·8033
  - 상태 또는 동시 변경 충돌
  - 화면 재조회 유도
- 413·8031
  - 파일 크기 초과
- 415·8030
  - 잘못된 ZIP
- 503·9003
  - AuthServer 관리자 인증 장애
  - 로그인 삭제 금지
- 빈 Body·비JSON·네트워크 오류
  - 안전한 일반 메시지
  - 원본 HTML·응답 Body·Secret 표시 금지

수정:

- C02·C03 Update 화면의 중복 오류 분기를 Helper 사용으로 통합
- 403 발생 시 메뉴와 직접 URL 권한 상태 재평가

### 테스트

- `AdminWeb.Tests/Updates/UpdateApiErrorHandlerTests.cs`
- `AdminWeb.Tests/Updates/AuditQueryPolicyTests.cs`

## C05 검증 범위

- System
- Admin + UpdateManage=12
- Admin - UpdateManage=12
- PartnerUser
- 기타 역할
- 메뉴 표시와 직접 URL Guard 일치
- 기존 AuthServer ApiClient와 기존 화면 회귀 없음
- Draft 생성·수정·삭제·업로드·게시
- Published 읽기전용·중지
- Disabled 읽기전용
- browser 직접 업로드와 progress·cancel
- 401·403·404·409·413·415·500·503
- 감사 로그 읽기전용·서버 페이징
- UpdateServer 프로젝트 참조 없음
- permissionCodes sessionStorage 저장 없음

## 재사용 요소

- 기존 `AuthStateService` AccountToken
- 기존 `ApiResponse<T>` JSON 구조
- Bootstrap card, alert, table, badge
- `PageTitle`, InteractiveServer route
- App.razor 전역 JS 로딩 방식
- popup.js 자체는 수정하지 않음

## 주요 위험과 대응

### JS Interop 시점

위험: OnInitialized에서 sessionStorage를 호출하면 prerender/interactive 전환 오류가 발생할 수 있다.

대응: 메뉴, Guard, Update page 최초 호출은 first OnAfterRenderAsync 이후 수행한다.

### 권한 메뉴 Flash

위험: 접근정보 조회 전에 업데이트 메뉴가 렌더링될 수 있다.

대응: 권한 필요 메뉴는 접근정보 확인 전 기본 제외한다.

### Scoped 캐시 수명

위험: Blazor Server Scoped 서비스는 browser circuit 수명 동안 유지된다.

대응: Token 연결 캐시와 401·403·로그아웃 명시 무효화를 함께 사용한다.

### 기존 ApiClient 영향

위험: BaseAddress를 UpdateServer로 변경하면 AuthServer 기존 기능 전체가 손상된다.

대응: 별도 UpdateApiClient만 추가한다.

### 직접 업로드 중계

위험: IBrowserFile을 .NET에서 읽으면 1GB가 SignalR과 AdminWeb 서버를 통과한다.

대응: JS가 input File을 직접 XHR로 전송하며 .NET은 파일 bytes를 읽지 않는다.

### 오류 상태 손실

위험: 기존 ApiClient 방식은 HTTP 401과 403을 구분하기 어렵다.

대응: UpdateApiClient 결과에 HTTP StatusCode와 ErrorCode를 모두 보존한다.

### UI 권한을 보안 경계로 오인

위험: 메뉴와 Guard만으로 UpdateServer API가 보호된다고 오해할 수 있다.

대응: UI는 fail-closed 보조 경계이고 UpdateServer의 매 요청 AuthServer 권한 확인이 실제 경계다.

## 문서 경로 참고

C00~C05 Prompt는 완료 시 `codex/update-server/WORK_STATUS.md`를 갱신하라고 적고 있으나, 현재 브랜치의 실제 상태 파일은 다음이다.

```text
codex/WORK_STATUS.md
```

현재 파일을 기준으로 갱신한다. 이 차이는 구현 차단 사유가 아니며 C 단계에서 새 상태 파일을 중복 생성하지 않는다.

## 검증 결과

실행 명령:

```powershell
cd D:\_work\poscam
git switch feature/update-server-auth-contract
git pull
dotnet build poscam.sln -c Release
```

결과:

- 로컬 확인 필요
- C00 변경은 분석 보고서와 WORK_STATUS 문서뿐이며 실행 코드 변경 없음

## 남은 문제

- 컴파일 오류: 로컬 검증 필요
- 실제 동작 오류: C00은 구현 전 분석 단계
- 불필요한 중복: 현재 업데이트 전용 Client·Guard·오류 Helper 없음
- 다음 단계 선행조건: Release 빌드 성공 후 C01 시작

## 정책 이탈 여부

- 없음
