namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 사용자 장비 해제 응답 DTO.
/// </summary>
public class ViewerDeviceReleaseResponse
{
    /// <summary>
    /// 해제된 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 해제 성공 여부.
    /// </summary>
    public bool Released { get; set; }
}