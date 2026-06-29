# C03 브라우저 직접 Artifact 업로드 구현 보고

## 작업 결과

- 상태: InProgress
- JavaScript XHR 직접 업로드: 구현 완료
- 진행률·취소·이탈 경고: 구현 완료
- 동일 Target 교체 확인: 구현 완료
- Release 상세 화면 연결: 구현 완료
- 정책 자동 테스트: 추가 완료
- 로컬 Release 빌드·전체 테스트: 확인 필요
- Browser Network 실제 검증: 확인 필요

## 기준 환경

- GitHub 저장소: `park0684/poscam_admin`
- 로컬 경로: `D:\_work\poscam`
- 브랜치: `feature/update-server-auth-contract`
- 솔루션: `D:\_work\poscam\poscam.sln`
- UpdateServer 계약 브랜치: `feature/initial-update-server`

## 변경 파일

### JavaScript 직접 업로드

- `AdminWeb/wwwroot/js/updateUpload.js`
  - 일반 HTML file input의 browser `File` 객체 사용
  - `XMLHttpRequest`와 `FormData`로 UpdateServer 직접 전송
  - `Authorization: Bearer {AccountToken}` Header
  - `X-Request-ID` Header
  - multipart 필드 `os`, `architecture`, `packageType`, `file`
  - upload progress callback
  - abort 취소
  - JSON 응답만 파싱
  - 비JSON·HTML 응답 원문은 .NET에 전달하지 않음
  - 동시 중복 실행 방지

### Interop 계약과 정책

- `AdminWeb/Models/Updates/ArtifactUploadInteropDtos.cs`
  - 시작 옵션
  - 선택 파일 메타데이터
  - 진행률
  - 완료 결과
  - URL·Target·오류 처리 정책
  - 파일 바이트 필드 없음
- PublicBaseUrl 검증
  - HTTP 또는 HTTPS 절대 URL
  - 사용자정보 금지
  - Query 금지
  - Fragment 금지
  - Token Query String 금지
- 기본 Client 사전 크기 제한: 1GB
  - 실제 최종 제한은 UpdateServer 설정과 413 응답이 기준

### 업로드 UI

- `AdminWeb/Components/Pages/Updates/ArtifactUploadPanel.razor`
  - Draft 전용 업로드 패널
  - OS: windows
  - Architecture: x86, x64, any
  - Package Type: full
  - 일반 `<input type="file">` 사용
  - ZIP 확장자·빈 파일·1GB 초과 사전 검사
  - 동일 활성 Target 교체 확인
  - 진행률과 전송량 표시
  - 취소 버튼
  - 내부 이동 시 취소 확인
  - 외부 이동 시 `NavigationLock` 경고
  - 401 로그인 제거 및 `/login` 이동
  - 403 로그인 유지, 접근 캐시 무효화
  - 성공 후 부모 상세 재조회 요청

### 상세 화면 연결

- `AdminWeb/Components/Pages/Updates/ReleaseDetail.razor`
  - Draft 상태에서만 Upload Panel 렌더링
  - 업로드 중 Draft 저장·삭제·게시 차단
  - 활성 Artifact가 있을 때만 게시 가능
  - 업로드 성공 후 상세 API 재조회
  - Artifact 목록 갱신
  - Published·Disabled에서는 업로드 UI 미표시

### 정적 참조

- `AdminWeb/Components/App.razor`
  - `/js/updateUpload.js` 로드

### 테스트

- `AdminWeb.Tests/Updates/ArtifactUploadInteropPolicyTests.cs`
  - 직접 업로드 URL 생성
  - URL에 Token 없음
  - 잘못된 URL·사용자정보·Query·Fragment 거부
  - 동일 활성 Target 판정
  - Disabled·다른 Target 무시
  - 401·5001·5003·5004 판정
  - 403·7001 판정
  - 취소·네트워크·409·413·415·500·503 메시지

## 데이터 흐름

```text
Browser file input
→ JavaScript File 객체
→ FormData
→ XMLHttpRequest
→ UpdateServer PublicBaseUrl
→ POST /api/v1/admin/releases/{releaseCode}/artifacts
```

다음 경로는 사용하지 않는다.

```text
Browser ZIP
→ Blazor SignalR
→ AdminWeb 서버 메모리/임시파일
→ UpdateServer
```

.NET으로 전달되는 값은 다음 메타데이터뿐이다.

- 파일명
- 파일 크기
- MIME 표시값
- 진행 byte와 percent
- HTTP 상태
- ErrorCode
- Request ID
- 업로드 결과 Artifact 메타데이터

ZIP 파일 byte는 .NET 또는 SignalR로 전달하지 않는다.

## multipart 계약

```text
POST {PublicBaseUrl}/api/v1/admin/releases/{releaseCode}/artifacts
Authorization: Bearer {AccountToken}
X-Request-ID: {32-character-guid}
Content-Type: multipart/form-data; boundary=browser-generated

os=windows
architecture=x86|x64|any
packageType=full
file={browser File}
```

Content-Type boundary는 browser가 생성하므로 JavaScript에서 직접 설정하지 않는다.

## 중복·상태 차단

- 기존 활성 Artifact와 OS·Architecture·PackageType이 같으면 교체 확인
- 취소하면 요청을 시작하지 않음
- 업로드 중 Draft 저장 차단
- 업로드 중 Draft 삭제 차단
- 업로드 중 게시 차단
- 업로드 중 내부 페이지 이동 시 확인
- 업로드 중 외부 이탈 시 browser 경고
- Published·Disabled에서는 업로드 패널 미표시
- 서버 상태가 동시에 변경된 경우 409 처리

## 오류 처리

### 401 또는 5001·5003·5004

- 접근 캐시 제거
- sessionStorage 로그인 상태 제거
- `/login` 이동

### 403 또는 7001

- 로그인 유지
- 접근 캐시 제거
- 상세 화면 접근 차단

### 409 또는 8022

- 동일 Target 또는 동시 상태 변경 안내
- 상세정보 재확인 유도

### 413 또는 8031

- 파일 크기 초과

### 415 또는 8030

- 유효하지 않은 ZIP

### 500 또는 8032·9999

- 안전한 서버 오류 메시지
- 원본 응답 HTML·Stack Trace 미표시

### 503 또는 9003

- AuthServer 관리자 권한 확인 장애
- 로그인 상태 유지

### 사용자 취소

- 네트워크 오류와 별도 메시지

### 네트워크·CORS 오류

- 서버 오류와 별도 메시지

C04에서 C02·C03의 중복 오류 처리를 공통 Handler로 통합한다.

## 금지 사항 확인

다음 API와 구조는 사용하지 않았다.

- `IBrowserFile`
- `OpenReadStream`
- `MultipartFormDataContent`
- AdminWeb 임시파일
- AdminWeb 서버 업로드 중계
- Token Query String
- AllowAnyOrigin

UpdateServer CORS는 B09에서 설정된 정확한 AdminWeb Origin, Authorization, Content-Type, X-Request-ID만 사용한다.

## 자동 테스트 수

C02 완료 기준:

- AdminWeb.Tests 39개
- AuthServer.Tests 25개
- 전체 64개

C03 추가 테스트:

- ArtifactUploadInteropPolicyTests 22개

예상 결과:

- AdminWeb.Tests 61개
- AuthServer.Tests 25개
- 전체 86개

## 로컬 검증 명령

```powershell
cd D:\_work\poscam

git switch feature/update-server-auth-contract
git pull

dotnet restore poscam.sln
dotnet build poscam.sln -c Release
dotnet test poscam.sln -c Release --no-build
```

## 로컬 검증 기준

- Restore 성공
- AuthServer Release 빌드 성공
- AdminWeb Release 빌드 성공
- AuthServer.Tests Release 빌드 성공
- AdminWeb.Tests Release 빌드 성공
- 컴파일 오류 0
- C03 신규 경고 0
- 기존 AdminWeb 미사용 필드 경고 6개 이하
- AdminWeb.Tests 61/61 성공
- AuthServer.Tests 25/25 성공
- 전체 86/86 성공
- 실패 0
- 건너뜀 0

## Browser Network 검증

실제 UpdateServer와 Draft Release를 사용할 수 있는 환경에서 다음을 확인한다.

1. Draft Release 상세에서 ZIP 선택
2. Browser Developer Tools의 Network 탭 열기
3. ZIP 업로드 시작
4. Request URL이 UpdateServer `PublicBaseUrl`인지 확인
5. Request Headers에 Authorization Bearer가 있는지 확인
6. Query String에 Token이 없는지 확인
7. Form Data가 `os`, `architecture`, `packageType`, `file`인지 확인
8. AdminWeb 또는 Blazor SignalR 요청에 ZIP payload가 없는지 확인
9. 진행률 표시 확인
10. 취소 동작 확인
11. 업로드 중 저장·삭제·게시 차단 확인
12. 성공 후 Artifact 목록 자동 갱신 확인
13. 동일 Target 재업로드 시 교체 확인과 `replaced=true` 결과 확인

Browser Network 실제 검증은 UpdateServer·DB·Storage 실행 환경이 필요하다. 해당 환경이 아직 없다면 C03은 코드·빌드 검증 완료 후에도 Network 검증 대기로 유지하며 C05 전에 반드시 수행한다.

## 남은 문제

- 컴파일 오류: 로컬 검증 필요
- 실제 동작 오류: Browser Network 검증 필요
- 불필요한 중복: 외부 이탈 경고는 NavigationLock 하나로 통일
- 다음 단계 선행조건: Release 빌드·전체 테스트와 Browser Network 검증 성공 후 C04 시작

## 정책 이탈 여부

- Release 상세 연결과 자동 테스트는 C00 분석에서 사전 보고한 필수 추가 범위다.
- 감사 화면과 공통 오류 Handler는 추가하지 않았다.
- 파일 byte는 AdminWeb과 SignalR을 통과하지 않는다.
- 그 외 정책 이탈 없음.
