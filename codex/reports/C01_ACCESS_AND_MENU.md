# C01 접근정보·메뉴 구현 보고

## 작업 결과

- 상태: InProgress
- 접근정보 DTO·정책·서비스: 구현 완료
- 권한 기반 업데이트 메뉴: 구현 완료
- AdminWeb 테스트 프로젝트: 추가 완료
- 로컬 Release 빌드·전체 테스트: 확인 필요

## 기준 환경

- GitHub 저장소: `park0684/poscam_admin`
- 로컬 경로: `D:\_work\poscam`
- 브랜치: `feature/update-server-auth-contract`
- 솔루션: `D:\_work\poscam\poscam.sln`

## 변경 파일

### 접근정보

- `AdminWeb/Models/Account/CurrentUserAccessModels.cs`
  - AuthServer `GET /api/accounts/me/access` 데이터 DTO
  - Success, Unauthenticated, Forbidden, Failed 상태 구분
  - HTTP StatusCode, ErrorCode, 안전한 Message 보존
- `AdminWeb/Services/CurrentUserAccessPolicy.cs`
  - System(0) 전체 허용
  - Admin(1)은 UpdateManage=12 필요
  - PartnerUser(2)와 기타 역할 거부
- `AdminWeb/Services/CurrentUserAccessService.cs`
  - Bearer AccountToken으로 현재 접근정보 조회
  - 동일 Token의 성공 응답만 Scoped 메모리에 캐시
  - permissionCodes를 sessionStorage에 저장하지 않음
  - Token 변경 시 자동 캐시 폐기
  - forceRefresh, 401, 403, 로그아웃 시 캐시 무효화
  - 빈 Body, 비JSON, 네트워크 오류를 안전한 실패 결과로 변환
  - 401과 403을 구분

### 메뉴

- `AdminWeb/Models/Navigation/MenuItem.cs`
  - `RequiredPermissionCode` 추가
  - 기존 빈 `Roles` 정책은 제한 없음으로 유지
- `AdminWeb/Models/Navigation/MenuConfiguration.cs`
  - 업데이트관리 그룹 추가
  - 릴리스 관리: `updates/releases`
  - 감사 로그: `updates/audit-logs`
  - System과 Admin 역할만 후보
  - UpdateManage=12 요구
- `AdminWeb/Services/MenuAccessFilter.cs`
  - 원본 메뉴를 변경하지 않고 복사·필터
  - 접근정보가 없거나 조회 실패이면 권한 메뉴 숨김
  - 권한 있는 자식이 없는 빈 부모 그룹 숨김
  - 기존 메뉴 순서와 구조 유지
- `AdminWeb/Components/Layout/NavMenu.razor`
  - 초기 렌더에서는 권한 필요 메뉴를 숨김
  - 첫 Interactive Render 이후 접근정보 조회
  - 401: 로그인 상태와 접근 캐시 제거 후 `/login` 이동
  - 403·조회 실패: 로그인 유지, 업데이트 메뉴 숨김
  - 로그아웃 시 접근 캐시와 sessionStorage 로그인 상태 동시 제거

### DI

- `AdminWeb/Program.cs`
  - `MenuAccessFilter` Scoped 등록
  - AuthServer 접근정보용 named HttpClient 등록
  - `CurrentUserAccessService`를 명시적으로 Scoped 등록
  - 기존 AuthServer `ApiClient` 등록과 BaseAddress는 변경하지 않음

### 테스트

- `AdminWeb.Tests/poscam.AdminWeb.Tests.csproj`
- `AdminWeb.Tests/Navigation/MenuAccessFilterTests.cs`
- `AdminWeb.Tests/Services/CurrentUserAccessPolicyTests.cs`
- `poscam.sln`
  - AdminWeb.Tests 프로젝트 추가

## 권한 정책

```text
System(0)
→ permissionCodes와 무관하게 업데이트 메뉴 표시

Admin(1) + permissionCode 12
→ 업데이트 메뉴 표시

Admin(1) - permissionCode 12
→ 업데이트 메뉴 숨김

PartnerUser(2)
→ 숨김

기타 역할
→ 숨김

접근정보 조회 실패
→ 숨김
```

메뉴 숨김은 보조 UI 경계이다. 실제 보안 경계는 UpdateServer가 관리자 API 요청마다 AuthServer 내부 권한 API를 호출하는 구조다.

## Scoped 캐시 정책

- 캐시 항목: `CurrentUserAccessResponse`
- 캐시 위치: Blazor Server Scoped 서비스 메모리
- 연결 키: 현재 AccountToken
- 캐시 대상: 성공 응답만
- sessionStorage 저장: 금지
- 자동 무효화:
  - Token 변경
  - Token 없음
  - 401
  - 403
  - forceRefresh 시작
- 명시적 무효화:
  - 로그아웃
  - 향후 UpdateServer 403 공통 처리

강제 새로고침이 실패해도 이전 권한을 재사용하지 않도록 API 호출 전에 기존 캐시를 제거한다.

## Blazor 생명주기

`sessionStorage`는 JavaScript Interop이므로 `OnInitialized`에서 접근하지 않는다.

```text
OnInitialized
→ 접근정보 없이 기존 공개 메뉴만 필터
→ 업데이트 메뉴 기본 숨김

OnAfterRenderAsync(firstRender=true)
→ AccountToken 조회
→ /api/accounts/me/access 호출
→ 권한 메뉴 재구성
```

따라서 prerender 중 JS 호출 오류와 권한 없는 메뉴의 일시 노출을 방지한다.

## 테스트 범위

### 메뉴

- System 메뉴 표시
- Admin + 12 표시
- Admin - 12 숨김
- PartnerUser 숨김
- 기타 역할 숨김
- 접근정보 없음 숨김
- 빈 부모 그룹 숨김
- 기존 메뉴 순서·구조 유지
- 원본 MenuConfiguration 변경 없음

### 접근 정책·서비스

- System 권한 목록 없이 허용
- Admin 12 보유 여부 판정
- PartnerUser·기타 역할 거부
- 동일 Token 응답 1회 캐시
- Token 변경 시 재조회
- 명시적 Invalidate 후 재조회
- 401과 403 상태 구분
- Token 없음 시 API 호출 안 함

예상 AdminWeb.Tests 테스트 수: 19개

기존 AuthServer.Tests 25개를 포함한 예상 전체 테스트 수: 44개

## 검증 명령

```powershell
cd D:\_work\poscam

git switch feature/update-server-auth-contract
git pull

dotnet restore poscam.sln
dotnet build poscam.sln -c Release
dotnet test poscam.sln -c Release --no-build
```

## 검증 기준

- Restore 성공
- AuthServer Release 빌드 성공
- AdminWeb Release 빌드 성공
- AuthServer.Tests Release 빌드 성공
- AdminWeb.Tests Release 빌드 성공
- 컴파일 오류 0
- 새 경고 0
- AdminWeb.Tests 19/19 성공
- 전체 테스트 약 44개 성공
- 실패 0
- 건너뜀 0

기존 AdminWeb 미사용 필드 경고 6개는 C00에서 확인된 기존 경고이며 C01 변경으로 증가하지 않아야 한다.

## 남은 문제

- 컴파일 오류: 로컬 검증 필요
- 실제 동작 오류: 로그인 계정별 메뉴 확인은 C05 통합 검증에서 수행
- 불필요한 중복: 기존 ApiClient를 재사용하지 않고 접근정보 전용 Client로 분리
- 다음 단계 선행조건: Release 빌드와 전체 테스트 성공 후 C02 시작

## 정책 이탈 여부

- 테스트 프로젝트와 Solution 수정은 C01 메뉴 테스트 요구를 충족하기 위해 C00에서 사전 보고한 추가 범위다.
- 릴리스 화면, 직접 업로드, 감사 화면은 추가하지 않았다.
- 그 외 정책 이탈 없음.
