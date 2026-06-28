namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 관리자 세부 권한 코드.
/// 
/// DB에는 숫자 값만 저장한다.
/// 권한명과 의미는 백엔드 코드에서만 관리한다.
/// </summary>
public enum AdminPermissionType
{
    /// <summary>
    /// 관리자 계정 생성/수정 권한.
    /// </summary>
    AdminAccountManage = 1,

    /// <summary>
    /// 관리자 비밀번호 초기화 권한.
    /// </summary>
    AdminPasswordReset = 2,

    /// <summary>
    /// 관리자 권한 부여/수정 권한.
    /// </summary>
    AdminPermissionManage = 3,

    /// <summary>
    /// 파트너사 등록/수정 권한.
    /// </summary>
    PartnerManage = 4,

    /// <summary>
    /// 담당자 등록/수정 권한.
    /// </summary>
    PartnerUserManage = 5,

    /// <summary>
    /// 파트너사 가격 정책 등록/수정 권한.
    /// </summary>
    PartnerPricePolicyManage = 6,

    /// <summary>
    /// 매장 등록/수정 권한.
    /// </summary>
    StoreManage = 7,

    /// <summary>
    /// 정산 처리/수정 권한.
    /// </summary>
    SettlementManage = 8,

    /// <summary>
    /// 파트너 담당자 비밀번호 초기화 권한.
    /// </summary>
    PartnerUserPasswordReset = 9,

    /// <summary>
    /// 계약 등록/수정 권한.
    /// </summary>
    ContractManage = 10,

    /// <summary>
    /// 라이선스 발급/폐기/복구 권한.
    /// </summary>
    LicenseManage = 11,

    /// <summary>
    /// 프로그램 릴리스 등록, 패키지 업로드, 게시 및 배포 중지 권한.
    /// </summary>
    UpdateManage = 12
}
