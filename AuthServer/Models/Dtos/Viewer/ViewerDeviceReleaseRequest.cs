namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 사용자 장비 해제 요청 DTO.
/// 
/// 캠뷰어는 PC CAM과 달리 사용자 또는 매장 관리자가
/// 로그인 후 기존 장비를 직접 해제할 수 있다.
/// </summary>
public class ViewerDeviceReleaseRequest
{
    /// <summary>
    /// 해제할 캠뷰어 장비 코드.
    /// 장비 목록 조회 API에서 받은 DeviceCode를 사용한다.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 매장 로그인 ID.
    /// 장비 해제 권한 확인에 사용한다.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장 로그인 비밀번호.
    /// </summary>
    public string StorePassword { get; set; } = "";

    /// <summary>
    /// 해제 사유.
    /// 예: PC 교체, 노트북 변경, 사용 안 함.
    /// </summary>
    public string Reason { get; set; } = "";
}