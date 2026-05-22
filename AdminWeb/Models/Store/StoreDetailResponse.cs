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
    public string NvrId { get; set; } = "";

    public bool HasPassword { get; set; }

    public string NvrIp { get; set; } = "";

    public int NvrPort { get; set; }

    public int NvrChannels { get; set; }

    public string? NvrVersion { get; set; }
}

public class ChannelConfigDto
{
    public int PosNo { get; set; }

    public int ChannelNo { get; set; }

    public int Screen { get; set; }
}