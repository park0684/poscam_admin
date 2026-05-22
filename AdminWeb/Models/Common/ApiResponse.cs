namespace poscam.AdminWeb.Models.Common;

/// <summary>
/// AuthServer API 공통 응답 DTO.
/// 백엔드의 ApiResponse<T> 구조와 맞춘다.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = "";

    public int ErrorCode { get; set; }

    public T? Data { get; set; }
}