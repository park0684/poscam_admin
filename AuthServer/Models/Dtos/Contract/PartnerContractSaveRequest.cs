using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Contract;

/// <summary>
/// 파트너사 기준 계약 등록 요청 DTO.
/// 
/// 매장과 연결되지 않은 계약을 먼저 생성할 때 사용한다.
/// 계약의 소유 주체는 Route의 partnerCode로 결정하며,
/// 요청 본문에서는 매장 코드를 받지 않는다.
/// </summary>
public class PartnerContractSaveRequest
{
    /// <summary>
    /// 계약 유형.
    /// 1=테스트형, 2=구매형, 3=구독형
    /// </summary>
    public ContractType ContractType { get; set; }

    /// <summary>
    /// PC캠 허용 수량.
    /// </summary>
    public int PccamCount { get; set; }

    /// <summary>
    /// 캠뷰어 허용 수량.
    /// </summary>
    public int ViewerCount { get; set; }

    /// <summary>
    /// 계약 시작일.
    /// 테스트형 신규 등록 시 서버 기준 오늘로 처리한다.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 계약 종료일.
    /// 테스트형 신규 등록 시 시작일 + 15일로 자동 처리한다.
    /// 구독형은 필수다.
    /// 구매형은 null 허용.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 계약 상태.
    /// null이면 Active로 처리한다.
    /// </summary>
    public int? Status { get; set; }
}