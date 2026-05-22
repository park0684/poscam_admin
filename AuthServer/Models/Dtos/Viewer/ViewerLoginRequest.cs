namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 로그인 요청 DTO.
/// 
/// 캠뷰어는 PC 캠처럼 인증키에 강하게 묶지 않고,
/// 매장 계정 + HWID + 슬롯 수량 기준으로 인증한다.
/// </summary>
public class ViewerLoginRequest
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 매장 로그인 ID.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장 로그인 비밀번호.
    /// 현재는 평문 검증 기준이며, 추후 해시 검증으로 변경 가능하다.
    /// </summary>
    public string StorePassword { get; set; } = "";

    /// <summary>
    /// 현재 캠뷰어 PC의 HWID.
    /// 슬롯 등록 및 재인증 기준으로 사용한다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 장비명.
    /// 예: 점주 노트북, 사무실 PC
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 캠뷰어 프로그램 버전.
    /// </summary>
    public string? ProgramVersion { get; set; }
}