using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자용 설정 조회 서비스.
///
/// 관리자 페이지에서는 NVR/채널 설정을 수정하지 않고 조회만 한다.
/// 실제 설정 수정/저장은 현장 캠뷰어의 /api/config/sync 흐름에서만 수행한다.
/// </summary>
public class ConfigManageService
{
    private readonly StoreRepository _storeRepository;
    private readonly NvrConfigRepository _nvrConfigRepository;
    private readonly ChannelConfigRepository _channelConfigRepository;
    private readonly AdminPermissionService _adminPermissionService;

    public ConfigManageService(
        StoreRepository storeRepository,
        NvrConfigRepository nvrConfigRepository,
        ChannelConfigRepository channelConfigRepository,
        AdminPermissionService adminPermissionService)
    {
        _storeRepository = storeRepository;
        _nvrConfigRepository = nvrConfigRepository;
        _channelConfigRepository = channelConfigRepository;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 매장 설정 정보를 조회한다.
    ///
    /// 권한 정책:
    /// - System: 전체 매장 설정 조회 가능
    /// - Admin: StoreManage 권한이 있어야 전체 매장 설정 조회 가능
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 조회 가능
    ///
    /// 주의:
    /// - NVR 비밀번호는 직접 반환하지 않는다.
    /// - 비밀번호 존재 여부만 HasPassword로 반환한다.
    /// - 여러 NVR이 등록된 경우 NvrNo 순서대로 모두 반환한다.
    /// </summary>
    public async Task<ApiResponse<ManageConfigResponse>> GetStoreConfigAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<ManageConfigResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (storeCode <= 0)
        {
            return ApiResponse<ManageConfigResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await _adminPermissionService.CheckPermissionAsync(
                loginUser,
                AdminPermissionType.StoreManage);

            if (!permissionResult.Success)
            {
                return ApiResponse<ManageConfigResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0)
            {
                return ApiResponse<ManageConfigResponse>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 소속 파트너 정보가 없습니다.");
            }
        }
        else
        {
            return ApiResponse<ManageConfigResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "매장 설정 정보를 조회할 권한이 없습니다.");
        }

        var store = await _storeRepository.GetByCodeAsync(storeCode);

        if (store == null)
        {
            return ApiResponse<ManageConfigResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        if (loginUserRole == UserRole.PartnerUser)
        {
            var canAccess = await _storeRepository.CanPartnerAccessStoreAsync(
                loginUser.PartnerCode!.Value,
                storeCode);

            if (!canAccess)
            {
                return ApiResponse<ManageConfigResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 매장의 설정 정보를 조회할 권한이 없습니다.");
            }
        }

        var nvrConfigs = await _nvrConfigRepository.GetListByStoreAsync(storeCode);
        var channels = await _channelConfigRepository.GetByStoreAsync(storeCode);

        var manageNvrs = nvrConfigs
            .OrderBy(x => x.NvrNo)
            .Select(x => new ManageNvrConfigDto
            {
                NvrNo = x.NvrNo,
                NvrProvider = x.NvrProvider,
                NvrId = x.NvrId,
                HasPassword = !string.IsNullOrWhiteSpace(x.NvrPassword),
                NvrIp = x.NvrIp,
                NvrPort = x.NvrPort,
                NvrRtspPort = x.NvrRtspPort,
                NvrChannels = x.NvrChannels,
                NvrVersion = x.NvrVersion
            })
            .ToList();

        var distinctVersions = nvrConfigs
            .Select(x => x.NvrVersion ?? "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var response = new ManageConfigResponse
        {
            StoreCode = storeCode,
            ConfigVersion = distinctVersions.Count == 1
                ? distinctVersions[0]
                : "",
            Nvrs = manageNvrs,
            // 기존 단일 NVR 관리자 화면과의 전환기 호환용이다.
            NvrConfig = manageNvrs.FirstOrDefault(),
            Channels = channels
                .Select(x => new ChannelConfigDto
                {
                    PosNo = x.ChnPos,
                    NvrNo = x.ChnNvrNo,
                    ChannelNo = x.ChnCh,
                    Screen = x.ChnScreen
                })
                .OrderBy(x => x.PosNo)
                .ThenBy(x => x.Screen)
                .ToList()
        };

        return ApiResponse<ManageConfigResponse>.Ok(
            response,
            "매장 설정 정보를 조회했습니다.");
    }
}