using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;

namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 조회 응답 DTO.
/// 
/// 관리자/담당자 화면에서 매장 상세 페이지를 구성하기 위한 데이터다.
/// NVR 설정은 조회 전용이며, 비밀번호는 노출하지 않는다.
/// </summary>
public class StoreDetailResponse
{
    /// <summary>
    /// 매장 기본정보.
    /// </summary>
    public StoreDetailDto Store { get; set; } = new();

    /// <summary>
    /// 담당자/파트너 연결 정보.
    /// </summary>
    public List<StoreAssignmentDto> Assignments { get; set; } = new();

    /// <summary>
    /// 계약 정보 목록.
    /// </summary>
    public List<StoreContractDto> Contracts { get; set; } = new();

    /// <summary>
    /// 라이선스 정보 목록.
    /// </summary>
    public List<StoreLicenseDto> Licenses { get; set; } = new();

    /// <summary>
    /// 연결 디바이스 정보.
    /// </summary>
    public StoreDeviceGroupDto Devices { get; set; } = new();

    /// <summary>
    /// 관리자 화면용 NVR 설정 목록.
    /// NVR 비밀번호는 직접 내려주지 않는다.
    /// </summary>
    public List<ManageNvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 기존 단일 NVR 상세 응답 호환용 항목.
    /// 다중 NVR 환경에서는 Nvrs의 첫 NVR을 가리킨다.
    /// </summary>
    public ManageNvrConfigDto? NvrConfig { get; set; }

    /// <summary>
    /// 채널 매핑 설정.
    /// 각 항목의 NvrNo로 Nvrs의 NVR을 참조한다.
    /// </summary>
    public List<ChannelConfigDto> ChannelConfigs { get; set; } = new();
}