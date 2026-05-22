using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "OK")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                ErrorCode = AuthErrorCode.None,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(AuthErrorCode errorCode, string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Data = default
            };
        }
    }
}
