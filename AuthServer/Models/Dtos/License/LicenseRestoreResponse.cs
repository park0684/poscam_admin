namespace poscam.AuthServer.Models.Dtos.License;

/// <summary>
/// 인증키 복구 응답 DTO.
/// </summary>
public class LicenseRestoreResponse
{
    /// <summary>
    /// 복구된 라이선스 코드.
    /// </summary>
    public int LicenseCode { get; set; }

    /// <summary>
    /// 라이선스가 속한 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 계약과 연결된 매장 코드.
    /// 매장 없는 계약이면 null.
    /// </summary>
    public int? StoreCode { get; set; }

    /// <summary>
    /// 복구 후 라이선스 상태.
    /// 1=사용중, 2=초기화
    /// </summary>
    public int RestoredStatus { get; set; }

    /// <summary>
    /// 복구 성공 여부.
    /// </summary>
    public bool Restored { get; set; }
}