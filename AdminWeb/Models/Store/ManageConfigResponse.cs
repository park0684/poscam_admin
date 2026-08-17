namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 관리자 화면의 매장 NVR/채널 설정 조회 응답 DTO.
/// AuthServer의 ManageConfigResponse와 구조를 맞춘다.
/// </summary>
public class ManageConfigResponse
{
    public int StoreCode { get; set; }

    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 신규 다중 NVR 조회 목록.
    /// </summary>
    public List<ManageNvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 기존 단일 NVR 응답 호환용.
    /// Nvrs가 비어 있을 때 화면 fallback으로 사용한다.
    /// </summary>
    public ManageNvrConfigDto? NvrConfig { get; set; }

    public List<ChannelConfigDto> Channels { get; set; } = new();
}