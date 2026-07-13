namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// POSCAM에서 지원하는 NVR 제조사 및 Provider 고정 코드.
/// DB nvr_configs.nvr_provider 및 CamViewer NvrProviderType 값과 동일해야 한다.
/// </summary>
public enum NvrProviderType
{
    /// <summary>
    /// 제조사 미지정.
    /// 신규 설정 저장값으로 사용하지 않는다.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Dahua NetSDK 직접 연결.
    /// </summary>
    Dahua = 1,

    /// <summary>
    /// TP-Link VIGI 로컬 OpenAPI 및 RTSP 직접 연결.
    /// 클라우드 계정 연동은 지원하지 않는다.
    /// </summary>
    TpLinkVigi = 2,

    /// <summary>
    /// KT Telecop 제조사 SDK 연결.
    /// </summary>
    KtTelecop = 3
}
