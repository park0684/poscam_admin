namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 등록 장비 목록 조회 요청 DTO.
/// 
/// 캠뷰어 슬롯 초과 시 기존 캠뷰어 장비 목록을 보여주기 위해 사용한다.
/// storeCode만으로 조회하지 않고, 매장 ID/비밀번호를 검증한 뒤 조회한다.
/// </summary>
public class ViewerDeviceListRequest
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
    /// 매장 비밀번호.
    /// </summary>
    public string StorePassword { get; set; } = "";
}