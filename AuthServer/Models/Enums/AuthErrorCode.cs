namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 인증, 계약, 라이선스, 장비, 설정, 권한 처리 과정에서 사용하는 오류 코드.
///
/// 코드 구간:
/// - 1000번대: 매장
/// - 2000번대: 계약
/// - 3000번대: 라이선스
/// - 4000번대: 장비
/// - 5000번대: 인증/토큰
/// - 6000번대: 설정
/// - 7000번대: 권한
/// - 9000번대: 공통 시스템 오류
/// </summary>
public enum AuthErrorCode
{
    /// <summary>
    /// 오류 없음.
    /// 정상 처리 상태.
    /// </summary>
    None = 0,

    #region Store - 1000

    /// <summary>
    /// 유효하지 않은 매장 정보.
    /// 매장 코드가 존재하지 않거나 조회할 수 없는 경우.
    /// </summary>
    InvalidStore = 1001,

    /// <summary>
    /// 사용 불가능한 매장 상태.
    /// 매장이 정지, 종료, 비활성 상태인 경우.
    /// </summary>
    StoreInactive = 1002,

    #endregion

    #region Contract - 2000

    /// <summary>
    /// 계약 정보를 찾을 수 없음.
    /// 인증 또는 라이선스 처리 대상 계약이 존재하지 않는 경우.
    /// </summary>
    ContractNotFound = 2001,

    /// <summary>
    /// 사용 불가능한 계약 상태.
    /// 계약이 비활성, 취소, 종료 등으로 처리되어 있는 경우.
    /// </summary>
    ContractInactive = 2002,

    /// <summary>
    /// 계약 기간이 만료됨.
    /// 계약 종료일이 지나 더 이상 사용할 수 없는 경우.
    /// </summary>
    ContractExpired = 2003,

    /// <summary>
    /// 계약상 허용된 사용 수량을 초과함.
    /// 예: 계약된 장비 수 또는 라이선스 슬롯 수를 초과한 경우.
    /// </summary>
    ContractSlotExceeded = 2004,

    #endregion

    #region License - 3000

    /// <summary>
    /// 라이선스 정보를 찾을 수 없음.
    /// 입력된 인증키 또는 라이선스 코드가 존재하지 않는 경우.
    /// </summary>
    LicenseNotFound = 3001,

    /// <summary>
    /// 이미 사용 중인 라이선스.
    /// 기존 장비에 연결되어 있어 신규 활성화에 사용할 수 없는 경우.
    /// </summary>
    LicenseAlreadyUsed = 3002,

    /// <summary>
    /// 회수 또는 폐기된 라이선스.
    /// 더 이상 사용할 수 없도록 처리된 인증키인 경우.
    /// </summary>
    LicenseRevoked = 3003,

    /// <summary>
    /// 라이선스와 계약 정보가 일치하지 않음.
    /// 해당 라이선스가 요청된 계약 또는 사용 범위와 맞지 않는 경우.
    /// </summary>
    LicenseContractMismatch = 3004,

    /// <summary>
    /// 라이선스 형식이 올바르지 않음.
    /// 인증키 길이, 패턴, 접두어 등 기본 형식 검증에 실패한 경우.
    /// </summary>
    InvalidLicenseFormat = 3005,

    #endregion

    #region Device - 4000

    /// <summary>
    /// 장비 정보를 찾을 수 없음.
    /// 요청된 디바이스 또는 등록 장비가 존재하지 않는 경우.
    /// </summary>
    DeviceNotFound = 4001,

    /// <summary>
    /// 이미 등록된 장비.
    /// 동일 장비가 중복으로 등록 요청된 경우.
    /// </summary>
    DeviceAlreadyRegistered = 4002,

    /// <summary>
    /// 동일한 포스번호가 이미 등록되어 있음.
    /// 하나의 사용 범위 안에서 중복된 POS 번호를 등록하려는 경우.
    /// </summary>
    DuplicatePosNo = 4003,

    /// <summary>
    /// 동일한 HWID가 이미 등록되어 있음.
    /// 기존에 다른 등록 이력이 있는 장비 식별값을 다시 사용하려는 경우.
    /// </summary>
    DuplicateHwid = 4004,

    /// <summary>
    /// 허용된 장비 등록 수를 초과함.
    /// 계약 또는 라이선스 기준 장비 수량 제한을 넘은 경우.
    /// </summary>
    DeviceLimitExceeded = 4005,

    /// <summary>
    /// 요청 장비의 HWID가 기존 등록 정보와 일치하지 않음.
    /// 토큰 갱신, 재인증, 장비 검증 과정에서 불일치가 발생한 경우.
    /// </summary>
    HwidMismatch = 4006,

    #endregion

    #region Authentication / Token - 5000

    /// <summary>
    /// 로그인 정보가 올바르지 않음.
    /// 아이디가 없거나 로그인 대상이 유효하지 않은 경우.
    /// </summary>
    InvalidLogin = 5001,

    /// <summary>
    /// 비밀번호가 올바르지 않음.
    /// 입력된 비밀번호와 저장된 해시값이 일치하지 않는 경우.
    /// </summary>
    InvalidPassword = 5002,

    /// <summary>
    /// 토큰이 만료됨.
    /// 유효기간이 지난 토큰으로 인증을 시도한 경우.
    /// </summary>
    TokenExpired = 5003,

    /// <summary>
    /// 토큰이 유효하지 않음.
    /// 형식 오류, 위변조 의심, 복호화 실패 등으로 사용할 수 없는 경우.
    /// </summary>
    TokenInvalid = 5004,

    /// <summary>
    /// 오프라인 허용 기간이 만료됨.
    /// 서버 재인증 없이 사용할 수 있는 허용 기간을 초과한 경우.
    /// </summary>
    OfflineExpired = 5005,

    /// <summary>
    /// 시스템 시간 조작이 의심됨.
    /// 이전 인증 시각보다 비정상적으로 과거 시간이 감지되는 경우 등.
    /// </summary>
    TimeManipulationSuspected = 5006,

    #endregion

    #region Configuration - 6000

    /// <summary>
    /// NVR 설정 정보를 찾을 수 없음.
    /// 매장 또는 사용자에게 필요한 NVR 기본 설정이 존재하지 않는 경우.
    /// </summary>
    NvrConfigNotFound = 6001,

    /// <summary>
    /// 채널 설정 정보를 찾을 수 없음.
    /// CCTV 채널, POS 화면 채널 등 세부 채널 설정이 없는 경우.
    /// </summary>
    ChannelConfigNotFound = 6002,

    /// <summary>
    /// 설정 버전 충돌이 발생함.
    /// 서버와 클라이언트가 기준으로 삼는 설정 버전이 서로 맞지 않는 경우.
    /// </summary>
    ConfigVersionConflict = 6003,

    /// <summary>
    /// 클라이언트 설정 스키마가 서버의 매장 설정을 처리할 수 없음.
    /// 예: 다중 NVR 매장을 단일 NVR 스키마의 구버전 CamViewer가 조회하려는 경우.
    /// </summary>
    ConfigSchemaNotSupported = 6004,

    #endregion

    #region Permission - 7000

    /// <summary>
    /// 기능 실행 권한이 없음.
    /// System 전용 기능을 일반 계정이 호출하거나,
    /// 관리자 계정이 필요한 세부 권한을 보유하지 않은 경우.
    /// </summary>
    PermissionDenied = 7001,

    #endregion

    #region Common - 9000

    /// <summary>
    /// 요청값 검증 실패.
    /// 필수값 누락, 허용되지 않는 값, 잘못된 상태 전이 요청 등.
    /// </summary>
    ValidationError = 9001,

    /// <summary>
    /// 데이터베이스 처리 중 오류가 발생함.
    /// 조회, 저장, 수정, 삭제 작업 중 예외가 발생한 경우.
    /// </summary>
    DatabaseError = 9002,

    /// <summary>
    /// 정의되지 않은 알 수 없는 오류.
    /// 예상하지 못한 예외가 발생한 경우.
    /// </summary>
    UnknownError = 9999

    #endregion
}
