namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 인증 요청 유형.
/// auth_logs.al_request 값과 매칭된다.
/// </summary>
public enum AuthRequestType
{
    PccamActivate = 10,
    PccamVerify = 11,
    PccamHeartbeat = 12,

    ViewerLogin = 20,
    ViewerTokenVerify = 21,
    ViewerConfigDownload = 22,
    ViewerDeviceRelease = 23,

    AdminDeviceReset = 90,
    AdminLicenseIssue = 91
}
