namespace poscam.AuthServer.Models.Dtos.Common;

/// <summary>
/// 장비 목록 조회 시 사용하는 요약 DTO.
/// 
/// 캠뷰어 슬롯 초과 시 기존 장비 목록을 보여주거나,
/// 관리자 페이지에서 등록 장비 목록을 조회할 때 사용한다.
/// </summary>
public class DeviceSummaryDto
{
    /// <summary>
    /// 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 연결된 라이선스 코드.
    /// PC 캠은 값이 있고, 캠뷰어는 null일 수 있다.
    /// </summary>
    public int? LicenseCode { get; set; }

    /// <summary>
    /// 앱 유형.
    /// 1=PC 캠, 2=캠뷰어
    /// </summary>
    public int AppType { get; set; }

    /// <summary>
    /// 장비 HWID.
    /// 운영 화면에는 전체 값을 그대로 노출하지 않고 일부만 표시하는 것이 좋다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// POS 번호.
    /// 캠뷰어는 0일 수 있다.
    /// </summary>
    public int PosNo { get; set; }

    /// <summary>
    /// 장비명.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 장비 등록일.
    /// </summary>
    public DateTime? RegisteredAt { get; set; }
}