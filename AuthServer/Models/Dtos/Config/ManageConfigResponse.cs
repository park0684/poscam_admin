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

    /// <summary>
    /// 매장 전체 설정 버전.
    /// 다중 NVR의 버전이 모두 같을 때 해당 값을 반환한다.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 매장에 등록된 전체 NVR 목록.
    /// 신규 관리자 화면은 이 목록을 기준으로 표시한다.
    /// </summary>
    public List<ManageNvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 기존 단일 NVR 관리자 화면 호환용 필드.
    /// Nvrs의 첫 번째 NVR을 반환한다.
    /// </summary>
    public ManageNvrConfigDto? NvrConfig { get; set; }

    /// <summary>
    /// 계산대/화면별 채널 매핑.
    /// 각 항목의 NvrNo로 소속 NVR을 식별한다.
    /// </summary>
    public List<ChannelConfigDto> Channels { get; set; } = new();
}