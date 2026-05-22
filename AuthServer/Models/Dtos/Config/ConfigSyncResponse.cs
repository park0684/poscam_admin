namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 설정 동기화 응답 DTO.
/// </summary>
public class ConfigSyncResponse
{
    public int StoreCode { get; set; }

    public string ConfigVersion { get; set; } = "";

    public int ChannelCount { get; set; }

    public bool Synced { get; set; }
}