using poscam.AdminWeb.Models.Navigation;

namespace poscam.AdminWeb.Services;

public static class MenuConfiguration
{
    public static List<MenuItem> GetMenus()
    {
        return new List<MenuItem>
        {
            new MenuItem
            {
                Key = "Home",
                Title = "Home",
                Url = "",
                Order = 1
            },
            new MenuItem
            {
                Key = "stores",
                Title = "매장관리",
                Order = 10,
                Roles = new List<int>
                {
                    CurrentUserAccessPolicy.SystemRole,
                    CurrentUserAccessPolicy.AdminRole,
                    CurrentUserAccessPolicy.PartnerUserRole
                },
                RequiredPermissionCode = CurrentUserAccessPolicy.StoreManagePermissionCode,
                Children = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Key = "store-list",
                        Title = "매장 목록",
                        Url = "stores",
                        Order = 1,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole,
                            CurrentUserAccessPolicy.PartnerUserRole
                        },
                        RequiredPermissionCode = CurrentUserAccessPolicy.StoreManagePermissionCode
                    }
                }
            },
            new MenuItem
            {
                Key = "partners",
                Title = "파트너관리",
                Order = 20,
                Children = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Key = "partner-list",
                        Title = "파트너사",
                        Url = "partners",
                        Order = 1
                    },
                    new MenuItem
                    {
                        Key = "user-list",
                        Title = "담당자",
                        Url = "users",
                        Order = 2,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole,
                            CurrentUserAccessPolicy.PartnerUserRole
                        },
                        RequiredPermissionCode = CurrentUserAccessPolicy.PartnerUserManagePermissionCode
                    },
                    new MenuItem
                    {
                        Key = "partner-user-permissions",
                        Title = "담당자 권한",
                        Url = "users/permissions",
                        Order = 3,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole
                        },
                        RequiredPermissionCode = CurrentUserAccessPolicy.PartnerUserManagePermissionCode
                    }
                }
            },
            new MenuItem
            {
                Key = "settlements",
                Title = "정산관리",
                Order = 30,
                Children = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Key = "price-policies",
                        Title = "파트너사 단가관리",
                        Url = "settlements/price-policies",
                        Order = 1
                    },
                    new MenuItem
                    {
                        Key = "contract-charges",
                        Title = "월별 계약 청구내역",
                        Url = "settlements/contract-charges",
                        Order = 2
                    },
                    new MenuItem
                    {
                        Key = "partner-monthly",
                        Title = "파트너사별 월 정산",
                        Url = "settlements/partner-monthly",
                        Order = 3
                    },
                    new MenuItem
                    {
                        Key = "payments",
                        Title = "납부 처리",
                        Url = "settlements/payments",
                        Order = 4
                    }
                }
            },
            new MenuItem
            {
                Key = "system",
                Title = "시스템관리",
                Order = 40,
                Children = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Key = "admin-accounts",
                        Title = "관리자 계정",
                        Url = "admin/accounts",
                        Order = 1
                    }
                }
            },
            new MenuItem
            {
                Key = "updates",
                Title = "업데이트관리",
                Order = 50,
                Roles = new List<int>
                {
                    CurrentUserAccessPolicy.SystemRole,
                    CurrentUserAccessPolicy.AdminRole
                },
                RequiredPermissionCode = CurrentUserAccessPolicy.UpdateManagePermissionCode,
                Children = new List<MenuItem>
                {
                    new MenuItem
                    {
                        Key = "update-releases",
                        Title = "릴리스 관리",
                        Url = "updates/releases",
                        Order = 1,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole
                        },
                        RequiredPermissionCode = CurrentUserAccessPolicy.UpdateManagePermissionCode
                    },
                    new MenuItem
                    {
                        Key = "update-audit-logs",
                        Title = "감사 로그",
                        Url = "updates/audit-logs",
                        Order = 2,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole
                        },
                        RequiredPermissionCode = CurrentUserAccessPolicy.UpdateManagePermissionCode
                    }
                }
            }
        };
    }
}
