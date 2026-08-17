namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 상세 조회 응답 DTO.
/// </summary>
public class StoreDetailResponse
{
    public StoreDetailDto Store { get; set; } = new();

    public List<StoreAssignmentDto> Assignments { get; set; } = new();

    public List<StoreContractDto> Contracts { get; set; } = new();

    public List<StoreLicenseDto> Licenses { get; set; } = new();

    public StoreDeviceGroupDto Devices { get; set; } = new();

    /// <summary>
    /// 매장에 등록된 전체 NVR 목록.
    /// </summary>
    public List<ManageNvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 기존 단일 NVR 화면 호환용 첫 NVR.
    /// </summary>
    public ManageNvrConfigDto? NvrConfig { get; set; }

    public List<ChannelConfigDto> ChannelConfigs { get; set; } = new();
}

public class StoreDetailDto
{
    public int StoreCode { get; set; }

    public string StoreId { get; set; } = "";

    public string StoreName { get; set; } = "";

    public string? StoreBizNum { get; set; }

    public string? StoreOwnerName { get; set; }

    public string? StoreTel { get; set; }

    public string? StoreEmail { get; set; }

    public string? StoreZipcode { get; set; }

    public string? StoreAddress1 { get; set; }

    public string? StoreAddress2 { get; set; }

    public string? StoreMemo { get; set; }

    public int StoreStatus { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class StoreAssignmentDto
{
    public int AssignmentCode { get; set; }

    public int StoreCode { get; set; }

    public int UserCode { get; set; }

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public string? UserEmail { get; set; }

    public int? PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public string AssignmentRole { get; set; } = "";

    public bool IsPrimary { get; set; }

    public int Status { get; set; }

    public DateTime AssignedAt { get; set; }
}

public class StoreContractDto
{
    public int ContractCode { get; set; }

    public int StoreCode { get; set; }

    public string ContractNo { get; set; } = "";

    public int ContractType { get; set; }

    public int PccamCount { get; set; }

    public int ViewerCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Status { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class StoreLicenseDto
{
    public int LicenseCode { get; set; }

    public int ContractCode { get; set; }

    public string ContractNo { get; set; } = "";

    public string LicenseKey { get; set; } = "";

    public int LicenseStatus { get; set; }

    public int? RegisteredDeviceCode { get; set; }

    public string? RegisteredHwidMasked { get; set; }

    public int? PosNo { get; set; }

    public DateTime RegisteredAt { get; set; }
}

public class StoreDeviceGroupDto
{
    public List<StoreDeviceDto> Pccams { get; set; } = new();

    public List<StoreDeviceDto> Viewers { get; set; } = new();
}

public class StoreDeviceDto
{
    public int DeviceCode { get; set; }

    public int StoreCode { get; set; }

    public int? LicenseCode { get; set; }

    public int AppType { get; set; }

    public string HwidMasked { get; set; } = "";

    public int PosNo { get; set; }

    public string? DeviceName { get; set; }

    public DateTime? RegisteredAt { get; set; }
}

public class ManageNvrConfigDto
{
    /// <summary>
    /// 매장 내부 NVR 번호.
    /// 기존 단일 NVR 응답에서 값이 없으면 0으로 수신될 수 있다.
    /// </summary>
    public int NvrNo { get; set; }

    /// <summary>
    /// 고정 Provider 코드: 1=Dahua, 2=TP-Link VIGI, 3=KT Telecop.
    /// 구형 매장 상세 응답은 기존 운영값인 Dahua를 기본값으로 사용한다.
    /// </summary>
    public int NvrProvider { get; set; } = 1;

    public string NvrId { get; set; } = "";

    public bool HasPassword { get; set; }

    public string NvrIp { get; set; } = "";

    /// <summary>
    /// SDK 또는 로컬 OpenAPI 제어 포트.
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// 영상 재생용 RTSP 포트.
    /// </summary>
    public int NvrRtspPort { get; set; } = 554;

    public int? NvrChannels { get; set; }

    public string? NvrVersion { get; set; }
}

public class ChannelConfigDto
{
    public int PosNo { get; set; }

    /// <summary>
    /// 채널이 속한 매장 내부 NVR 번호.
    /// </summary>
    public int NvrNo { get; set; }

    public int ChannelNo { get; set; }

    public int Screen { get; set; }
}
