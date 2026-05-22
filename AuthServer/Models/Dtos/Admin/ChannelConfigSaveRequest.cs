namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// POS 번호와 NVR 채널 매핑 저장 요청 DTO.
/// </summary>
public class ChannelConfigSaveRequest
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// POS 번호.
    /// </summary>
    public int PosNo { get; set; }

    /// <summary>
    /// NVR 채널 번호.
    /// </summary>
    public int ChannelNo { get; set; }

    /// <summary>
    /// 화면 위치.
    /// 예: 0=좌측, 1=우측
    /// </summary>
    public int Screen { get; set; }
}