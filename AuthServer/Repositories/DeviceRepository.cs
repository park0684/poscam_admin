using System.Data;
using Dapper;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Dtos.Store;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// devices 테이블 접근 Repository.
/// 
/// PC 캠 장비와 캠뷰어 장비를 모두 관리한다.
/// 장비 유형은 dev_apptype으로 구분한다.
/// </summary>
public class DeviceRepository : RepositoryBase
{
    public DeviceRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 장비 코드로 장비를 조회한다.
    /// </summary>
    public async Task<Device?> GetByCodeAsync(int deviceCode)
    {
        const string sql = @"
        SELECT
            dev_code    AS DevCode,
            dev_store   AS DevStore,
            dev_license AS DevLicense,
            dev_apptype AS DevAppType,
            dev_hwid    AS DevHwid,
            dev_pos     AS DevPos,
            dev_name    AS DevName,
            dev_rdate   AS DevRDate
        FROM devices
        WHERE dev_code = @DeviceCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Device>(
                sql,
                new { DeviceCode = deviceCode }));
    }

    /// <summary>
    /// 트랜잭션 내부에서 장비 코드로 장비를 조회한다.
    /// </summary>
    public async Task<Device?> GetByCodeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int deviceCode)
    {
        const string sql = @"
        SELECT
            dev_code    AS DevCode,
            dev_store   AS DevStore,
            dev_license AS DevLicense,
            dev_apptype AS DevAppType,
            dev_hwid    AS DevHwid,
            dev_pos     AS DevPos,
            dev_name    AS DevName,
            dev_rdate   AS DevRDate
        FROM devices
        WHERE dev_code = @DeviceCode;
        ";

        return await connection.QueryFirstOrDefaultAsync<Device>(
            sql,
            new { DeviceCode = deviceCode },
            transaction);
    }

    /// <summary>
    /// PC캠 장비를 HWID 기준으로 조회한다.
    /// 
    /// 실행 인증은 토큰의 DeviceCode 기준으로 검증하고,
    /// 이 메서드는 하트비트 및 보조 조회 용도로 사용한다.
    /// </summary>
    public async Task<Device?> FindPccamDeviceAsync(string hwid)
    {
        const string sql = @"
        SELECT
            dev_code    AS DevCode,
            dev_store   AS DevStore,
            dev_license AS DevLicense,
            dev_apptype AS DevAppType,
            dev_hwid    AS DevHwid,
            dev_pos     AS DevPos,
            dev_name    AS DevName,
            dev_rdate   AS DevRDate
        FROM devices
    WHERE dev_hwid = @Hwid
      AND dev_apptype = 1
    ORDER BY dev_rdate DESC, dev_code DESC
    LIMIT 1;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Device>(
                sql,
                new { Hwid = hwid }));
    }

    /// <summary>
    /// 캠뷰어 로그인 시 기존 등록된 HWID인지 확인한다.
    /// 
    /// 이미 등록된 캠뷰어 장비라면 슬롯 수량을 다시 차감하지 않고 재로그인 허용이 가능하다.
    /// </summary>
    public async Task<Device?> FindViewerByHwidAsync(
        int storeCode,
        string hwid)
    {
        const string sql = @"
        SELECT
            dev_code    AS DevCode,
            dev_store   AS DevStore,
            dev_license AS DevLicense,
            dev_apptype AS DevAppType,
            dev_hwid    AS DevHwid,
            dev_pos     AS DevPos,
            dev_name    AS DevName,
            dev_rdate   AS DevRDate
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_hwid = @Hwid
          AND dev_apptype = 2;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Device>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    Hwid = hwid
                }));
    }

    /// <summary>
    /// 특정 매장, 특정 앱 유형의 등록 장비 수량을 조회한다.
    /// 
    /// 캠뷰어 슬롯 수량 검증에 사용한다.
    /// </summary>
    public async Task<int> CountByStoreAndAppTypeAsync(
        int storeCode,
        int appType)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_apptype = @AppType;
        ";

                return await WithConnectionAsync(conn =>
                    conn.ExecuteScalarAsync<int>(
                        sql,
                        new
                        {
                            StoreCode = storeCode,
                            AppType = appType
                        }));
            }

    /// <summary>
    /// 트랜잭션 내부에서 특정 매장, 특정 앱 유형의 등록 장비 수량을 조회한다.
    /// </summary>
    public async Task<int> CountByStoreAndAppTypeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int storeCode,
        int appType)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_apptype = @AppType;
        ";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                StoreCode = storeCode,
                AppType = appType
            },
            transaction);
    }

    /// <summary>
    /// 같은 매장 내 PC 캠 POS 번호가 이미 등록되어 있는지 확인한다.
    /// 
    /// PC 캠은 POS 번호 중복을 막아야 한다.
    /// 기능 개선 후 POS 번호 대신 HWID로 중복 검증을 하게 되면 이 메서드는 제거할 수 있다.
    /// </summary>
    public async Task<bool> ExistsPccamPosAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int storeCode,
        int posNo)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_pos = @PosNo
          AND dev_apptype = 1;
        ";

        var count = await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                StoreCode = storeCode,
                PosNo = posNo
            },
            transaction);

        return count > 0;
    }

    /// <summary>
    /// 동일 라이선스로 이미 등록된 장비가 있는지 확인한다.
    /// 
    /// PC 캠 인증키 1개는 PC 1대만 허용한다.
    /// </summary>
    public async Task<Device?> FindByLicenseAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int licenseCode)
    {
        const string sql = @"
        SELECT
            dev_code    AS DevCode,
            dev_store   AS DevStore,
            dev_license AS DevLicense,
            dev_apptype AS DevAppType,
            dev_hwid    AS DevHwid,
            dev_pos     AS DevPos,
            dev_name    AS DevName,
            dev_rdate   AS DevRDate
        FROM devices
        WHERE dev_license = @LicenseCode;
        ";

        return await connection.QueryFirstOrDefaultAsync<Device>(
            sql,
            new { LicenseCode = licenseCode },
            transaction);
    }

    /// <summary>
    /// 신규 장비를 등록한다.
    /// 
    /// PC캠:
    /// - dev_license 값 있음
    /// - dev_apptype = 1
    /// - 최초 인증 시 dev_pos = 0
    /// - POS 번호 연결은 이후 NVR/뷰어 설정 단계에서 처리
    /// 
    /// 캠뷰어:
    /// - dev_license null
    /// - dev_apptype = 2
    /// - dev_pos = 0 권장
    /// </summary>
    public async Task<int> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Device device)
    {
        const string sql = @"
        INSERT INTO devices
        (
            dev_store,
            dev_license,
            dev_apptype,
            dev_hwid,
            dev_pos,
            dev_name,
            dev_rdate
        )
        VALUES
        (
            @DevStore,
            @DevLicense,
            @DevAppType,
            @DevHwid,
            @DevPos,
            @DevName,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            device,
            transaction);
    }

    /// <summary>
    /// 특정 매장과 앱 유형 기준으로 장비 목록을 조회한다.
    /// 
    /// 캠뷰어 슬롯 초과 시 기존 장비 목록 표시,
    /// 관리자 장비 관리 화면에서 사용할 수 있다.
    /// </summary>
    public async Task<List<DeviceSummaryDto>> GetDeviceSummariesAsync(
        int storeCode,
        int appType)
    {
        const string sql = @"
        SELECT
            dev_code    AS DeviceCode,
            dev_store   AS StoreCode,
            dev_license AS LicenseCode,
            dev_apptype AS AppType,
            dev_hwid    AS Hwid,
            dev_pos     AS PosNo,
            dev_name    AS DeviceName,
            dev_rdate   AS RegisteredAt
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_apptype = @AppType
        ORDER BY dev_rdate DESC, dev_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<DeviceSummaryDto>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    AppType = appType
                }));

        return result.ToList();
    }

    /// <summary>
    /// 장비를 물리 삭제한다.
    /// 
    /// 정책상 delete 방식을 사용하기로 했기 때문에
    /// 초기화/해제 시 devices row를 삭제한다.
    /// </summary>
    public async Task<int> DeleteAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int deviceCode)
    {
        const string sql = @"
        DELETE FROM devices
        WHERE dev_code = @DeviceCode;
        ";

        return await connection.ExecuteAsync(
            sql,
            new { DeviceCode = deviceCode },
            transaction);
    }

    /// <summary>
    /// 특정 매장에 등록된 전체 장비 목록을 조회한다.
    /// 
    /// PC캠과 캠뷰어를 모두 포함한다.
    /// StoreManageService에서 AppType 기준으로 PC캠/캠뷰어를 분리한다.
    /// </summary>
    public async Task<List<StoreDeviceDto>> GetByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            dev_code    AS DeviceCode,
            dev_store   AS StoreCode,
            dev_license AS LicenseCode,
            dev_apptype AS AppType,
            CASE
                WHEN dev_hwid IS NULL THEN ''
                WHEN LENGTH(dev_hwid) <= 8 THEN dev_hwid
                ELSE CONCAT(LEFT(dev_hwid, 4), '****', RIGHT(dev_hwid, 4))
            END AS HwidMasked,
            dev_pos     AS PosNo,
            dev_name    AS DeviceName,
            dev_rdate   AS RegisteredAt
        FROM devices
        WHERE dev_store = @StoreCode
        ORDER BY dev_apptype ASC, dev_pos ASC, dev_rdate DESC, dev_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreDeviceDto>(
                sql,
                new { StoreCode = storeCode }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 매장의 특정 앱 유형 장비 목록을 조회한다.
    /// 
    /// appType:
    /// 1 = PC캠
    /// 2 = 캠뷰어
    /// </summary>
    public async Task<List<StoreDeviceDto>> GetByStoreAndAppTypeAsync(
        int storeCode,
        int appType)
    {
        const string sql = @"
        SELECT
            dev_code    AS DeviceCode,
            dev_store   AS StoreCode,
            dev_license AS LicenseCode,
            dev_apptype AS AppType,
            CASE
                WHEN dev_hwid IS NULL THEN ''
                WHEN LENGTH(dev_hwid) <= 8 THEN dev_hwid
                ELSE CONCAT(LEFT(dev_hwid, 4), '****', RIGHT(dev_hwid, 4))
            END AS HwidMasked,
            dev_pos     AS PosNo,
            dev_name    AS DeviceName,
            dev_rdate   AS RegisteredAt
        FROM devices
        WHERE dev_store = @StoreCode
          AND dev_apptype = @AppType
        ORDER BY dev_pos ASC, dev_rdate DESC, dev_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreDeviceDto>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    AppType = appType
                }));

        return result.ToList();
    }
}