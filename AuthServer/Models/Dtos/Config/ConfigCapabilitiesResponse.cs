namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// CamViewer 설정 API가 지원하는 설정 스키마 기능을 반환한다.
///
/// 이 응답은 매장/NVR 접속정보를 포함하지 않는 정적 capability 정보다.
/// 신규 CamViewer가 구형 AuthServer에 다중 NVR 설정을 잘못 업로드하지 않도록
/// 서버 지원 여부를 쓰기 전에 확인하는 데 사용한다.
/// </summary>
public class ConfigCapabilitiesResponse
{
    /// <summary>
    /// 서버가 지원하는 최대 설정 스키마 버전.
    /// 다중 NVR 지원 버전은 2이다.
    /// </summary>
    public int MaxConfigSchemaVersion { get; set; } = 2;

    /// <summary>
    /// 다중 NVR 설정 저장/조회 지원 여부.
    /// </summary>
    public bool SupportsMultiNvr { get; set; } = true;
}
