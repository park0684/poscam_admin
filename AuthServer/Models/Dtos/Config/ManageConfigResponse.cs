using poscam.AuthServer.Models.Dtos.Viewer;

namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 관리자 화면의 매장 설정 조회 응답 DTO.
/// 
/// 관리자 페이지에서는 NVR/채널 설정을 조회만 하고,
/// 수정/저장은 현장 캠뷰어에서만 수행한다.
/// </summary>
public class ManageConfigResponse
{
    public int StoreCode { get; set; }

    public string ConfigVersion { get; set; } = "";

    public ManageNvrConfigDto? NvrConfig { get; set; }

    public List<ChannelConfigDto> Channels { get; set; } = new();
}