using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 계약 등록 요청 DTO.
/// 
/// 특정 매장에 구매형, 구독형, 테스트형 계약을 등록할 때 사용한다.
/// </summary>
public class ContractCreateRequest
{
    /// <summary>
    /// 계약을 등록할 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 계약 번호.
    /// 내부 관리번호 또는 외부 계약서 번호를 사용할 수 있다.
    /// </summary>
    public string ContractNo { get; set; } = "";

    /// <summary>
    /// 계약 유형.
    /// Trial, Purchase, Subscription 중 하나.
    /// </summary>
    public ContractType ContractType { get; set; }

    /// <summary>
    /// PC 캠 허용 수량.
    /// 이 수량을 기준으로 PC 캠 라이선스 발급 및 장비 등록을 제한한다.
    /// </summary>
    public int PccamCount { get; set; }

    /// <summary>
    /// 캠뷰어 허용 수량.
    /// 캠뷰어 슬롯 제한에 사용한다.
    /// </summary>
    public int ViewerCount { get; set; }

    /// <summary>
    /// 계약 시작일.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 계약 종료일.
    /// 구매형은 null 허용 가능.
    /// </summary>
    public DateTime? EndDate { get; set; }
}