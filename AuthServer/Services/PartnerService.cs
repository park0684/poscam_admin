using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Partner;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 파트너사 관리 서비스.
/// 
/// 파트너사 등록, 수정, 목록 조회, 상세 조회를 담당한다.
/// 파트너사의 역할은 여기서 고정하지 않고,
/// 매장 담당자 연결 시 assignment_role로 부여한다.
/// </summary>
public class PartnerService
{
    private readonly PartnerRepository _partnerRepository;
    private readonly AdminPermissionService _adminPermissionService;

    public PartnerService(PartnerRepository partnerRepository, AdminPermissionService adminPermissionService)
    {
        _partnerRepository = partnerRepository;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 파트너사 목록을 조회한다.
    /// 
    /// System 계정은 항상 허용되며,
    /// 관리자 계정은 PartnerManage 권한이 있어야 조회할 수 있다.
    /// </summary>
    public async Task<ApiResponse<List<PartnerListItemDto>>> GetListAsync(
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<PartnerListItemDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        var partners = await _partnerRepository.GetListAsync();

        return ApiResponse<List<PartnerListItemDto>>.Ok(
            partners,
            "파트너사 목록을 조회했습니다.");
    }

    /// <summary>
    /// 파트너사 상세 정보를 조회한다.
    /// 
    /// System 계정은 항상 허용되며,
    /// 관리자 계정은 PartnerManage 권한이 있어야 조회할 수 있다.
    /// </summary>
    public async Task<ApiResponse<PartnerDetailDto>> GetDetailAsync(
        int partnerCode,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<PartnerDetailDto>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (partnerCode <= 0)
        {
            return ApiResponse<PartnerDetailDto>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 코드가 올바르지 않습니다.");
        }

        var partner = await _partnerRepository.GetDetailAsync(partnerCode);

        if (partner == null)
        {
            return ApiResponse<PartnerDetailDto>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 정보를 찾을 수 없습니다.");
        }

        return ApiResponse<PartnerDetailDto>.Ok(
            partner,
            "파트너사 상세 정보를 조회했습니다.");
    }

    /// <summary>
    /// 신규 파트너사를 등록한다.
    /// 
    /// System 계정은 항상 허용되며,
    /// 관리자 계정은 PartnerManage 권한이 있어야 등록할 수 있다.
    /// </summary>
    public async Task<ApiResponse<PartnerSaveResponse>> CreateAsync(
        PartnerCreateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (string.IsNullOrWhiteSpace(request.PartnerName))
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사명을 입력해야 합니다.");
        }

        var partner = new Partner
        {
            PartnerName = request.PartnerName.Trim(),
            PartnerBizNum = request.PartnerBizNum?.Trim(),
            PartnerOwnerName = request.PartnerOwnerName?.Trim(),
            PartnerTel = request.PartnerTel?.Trim(),
            PartnerEmail = request.PartnerEmail?.Trim(),
            PartnerZipcode = request.PartnerZipcode?.Trim(),
            PartnerAddress1 = request.PartnerAddress1?.Trim(),
            PartnerAddress2 = request.PartnerAddress2?.Trim(),
            PartnerMemo = request.PartnerMemo?.Trim(),
            PartnerStatus = (int)PartnerStatus.Active
        };

        var partnerCode = await _partnerRepository.InsertAsync(partner);

        var response = new PartnerSaveResponse
        {
            PartnerCode = partnerCode,
            PartnerName = partner.PartnerName,
            Saved = true
        };

        return ApiResponse<PartnerSaveResponse>.Ok(
            response,
            "파트너사가 등록되었습니다.");
    }

    /// <summary>
    /// 파트너사 정보를 수정한다.
    /// 
    /// System 계정은 항상 허용되며,
    /// 관리자 계정은 PartnerManage 권한이 있어야 수정할 수 있다.
    /// </summary>
    public async Task<ApiResponse<PartnerSaveResponse>> UpdateAsync(
        PartnerUpdateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (request.PartnerCode <= 0)
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 코드가 올바르지 않습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.PartnerName))
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사명을 입력해야 합니다.");
        }

        var existingPartner = await _partnerRepository.GetByCodeAsync(
            request.PartnerCode);

        if (existingPartner == null)
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "수정할 파트너사 정보를 찾을 수 없습니다.");
        }

        existingPartner.PartnerName = request.PartnerName.Trim();
        existingPartner.PartnerBizNum = request.PartnerBizNum?.Trim();
        existingPartner.PartnerOwnerName = request.PartnerOwnerName?.Trim();
        existingPartner.PartnerTel = request.PartnerTel?.Trim();
        existingPartner.PartnerEmail = request.PartnerEmail?.Trim();
        existingPartner.PartnerZipcode = request.PartnerZipcode?.Trim();
        existingPartner.PartnerAddress1 = request.PartnerAddress1?.Trim();
        existingPartner.PartnerAddress2 = request.PartnerAddress2?.Trim();
        existingPartner.PartnerMemo = request.PartnerMemo?.Trim();
        existingPartner.PartnerStatus = request.PartnerStatus;

        var affected = await _partnerRepository.UpdateAsync(existingPartner);

        if (affected <= 0)
        {
            return ApiResponse<PartnerSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 정보가 수정되지 않았습니다.");
        }

        var response = new PartnerSaveResponse
        {
            PartnerCode = existingPartner.PartnerCode,
            PartnerName = existingPartner.PartnerName,
            Saved = true
        };

        return ApiResponse<PartnerSaveResponse>.Ok(
            response,
            "파트너사 정보가 수정되었습니다.");
    }
}