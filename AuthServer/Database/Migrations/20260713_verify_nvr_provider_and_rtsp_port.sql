-- NVR Provider / RTSP 포트 마이그레이션 검증용 읽기 전용 SQL.
-- 데이터나 스키마를 변경하지 않는다.
-- 20260713_add_nvr_provider_and_rtsp_port.sql 실행 후 사용한다.

-- 1. 대상 DB 및 테이블 존재 확인.
SELECT
    DATABASE() AS current_database,
    CASE
        WHEN COUNT(*) = 1 THEN 'PASS'
        ELSE 'FAIL'
    END AS nvr_configs_table_check
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs';

-- 2. 신규 컬럼의 타입, NULL 허용 여부, 기본값 확인.
SELECT
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CASE
        WHEN COLUMN_NAME = 'nvr_provider'
             AND DATA_TYPE = 'int'
             AND IS_NULLABLE = 'NO'
             AND CAST(COLUMN_DEFAULT AS CHAR) = '1'
            THEN 'PASS'
        WHEN COLUMN_NAME = 'nvr_rtsp_port'
             AND DATA_TYPE = 'int'
             AND IS_NULLABLE = 'NO'
             AND CAST(COLUMN_DEFAULT AS CHAR) = '554'
            THEN 'PASS'
        ELSE 'FAIL'
    END AS column_check
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs'
  AND COLUMN_NAME IN ('nvr_provider', 'nvr_rtsp_port')
ORDER BY ORDINAL_POSITION;

-- 3. 신규 컬럼이 정확히 두 개 존재하는지 확인.
SELECT
    COUNT(*) AS found_column_count,
    CASE
        WHEN COUNT(*) = 2 THEN 'PASS'
        ELSE 'FAIL'
    END AS required_column_count_check
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs'
  AND COLUMN_NAME IN ('nvr_provider', 'nvr_rtsp_port');

-- 4. Provider 코드와 포트 값의 유효성 확인.
SELECT
    COUNT(*) AS total_rows,
    SUM(CASE WHEN nvr_provider NOT IN (1, 2, 3) THEN 1 ELSE 0 END) AS invalid_provider_rows,
    SUM(CASE WHEN nvr_port < 1 OR nvr_port > 65535 THEN 1 ELSE 0 END) AS invalid_control_port_rows,
    SUM(CASE WHEN nvr_rtsp_port < 1 OR nvr_rtsp_port > 65535 THEN 1 ELSE 0 END) AS invalid_rtsp_port_rows,
    CASE
        WHEN SUM(CASE WHEN nvr_provider NOT IN (1, 2, 3) THEN 1 ELSE 0 END) = 0
         AND SUM(CASE WHEN nvr_port < 1 OR nvr_port > 65535 THEN 1 ELSE 0 END) = 0
         AND SUM(CASE WHEN nvr_rtsp_port < 1 OR nvr_rtsp_port > 65535 THEN 1 ELSE 0 END) = 0
            THEN 'PASS'
        ELSE 'FAIL'
    END AS stored_value_check
FROM nvr_configs;

-- 5. 기존 데이터 기본 보정 결과 확인.
-- 마이그레이션 전 존재하던 행은 Provider=1, RTSP=554여야 한다.
SELECT
    COUNT(*) AS non_default_legacy_rows,
    CASE
        WHEN COUNT(*) = 0 THEN 'PASS'
        ELSE 'CHECK_REQUIRED'
    END AS legacy_default_check
FROM nvr_configs
WHERE nvr_provider <> 1
   OR nvr_rtsp_port <> 554;

-- 6. 실제 저장값 목록 확인.
SELECT
    nvr_store,
    nvr_provider,
    nvr_ip,
    nvr_port AS control_port,
    nvr_rtsp_port,
    nvr_version
FROM nvr_configs
ORDER BY nvr_store;
