namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 등록 장비 목록 조회 요청 DTO.
/// 
/// 캠뷰어 슬롯 초과 시 기존 캠뷰어 장비 목록을 보여주기 위해 사용한다.
/// 사용자는 내부 매장 코드인 stores.store_code를 입력하지 않고,
/// stores.store_id와 비밀번호만 입력한다.
/// </summary>
public class ViewerDeviceListRequest
{
    /// <summary>
    /// 매장 로그인 ID.
    /// 화면에서는 매장코드로 표시할 수 있지만,
    /// DB 기준으로는 stores.store_id 값이다.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장 비밀번호.
    /// </summary>
    public string StorePassword { get; set; } = "";
}