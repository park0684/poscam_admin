namespace poscam.AdminWeb.Models.UserManage

/// <summary>
/// 사용자 비밀번호 변경/초기화 DTO.
///
/// 현재 정책상 관리자의 비밀번호 초기화에 사용한다.
/// </summary>
{
    public class UserPasswordChangeRequest
    {
        /// <summary>
        /// 담당자 본인이 직접 변경할 경우 사용하기 위한 필드.
        /// 현재 1차 화면에서는 사용하지 않는다.
        /// </summary>
        public string? CurrentPassword { get; set; }

        /// <summary>
        /// 새 비밀번호.
        /// 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
        /// </summary>
        public string NewPassword { get; set; } = "";

        /// <summary>
        /// 처리 메모.
        /// </summary>
        public string? Memo { get; set; }
    }

}
