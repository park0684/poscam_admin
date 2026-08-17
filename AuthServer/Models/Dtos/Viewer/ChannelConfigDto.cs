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
    /// 채널이 속한 매장 내부 NVR 번호.
    /// ConfigSchemaVersion 2 이상에서는 필수이다.
    /// 구버전 요청에서 누락된 경우 서비스 계층에서 NVR 1로 정규화한다.
    /// </summary>
    public int NvrNo { get; set; }

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
