namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 관리자 화면의 매장 NVR/채널 설정 조회 응답 DTO.
/// AuthServer의 ManageConfigResponse와 구조를 맞춘다.
/// </summary>
public class ManageConfigResponse
{
    public int StoreCode { get; set; }

    public string ConfigVersion { get; set; } = "";

    public ManageNvrConfigDto? NvrConfig { get; set; }

    public List<ChannelConfigDto> Channels { get; set; } = new();
}