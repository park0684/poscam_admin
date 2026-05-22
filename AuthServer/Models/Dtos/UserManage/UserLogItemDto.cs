namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 사용자 로그 목록 DTO.
/// 
/// 담당자 상세 화면에서 요청/처리 이력을 보여줄 때 사용한다.
/// </summary>
public class UserLogItemDto
{
    public int UlogCode { get; set; }

    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public int UlogType { get; set; }

    public int? UlogRequestType { get; set; }

    public int? UlogRequestStatus { get; set; }

    public int? UlogBeforeStatus { get; set; }

    public int? UlogAfterStatus { get; set; }

    public string? UlogReason { get; set; }

    public string? UlogMemo { get; set; }

    public string? UlogChangedFields { get; set; }

    public int? UlogRequestedBy { get; set; }

    public string? UlogRequestedByName { get; set; }

    public int? UlogProcessedBy { get; set; }

    public string? UlogProcessedByName { get; set; }

    public DateTime? UlogRequestedAt { get; set; }

    public DateTime? UlogProcessedAt { get; set; }

    public DateTime UlogRdate { get; set; }
}