namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어에 전달할 채널 매핑 DTO.
/// </summary>
public class ChannelConfigDto
{
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