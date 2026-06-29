# poscam_admin UpdateServer 연동 상태

| ID | 작업 | 상태 | 빌드 | 테스트 | 변경 파일 | 비고 |
|---|---|---|---|---|---|---|
| A00 | 저장소 분석 | Completed | Release 성공 (경고 0, 오류 0) | 테스트 프로젝트 없음 | codex/WORK_STATUS.md | A01 선행: 설정 비밀 제거, TokenExpired=5003, 역할 주석 수정 |
| A01 | 보안 설정·토큰 오류 | Completed | Release 성공 (경고 0, 오류 0) | 2개 통과 | AuthServer 설정·토큰·주석, AuthServer.Tests, 운영 Secret 문서, poscam.sln, WORK_STATUS | placeholder 적용, TokenExpired=5003 |
| A02 | UpdateManage=12 | Completed | Release 성공 (경고 6, 오류 0) | 3개 통과 | AdminPermissionType, AdminWeb 권한 목록, 권한 테스트, WORK_STATUS | 1~11 유지, UpdateManage=12 반영 |
| A03 | 현재 접근정보 API | Completed | Release 성공 (경고 0, 오류 0) | 7개 통과 | 접근정보 DTO·Controller·Service·Repository 계약·테스트, WORK_STATUS | 자기조회, 역할별 권한, 401/500 반영 |
| A04 | 내부 권한 API | Completed | Release 성공 (경고 0, 오류 0) | 14개 통과 | 서비스 키 설정, 내부 권한 Controller·Actor DTO·Helper·테스트, 운영 Secret 문서, WORK_STATUS | 키·Bearer 401, 권한 403, UpdateManage 고정 |
| A05 | AuthServer 검증 | Completed | Release 성공 (경고 0, 오류 0) | 25개 통과 | 위변조 토큰 결함 수정, 내부 API·계약 검증 테스트, WORK_STATUS | Secret 검색 이상 없음, B05 선행 가능 |
| C00 | AdminWeb 분석 | Completed | Release 성공 (경고 6, 오류 0) | 코드 테스트 없음 | codex/reports/C00_ADMINWEB_ANALYSIS.md, codex/reports/C00_BUILD_VERIFICATION.md, WORK_STATUS | 분석·로컬 검증 완료, 경고 6개는 기존 AdminWeb 미사용 필드 |
| C01 | 접근정보·메뉴 | Completed | Release 성공 (경고 0, 오류 0) | AdminWeb.Tests 19/19, 전체 44/44 성공 | 접근정보 DTO·Scoped 캐시·메뉴 필터·NavMenu·DI·AdminWeb.Tests·C01 보고서 | 사용자 로컬 검증 완료 |
| C02 | 릴리스 화면 | InProgress | 로컬 검증 필요 | AdminWeb.Tests 39개, 전체 64개 예상 | UpdateApiSettings·UpdateApiClient·DTO·Guard·릴리스 목록·신규·상세·UI 정책 테스트·C02 보고서 | 구현 완료, Release 빌드·전체 테스트 확인 후 Completed |
| C03 | 직접 업로드 | Pending | - | - | - | C02 완료 후 |
| C04 | 감사·오류 | Pending | - | - | - | - |
| C05 | AdminWeb 검증 | Pending | - | - | - | - |

상태: Pending / InProgress / Completed / Blocked
