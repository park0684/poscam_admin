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

/// <summary>
/// 관리자 조회 화면용 NVR 정보.
/// NVR 비밀번호 원문은 전달받지 않고 저장 여부만 표시한다.
/// </summary>
public class ManageNvrConfigDto
{
    public int NvrNo { get; set; }

    public int NvrProvider { get; set; }

    public string NvrId { get; set; } = "";

    public bool HasPassword { get; set; }

    public string NvrIp { get; set; } = "";

    public int NvrPort { get; set; }

    public int NvrRtspPort { get; set; } = 554;

    public int? NvrChannels { get; set; }

    public string? NvrVersion { get; set; }
}

/// <summary>
/// 계산대/화면별 NVR 채널 매핑.
/// </summary>
public class ChannelConfigDto
{
    public int PosNo { get; set; }

    public int NvrNo { get; set; }

    public int ChannelNo { get; set; }

    public int Screen { get; set; }
}