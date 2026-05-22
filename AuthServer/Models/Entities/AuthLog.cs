namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 인증 요청 로그 Entity.
/// DB 테이블: auth_logs
/// 
/// PC 캠 인증, 캠뷰어 로그인, 하트비트, 인증 실패 등을 기록한다.
/// 운영 중 인증 실패 원인 추적과 장애 분석에 사용한다.
/// </summary>
public class AuthLog
{
    /// <summary>
    /// 인증 로그 고유 ID.
    /// DB 컬럼: al_id
    /// </summary>
    public long AlId { get; set; }

    /// <summary>
    /// 인증 요청 유형.
    /// AuthRequestType enum 값과 매칭된다.
    /// 예: PC 캠 최초 인증, PC 캠 실행 인증, 캠뷰어 로그인 등
    /// DB 컬럼: al_request
    /// </summary>
    public int AlRequest { get; set; }

    /// <summary>
    /// 요청 매장 코드.
    /// 매장 없는 계약의 인증 로그는 null 가능.
    /// DB 컬럼: al_store
    /// </summary>
    public int? AlStore { get; set; }

    /// <summary>
    /// 인증 결과.
    /// AuthResult enum 값과 매칭된다.
    /// 예: 0=실패, 1=성공
    /// DB 컬럼: al_result
    /// </summary>
    public int AlResult { get; set; }

    /// <summary>
    /// 오류 코드.
    /// AuthErrorCode enum 값과 매칭된다.
    /// 성공 시 null 또는 0 처리 가능.
    /// DB 컬럼: al_error
    /// </summary>
    public int? AlError { get; set; }

    /// <summary>
    /// 요청 IP.
    /// 관리자 페이지나 운영 로그에서 문제 추적에 사용한다.
    /// DB 컬럼: al_ip
    /// </summary>
    public string? AlIp { get; set; }

    /// <summary>
    /// 상세 로그.
    /// 프로그램 버전, HWID, 실패 사유, 요청 데이터 일부 등을 JSON 문자열로 저장할 수 있다.
    /// DB 컬럼: al_details
    /// </summary>
    public string? AlDetails { get; set; }

    /// <summary>
    /// 로그 등록일.
    /// DB 컬럼: al_rdate
    /// </summary>
    public DateTime AlRDate { get; set; }
}