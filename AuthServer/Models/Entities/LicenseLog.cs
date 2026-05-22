namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 라이선스 작업 로그 Entity.
/// DB 테이블: licenselog
/// 
/// 라이선스 발급, 활성화, 초기화, 폐기 등의 이력을 기록한다.
/// </summary>
public class LicenseLog
{
    /// <summary>
    /// 라이선스 로그 코드.
    /// 현재 스키마 기준 VARCHAR(20) 형태를 유지한다.
    /// 예: L202605030001
    /// DB 컬럼: lig_code
    /// </summary>
    public string LigCode { get; set; } = "";

    /// <summary>
    /// 대상 라이선스 코드.
    /// 기존 lig__licensecode 오타를 lig_license로 정리한 컬럼.
    /// DB 컬럼: lig_license
    /// </summary>
    public int LigLicense { get; set; }

    /// <summary>
    /// 대상 매장 코드.
    /// 매장과 연결되지 않은 계약의 라이선스 발급 로그는 null 가능.
    /// DB 컬럼: lig_store
    /// </summary>
    public int? LigStore { get; set; }

    /// <summary>
    /// 대상 HWID.
    /// 인증 또는 초기화 대상 장비를 추적하기 위해 저장한다.
    /// DB 컬럼: lig_hwid
    /// </summary>
    public string LigHwid { get; set; } = "";

    /// <summary>
    /// 작업 유형.
    /// LicenseActionType enum 값과 매칭된다.
    /// 예: 발급, 활성화, 초기화, 폐기, 검증, 하트비트
    /// DB 컬럼: lig_action_type
    /// </summary>
    public int LigActionType { get; set; }

    /// <summary>
    /// 작업 사유.
    /// 예: 최초 인증, 관리자 초기화, 장비 교체, 폐기 등
    /// DB 컬럼: lig_reason
    /// </summary>
    public string LigReason { get; set; } = "";

    /// <summary>
    /// 로그 등록일.
    /// DB 컬럼: lig_rdate
    /// </summary>
    public DateTime LigRDate { get; set; }
}