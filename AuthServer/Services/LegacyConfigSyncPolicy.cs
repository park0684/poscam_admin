using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Services;

/// <summary>
/// 구버전(ConfigSchemaVersion &lt; 2) CamViewer의 설정 업로드 보호 정책.
///
/// 기존 서버 설정 조회가 성공하면 단일 NVR 매장이므로 업로드를 허용한다.
/// 기존 설정이 아직 없는 신규 매장은 NvrConfigNotFound일 때 최초 업로드를 허용한다.
/// 그 외 다중 NVR 스키마 차단, 설정 버전 충돌, 인증 실패 등은 쓰기 전에 차단한다.
/// </summary>
public static class LegacyConfigSyncPolicy
{
    public static bool CanContinue(
        ApiResponse<ViewerConfigResponse>? existingConfigCheck)
    {
        if (existingConfigCheck == null)
        {
            return false;
        }

        return existingConfigCheck.Success ||
               existingConfigCheck.ErrorCode == AuthErrorCode.NvrConfigNotFound;
    }
}
