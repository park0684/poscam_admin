namespace poscam.AdminWeb.Models.Contract;

/// <summary>
/// 계약 등록/수정 요청 DTO.
/// AuthServer의 ContractSaveRequest와 구조를 맞춘다.
/// </summary>
public class ContractSaveRequest
{
    /// <summary>
    /// 계약 코드.
    /// 신규 등록 시 null.
    /// </summary>
    public int? ContractCode { get; set; }

    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 계약 유형.
    /// 1=테스트형, 2=구매형, 3=구독형
    /// </summary>
    public int ContractType { get; set; } = 1;

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
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 계약 종료일.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 계약 상태.
    /// 1=정상, 0=비활성, 2=일시중지, 9=종료/차단
    /// </summary>
    public int? Status { get; set; } = 1;
}