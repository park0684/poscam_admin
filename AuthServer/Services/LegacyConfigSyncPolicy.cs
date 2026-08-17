using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Services;

/// <summary>
/// 구버전(ConfigSchemaVersion &lt; 2) CamViewer가 안전하게 표현할 수 있는
/// 설정인지 판정하는 호환 정책이다.
///
/// legacy CamViewer가 정확히 표현할 수 있는 설정은 다음뿐이다.
/// - NVR이 정확히 1대
/// - 그 NVR의 번호가 1
/// - 모든 채널이 NVR 1을 참조
///
/// NVR이 한 대뿐이어도 NvrNo=2처럼 번호가 1이 아니면 Schema 2가 필요하다.
/// 그렇지 않으면 구버전 CamViewer가 NVR 번호를 잃고 NVR 1로 다시 저장할 수 있다.
/// </summary>
public static class LegacyConfigSyncPolicy
{
    private const int LegacyNvrNo = 1;

    /// <summary>
    /// 서버 설정이 legacy 단일 NVR 계약으로 손실 없이 표현 가능한지 확인한다.
    /// </summary>
    public static bool IsLegacyRepresentable(
        ViewerConfigResponse? config)
    {
        if (config == null || config.Nvrs == null || config.Nvrs.Count != 1)
        {
            return false;
        }

        if (config.Nvrs[0] == null ||
            config.Nvrs[0].NvrNo != LegacyNvrNo)
        {
            return false;
        }

        if (config.Channels == null)
        {
            return true;
        }

        return config.Channels.All(
            channel => channel != null && channel.NvrNo == LegacyNvrNo);
    }

    /// <summary>
    /// legacy 설정 업로드가 실제 write 단계로 계속 진행해도 되는지 확인한다.
    ///
    /// 기존 설정이 없으면 최초 단일 NVR 업로드를 허용하고,
    /// 기존 설정이 있으면 legacy로 손실 없이 표현 가능한 경우에만 허용한다.
    /// </summary>
    public static bool CanContinue(
        ApiResponse<ViewerConfigResponse>? existingConfigCheck)
    {
        if (existingConfigCheck == null)
        {
            return false;
        }

        if (existingConfigCheck.Success)
        {
            return IsLegacyRepresentable(existingConfigCheck.Data);
        }

        return existingConfigCheck.ErrorCode == AuthErrorCode.NvrConfigNotFound;
    }
}
