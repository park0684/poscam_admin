namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 매장 정보를 나타내는 Entity.
/// DB 테이블: stores
/// 
/// 매장 등록, 계약 연결, 장비 등록, NVR 설정의 기준이 되는 테이블이다.
/// </summary>
public class Store
{
    /// <summary>
    /// 매장 고유 코드.
    /// DB 컬럼: store_code
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 매장 로그인 ID.
    /// 캠뷰어 로그인 시 사용한다.
    /// DB 컬럼: store_id
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장 로그인 비밀번호.
    /// 현재는 평문 저장 기준이지만, 추후 해시 방식으로 변경 예정이다.
    /// DB 컬럼: store_password
    /// </summary>
    public string StorePassword { get; set; } = "";

    /// <summary>
    /// 매장명.
    /// DB 컬럼: store_name
    /// </summary>
    public string StoreName { get; set; } = "";

    /// <summary>
    /// 사업자번호.
    /// DB 컬럼: store_biznum
    /// </summary>
    public string? StoreBizNum { get; set; }

    /// <summary>
    /// 대표자명.
    /// DB 컬럼: store_owner_name
    /// </summary>
    public string? StoreOwnerName { get; set; }

    /// <summary>
    /// 매장 연락처.
    /// DB 컬럼: store_tel
    /// </summary>
    public string? StoreTel { get; set; }

    /// <summary>
    /// 매장 이메일.
    /// DB 컬럼: store_email
    /// </summary>
    public string? StoreEmail { get; set; }

    /// <summary>
    /// 우편번호.
    /// DB 컬럼: store_zipcode
    /// </summary>
    public string? StoreZipcode { get; set; }

    /// <summary>
    /// 기본 주소.
    /// DB 컬럼: store_address1
    /// </summary>
    public string? StoreAddress1 { get; set; }

    /// <summary>
    /// 상세 주소.
    /// DB 컬럼: store_address2
    /// </summary>
    public string? StoreAddress2 { get; set; }

    /// <summary>
    /// 매장 메모.
    /// DB 컬럼: store_memo
    /// </summary>
    public string? StoreMemo { get; set; }

    /// <summary>
    /// 매장 상태값.
    /// StoreStatus enum 값과 매칭된다.
    /// DB 컬럼: store_status
    /// </summary>
    public int StoreStatus { get; set; }

    /// <summary>
    /// 매장 등록일.
    /// DB 컬럼: store_rdate
    /// </summary>
    public DateTime? StoreRDate { get; set; }

    /// <summary>
    /// 매장 수정일.
    /// DB 컬럼: store_udate
    /// </summary>
    public DateTime? StoreUDate { get; set; }
}