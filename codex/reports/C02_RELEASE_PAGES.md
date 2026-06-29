# C02 릴리스 관리 화면 구현 보고

## 작업 결과

- 상태: Completed
- UpdateServer 전용 설정·Client: 구현 완료
- 직접 URL 권한 Guard: 구현 완료
- 릴리스 목록·신규·상세 화면: 구현 완료
- 상태별 수정·게시·중지 UI: 구현 완료
- Client·UI 정책 테스트: 추가 완료
- 로컬 Release 빌드: 성공, 오류 0
- 빌드 경고: 기존 AdminWeb 미사용 필드 경고 6개, C02 신규 경고 0
- 전체 테스트: 64/64 성공, 실패 0, 건너뜀 0

## 기준 환경

- GitHub 저장소: `park0684/poscam_admin`
- 로컬 경로: `D:\_work\poscam`
- 브랜치: `feature/update-server-auth-contract`
- 솔루션: `D:\_work\poscam\poscam.sln`
- UpdateServer 계약 브랜치: `feature/initial-update-server`

## 변경 파일

### 설정과 API Client

- `AdminWeb/Models/UpdateApiSettings.cs`
  - `InternalBaseUrl`: AdminWeb 서버에서 UpdateServer JSON API 호출
  - `PublicBaseUrl`: C03 browser 직접 업로드용 공개 주소
- `AdminWeb/Models/Updates/UpdateApiCallResult.cs`
  - HTTP StatusCode, ErrorCode, Message, Data, X-Request-ID 보존
  - 401, 403, 404, 409, 503 상태 판정
- `AdminWeb/Models/Updates/PagedResponse.cs`
- `AdminWeb/Models/Updates/UpdateErrorCode.cs`
  - UpdateServer 오류 코드의 독립 JSON 계약 복제본
- `AdminWeb/Services/UpdateApiClient.cs`
  - 기존 AuthServer `ApiClient`와 독립
  - Request마다 Bearer AccountToken 설정
  - GET, POST, PUT, DELETE
  - 빈 Body·비JSON·네트워크·Timeout 안전 처리
  - 원본 HTML·Stack Trace를 화면에 반환하지 않음
- `AdminWeb/Program.cs`
  - UpdateApiSettings 검증
  - 독립 Typed UpdateApiClient 등록
  - 기존 ApiClient BaseAddress 변경 없음
- `AdminWeb/appsettings.json`
  - 로컬 Internal/Public URL `https://localhost:7164`

### DTO와 UI 정책

- `AdminWeb/Models/Updates/ReleaseDtos.cs`
  - 활성 제품, 목록 조건, CRUD 요청·응답, Artifact 요약, Lifecycle 응답
  - 생성·수정용 `ReleaseEditModel`
  - 3자리·4자리 숫자 버전 검증
  - 전체 강제와 기준 버전 강제 동시 설정 금지
  - 기준 버전이 릴리스 버전보다 높은 설정 금지
- `AdminWeb/Services/ReleaseUiPolicy.cs`
  - Draft: 수정·삭제 가능, Artifact 존재 시 게시 가능
  - Published: 읽기전용, Disable만 가능
  - Disabled: 읽기전용

### 직접 URL Guard

- `AdminWeb/Components/Pages/Updates/UpdateManagementGuard.razor`
  - 첫 Interactive Render 이후 C01 접근정보 확인
  - System 또는 Admin+UpdateManage=12만 ChildContent 렌더링
  - 401: 로그인 상태 제거 후 `/login`
  - 403·조회 실패: 내용 렌더링 금지
  - 메뉴 숨김과 별개로 모든 업데이트 화면에서 재검증

### 릴리스 화면

- `AdminWeb/Components/Pages/Updates/ReleaseList.razor`
  - `/updates/releases`
  - 제품, 채널, 상태, 검색어 필터
  - page, pageSize 서버 페이징
  - 상태·강제 정책·게시일·등록자 표시
- `AdminWeb/Components/Pages/Updates/ReleaseCreate.razor`
  - `/updates/releases/new`
  - Active Product 목록 사용
  - Draft 생성
  - 중복 릴리스 409 처리
- `AdminWeb/Components/Pages/Updates/ReleaseDetail.razor`
  - `/updates/releases/{ReleaseCode:long}`
  - Draft 수정·삭제
  - 게시 확인
  - Published 배포 중지 확인
  - Published·Disabled 핵심 필드 읽기전용
  - Artifact 목록 읽기전용 표시
  - 404, 409, 503 처리
- `AdminWeb/Components/Pages/Updates/ReleaseForm.razor`
  - 생성·Draft 수정 입력 UI 공통화
  - 읽기전용 상태에서는 비활성 제품 코드도 정확히 표시

### 테스트

- `AdminWeb.Tests/Services/UpdateApiClientTests.cs`
- `AdminWeb.Tests/Updates/ReleaseUiPolicyTests.cs`
- `AdminWeb.Tests/Navigation/MenuAccessFilterTests.cs`
  - xUnit2031 분석 경고 제거

## API 연결

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

AdminWeb은 UpdateServer 프로젝트나 DLL을 참조하지 않고 camelCase JSON 계약만 독립적으로 복제한다.

## 상태별 화면 정책

### Draft = 0

- 제품·버전·채널·강제 정책·노트·메모 수정
- Draft 저장
- Draft 삭제 확인
- 활성 Artifact가 1개 이상인 경우 게시 확인
- 게시 후 핵심정보·Artifact가 불변이라는 경고 표시

### Published = 1

- 모든 핵심 필드 읽기전용
- Artifact 목록 읽기전용
- 배포 중지 확인
- Draft 저장·삭제·게시 버튼 없음

### Disabled = 9

- 모든 핵심 필드 읽기전용
- Artifact 목록 읽기전용
- 재게시·수정·삭제·중지 버튼 없음

## 오류 처리

### 401 또는 5001·5003·5004

- CurrentUserAccessService 캐시 제거
- AuthStateService 로그인 상태 제거
- `/login` 강제 이동

### 403 또는 7001

- 로그인 유지
- 접근 캐시 제거
- 화면 데이터 제거
- 권한 없음 표시

### 404 또는 8010

- 릴리스 없음 표시
- 목록 이동 제공

### 409 또는 8011·8012

- 중복 또는 상태 변경 안내
- 상세 화면 재조회 유도

### 503 또는 9003

- 로그인 삭제 금지
- 관리자 권한 확인 서비스 장애 안내

C04에서 이 중복 오류 분기를 공통 Handler로 통합한다.

## Artifact 업로드 제외 확인

C02에서는 다음을 구현하지 않았다.

- `IBrowserFile`
- `OpenReadStream`
- `MultipartFormDataContent`
- AdminWeb 서버 업로드 중계
- browser XHR 업로드

기존 Artifact 목록은 읽기전용으로만 표시한다. 직접 업로드는 C03에서 연결한다.

## 테스트 범위

### UpdateApiClient 8개

- InternalBaseUrl 사용
- 요청별 Bearer Header
- camelCase JSON Body
- HTTP Status·ErrorCode·RequestId 보존
- 401·403·503 구분
- 비JSON 오류 응답 안전 처리
- Token 없음 시 HTTP 요청 차단

### Release UI 정책 12개

- Draft 수정·삭제·게시 조건
- Published 읽기전용·Disable
- Disabled 완전 읽기전용
- 전체 강제와 기준 버전 동시 설정 금지
- 기준 버전 상한
- 3자리·4자리 버전 허용
- 잘못된 버전 형식 거부
- 요청 문자열 정규화

C01 AdminWeb.Tests 19개 + C02 20개 = AdminWeb.Tests 39개

AuthServer.Tests 25개를 포함한 전체 테스트 수: 64개

## 검증 결과

실행 명령:

```powershell
cd D:\_work\poscam

git switch feature/update-server-auth-contract
git pull

dotnet restore poscam.sln
dotnet build poscam.sln -c Release
dotnet test poscam.sln -c Release --no-build
```

결과:

- Restore 성공
- AuthServer Release 빌드 성공
- AdminWeb Release 빌드 성공
- AuthServer.Tests Release 빌드 성공
- AdminWeb.Tests Release 빌드 성공
- 컴파일 오류 0
- 기존 AdminWeb 미사용 필드 경고 6개
- C02 신규 경고 0
- AdminWeb.Tests 39/39 성공
- AuthServer.Tests 25/25 성공
- 전체 64/64 성공
- 실패 0
- 건너뜀 0

## 남은 문제

- 컴파일 오류: 없음
- 실제 동작 오류: 자동 테스트에서 발견 없음
- 실제 UpdateServer·DB 연동 화면 검증: C05 범위
- Artifact 직접 업로드: C03 범위
- 감사 로그·공통 오류 Handler: C04 범위
- 다음 단계 선행조건: 충족, C03 시작 가능

## 정책 이탈 여부

- 상태별 화면을 자동 검증하기 위해 `ReleaseUiPolicy`와 테스트 파일을 추가했다.
- Artifact 업로드는 포함하지 않았다.
- UpdateServer 프로젝트 참조와 기존 ApiClient BaseAddress 변경은 없다.
- 그 외 정책 이탈 없음.
