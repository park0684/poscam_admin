namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 로그인 요청 DTO.
/// 
/// 사용자가 입력하는 매장코드는 stores.store_code가 아니라
/// stores.store_id 값이다.
/// </summary>
public class ViewerLoginRequest
{
    /// <summary>
    /// 매장 로그인 ID.
    /// 화면에서는 "매장코드"로 표시할 수 있지만,
    /// DB 기준으로는 stores.store_id에 해당한다.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장 로그인 비밀번호.
    /// </summary>
    public string StorePassword { get; set; } = "";

    /// <summary>
    /// 현재 캠뷰어 PC의 HWID.
    /// 슬롯 등록 및 재인증 기준으로 사용한다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 장비명.
    /// 예: 점주 노트북, 사무실 PC.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 캠뷰어 프로그램 버전.
    /// </summary>
    public string? ProgramVersion { get; set; }
}