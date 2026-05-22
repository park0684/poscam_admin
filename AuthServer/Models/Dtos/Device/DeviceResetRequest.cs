namespace poscam.AuthServer.Models.Dtos.Device;

/// <summary>
/// 관리자 장비 초기화 요청 DTO.
/// 
/// PC 캠 장비 초기화는 관리자만 수행하는 기준이다.
/// 캠뷰어 장비는 관리자 또는 사용자 해제가 가능하지만,
/// 이 DTO는 관리자 장비 초기화 API에서 사용한다.
/// </summary>
public class DeviceResetRequest
{
    /// <summary>
    /// 초기화할 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 초기화 사유.
    /// 예: PC 교체, 설치 오류, 장비 변경, 고객 요청
    /// </summary>
    public string Reason { get; set; } = "";
}