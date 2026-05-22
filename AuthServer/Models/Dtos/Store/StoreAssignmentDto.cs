namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 담당자 연결 정보 DTO.
/// </summary>
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