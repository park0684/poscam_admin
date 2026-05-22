namespace poscam.AdminWeb.Models.UserManage;

/// <summary>
/// 담당자 요청 유형 코드.
/// 
/// AuthServer에서 사용하는 요청 유형 코드와 동일한 값을 사용한다.
/// 화면에서는 숫자를 직접 비교하지 않고 이 enum을 기준으로 판단한다.
/// </summary>
public enum UserRequestType
{
    /// <summary>
    /// 요청 없음.
    /// </summary>
    None = 0,

    /// <summary>
    /// 가입 승인 요청.
    /// </summary>
    JoinApproval = 1,

    /// <summary>
    /// 담당자 정보 수정 요청.
    /// </summary>
    InfoUpdate = 2,

    /// <summary>
    /// 비밀번호 초기화 요청.
    /// </summary>
    PasswordReset = 3,

    /// <summary>
    /// 계정 일시중지 요청.
    /// </summary>
    Suspend = 4,

    /// <summary>
    /// 계정 정상복구 요청.
    /// </summary>
    Restore = 5,

    /// <summary>
    /// 계정 무효 처리 요청.
    /// </summary>
    Invalidate = 6,

    /// <summary>
    /// 계정 차단 요청.
    /// </summary>
    Block = 9
}

/// <summary>
/// 담당자 요청 처리 상태 코드.
/// 
/// AuthServer에서 사용하는 요청 상태 코드와 동일한 값을 사용한다.
/// </summary>
public enum UserRequestStatus
{
    /// <summary>
    /// 요청 없음.
    /// </summary>
    None = 0,

    /// <summary>
    /// 요청 대기.
    /// 관리자가 처리해야 하는 상태.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 처리 완료.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 반려.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 취소.
    /// </summary>
    Cancelled = 9
}

/// <summary>
/// 요청 처리 방식.
/// 
/// 요청처리 모달에서 어떤 동작을 해야 하는지 판단하는 용도.
/// </summary>
public enum UserRequestProcessAction
{
    /// <summary>
    /// 처리 없음.
    /// </summary>
    None = 0,

    /// <summary>
    /// 담당자 기본정보 수정 후 처리.
    /// </summary>
    EditUserInfo = 1,

    /// <summary>
    /// 비밀번호 초기화 모달 실행 후 처리.
    /// </summary>
    ResetPassword = 2,

    /// <summary>
    /// 담당자 상태 변경 후 처리.
    /// </summary>
    ChangeUserStatus = 3
}

/// <summary>
/// 담당자 요청 유형 표시 및 처리 메타 정보.
/// 
/// 화면마다 요청 유형명을 하드코딩하지 않도록 하기 위한 공통 모델.
/// </summary>
public sealed class UserRequestTypeInfo
{
    /// <summary>
    /// 요청 유형 코드.
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// 화면 표시명.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// 설명.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// 요청 처리 방식.
    /// </summary>
    public UserRequestProcessAction ProcessAction { get; init; }

    /// <summary>
    /// 상태 변경 요청일 경우 적용할 사용자 상태 값.
    /// 
    /// 예:
    /// 일시중지 요청 → 2
    /// 정상복구 요청 → 1
    /// 무효 요청 → 3
    /// 차단 요청 → 9
    /// </summary>
    public int? TargetUserStatus { get; init; }

    /// <summary>
    /// 기본 처리 완료 메모.
    /// 관리자가 별도 메모를 입력하지 않았을 때 사용한다.
    /// </summary>
    public string DefaultProcessMemo { get; init; } = "";
}

/// <summary>
/// 담당자 요청 상태 표시 정보.
/// </summary>
public sealed class UserRequestStatusInfo
{
    /// <summary>
    /// 요청 상태 코드.
    /// </summary>
    public int Code { get; init; }

    /// <summary>
    /// 화면 표시명.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Bootstrap badge class.
    /// </summary>
    public string BadgeClass { get; init; } = "badge bg-secondary";
}

/// <summary>
/// 담당자 요청 관련 공통 메타 정보 제공 클래스.
/// 
/// UserDetailPopup, 요청처리 모달, 요청내역 목록, 대시보드에서 공통으로 사용한다.
/// 나중에 AuthServer에서 메타 정보를 API로 내려받게 되면 이 클래스만 교체하면 된다.
/// </summary>
public static class UserRequestMetadata
{
    /// <summary>
    /// 담당자 요청 유형 목록.
    /// </summary>
    private static readonly List<UserRequestTypeInfo> RequestTypes = new()
    {
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.JoinApproval,
            Name = "가입승인",
            Description = "신규 담당자 가입 승인 요청",
            ProcessAction = UserRequestProcessAction.ChangeUserStatus,
            TargetUserStatus = 1,
            DefaultProcessMemo = "가입 승인 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.InfoUpdate,
            Name = "정보수정",
            Description = "담당자 기본정보 수정 요청",
            ProcessAction = UserRequestProcessAction.EditUserInfo,
            TargetUserStatus = null,
            DefaultProcessMemo = "정보 수정 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.PasswordReset,
            Name = "비밀번호초기화",
            Description = "담당자 비밀번호 초기화 요청",
            ProcessAction = UserRequestProcessAction.ResetPassword,
            TargetUserStatus = null,
            DefaultProcessMemo = "비밀번호 초기화 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.Suspend,
            Name = "일시중지",
            Description = "담당자 계정 일시중지 요청",
            ProcessAction = UserRequestProcessAction.ChangeUserStatus,
            TargetUserStatus = 2,
            DefaultProcessMemo = "일시중지 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.Restore,
            Name = "정상복구",
            Description = "담당자 계정 정상복구 요청",
            ProcessAction = UserRequestProcessAction.ChangeUserStatus,
            TargetUserStatus = 1,
            DefaultProcessMemo = "정상복구 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.Invalidate,
            Name = "무효",
            Description = "담당자 계정 무효 처리 요청",
            ProcessAction = UserRequestProcessAction.ChangeUserStatus,
            TargetUserStatus = 3,
            DefaultProcessMemo = "무효 요청 처리 완료"
        },
        new UserRequestTypeInfo
        {
            Code = (int)UserRequestType.Block,
            Name = "차단",
            Description = "담당자 계정 차단 요청",
            ProcessAction = UserRequestProcessAction.ChangeUserStatus,
            TargetUserStatus = 9,
            DefaultProcessMemo = "차단 요청 처리 완료"
        }
    };

    /// <summary>
    /// 담당자 요청 상태 목록.
    /// </summary>
    private static readonly List<UserRequestStatusInfo> RequestStatuses = new()
    {
        new UserRequestStatusInfo
        {
            Code = (int)UserRequestStatus.None,
            Name = "요청없음",
            BadgeClass = "badge bg-secondary"
        },
        new UserRequestStatusInfo
        {
            Code = (int)UserRequestStatus.Pending,
            Name = "요청대기",
            BadgeClass = "badge bg-warning text-dark"
        },
        new UserRequestStatusInfo
        {
            Code = (int)UserRequestStatus.Completed,
            Name = "처리완료",
            BadgeClass = "badge bg-success"
        },
        new UserRequestStatusInfo
        {
            Code = (int)UserRequestStatus.Rejected,
            Name = "반려",
            BadgeClass = "badge bg-danger"
        },
        new UserRequestStatusInfo
        {
            Code = (int)UserRequestStatus.Cancelled,
            Name = "취소",
            BadgeClass = "badge bg-dark"
        }
    };

    /// <summary>
    /// 전체 요청 유형 정보를 반환한다.
    /// 콤보박스나 요청내역 필터에서 사용할 수 있다.
    /// </summary>
    public static IReadOnlyList<UserRequestTypeInfo> GetRequestTypes()
    {
        return RequestTypes;
    }

    /// <summary>
    /// 전체 요청 상태 정보를 반환한다.
    /// 콤보박스나 요청내역 필터에서 사용할 수 있다.
    /// </summary>
    public static IReadOnlyList<UserRequestStatusInfo> GetRequestStatuses()
    {
        return RequestStatuses;
    }

    /// <summary>
    /// 요청 유형 코드에 해당하는 정보를 반환한다.
    /// </summary>
    public static UserRequestTypeInfo? FindRequestType(int? requestType)
    {
        if (requestType == null)
        {
            return null;
        }

        return RequestTypes.FirstOrDefault(x => x.Code == requestType.Value);
    }

    /// <summary>
    /// 요청 상태 코드에 해당하는 정보를 반환한다.
    /// </summary>
    public static UserRequestStatusInfo? FindRequestStatus(int? requestStatus)
    {
        if (requestStatus == null)
        {
            return null;
        }

        return RequestStatuses.FirstOrDefault(x => x.Code == requestStatus.Value);
    }

    /// <summary>
    /// 요청 유형명을 반환한다.
    /// </summary>
    public static string GetRequestTypeName(int? requestType)
    {
        var info = FindRequestType(requestType);

        return info == null
            ? "-"
            : info.Name;
    }

    /// <summary>
    /// 요청 상태명을 반환한다.
    /// </summary>
    public static string GetRequestStatusName(int? requestStatus)
    {
        var info = FindRequestStatus(requestStatus);

        return info == null
            ? "-"
            : info.Name;
    }

    /// <summary>
    /// 요청 상태 badge class를 반환한다.
    /// </summary>
    public static string GetRequestStatusBadgeClass(int? requestStatus)
    {
        var info = FindRequestStatus(requestStatus);

        return info == null
            ? "badge bg-secondary"
            : info.BadgeClass;
    }

    /// <summary>
    /// 처리 대기 요청인지 여부를 반환한다.
    /// </summary>
    public static bool IsPendingRequest(int? requestType, int requestStatus)
    {
        return requestType != null &&
               requestType > 0 &&
               requestStatus == (int)UserRequestStatus.Pending;
    }

    /// <summary>
    /// 요청 유형의 처리 방식을 반환한다.
    /// 
    /// 요청처리 모달에서 정보수정, 비밀번호초기화, 상태변경 중
    /// 어떤 방식으로 처리해야 하는지 판단할 때 사용한다.
    /// 알 수 없는 요청 유형이면 None을 반환한다.
    /// </summary>
    public static UserRequestProcessAction GetProcessAction(int? requestType)
    {
        var info = FindRequestType(requestType);

        return info == null
            ? UserRequestProcessAction.None
            : info.ProcessAction;
    }

    /// <summary>
    /// 상태 변경 요청일 경우 변경할 사용자 상태 값을 반환한다.
    /// 
    /// 예:
    /// 일시중지 요청 → 2
    /// 정상복구 요청 → 1
    /// 무효 요청 → 3
    /// 차단 요청 → 9
    /// 
    /// 상태 변경 요청이 아니거나 알 수 없는 요청 유형이면 null을 반환한다.
    /// </summary>
    public static int? GetTargetUserStatus(int? requestType)
    {
        var info = FindRequestType(requestType);

        return info?.TargetUserStatus;
    }

    /// <summary>
    /// 요청 처리 완료 시 사용할 기본 메모를 반환한다.
    /// 
    /// 요청 유형이 등록된 메타 정보에 있으면 해당 요청의 기본 처리 메모를 반환하고,
    /// 알 수 없는 요청 유형이면 공통 기본 문구를 반환한다.
    /// </summary>
    public static string GetDefaultProcessMemo(int? requestType)
    {
        var info = FindRequestType(requestType);

        return info == null
            ? "담당자 요청 처리 완료"
            : info.DefaultProcessMemo;
    }
}