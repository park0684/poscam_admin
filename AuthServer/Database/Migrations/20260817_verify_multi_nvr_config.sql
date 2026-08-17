-- POSCAM 다중 NVR 설정 마이그레이션 검증
-- 대상: 20260817_add_multi_nvr_config.sql 적용 후
--
-- 정상 기준:
-- - required_column_count = 2
-- - primary_key_column_count = 2
-- - invalid_nvr_no_count = 0
-- - invalid_channel_nvr_no_count = 0
-- - orphan_channel_count = 0
-- - duplicate_nvr_key_count = 0
-- - duplicate_channel_key_count = 0
-- - 아래 version mismatch 조회 결과 0행

-- 1. 필수 컬럼 존재 여부
SELECT
    COUNT(*) AS required_column_count,
    CASE WHEN COUNT(*) = 2 THEN 'OK' ELSE 'FAIL' END AS status
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND (
        (TABLE_NAME = 'nvr_configs' AND COLUMN_NAME = 'nvr_no')
        OR
        (TABLE_NAME = 'ch_config' AND COLUMN_NAME = 'chn_nvr_no')
      );

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND (
        (TABLE_NAME = 'nvr_configs' AND COLUMN_NAME = 'nvr_no')
        OR
        (TABLE_NAME = 'ch_config' AND COLUMN_NAME = 'chn_nvr_no')
      )
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- 2. nvr_configs Primary Key가 정확히 (nvr_store, nvr_no)인지 확인
SELECT
    COUNT(*) AS primary_key_column_count,
    CASE
        WHEN COUNT(*) = 2
         AND SUM(CASE WHEN COLUMN_NAME = 'nvr_store' AND ORDINAL_POSITION = 1 THEN 1 ELSE 0 END) = 1
         AND SUM(CASE WHEN COLUMN_NAME = 'nvr_no' AND ORDINAL_POSITION = 2 THEN 1 ELSE 0 END) = 1
        THEN 'OK'
        ELSE 'FAIL'
    END AS status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs'
  AND CONSTRAINT_NAME = 'PRIMARY';

SELECT
    TABLE_NAME,
    CONSTRAINT_NAME,
    COLUMN_NAME,
    ORDINAL_POSITION
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs'
  AND CONSTRAINT_NAME = 'PRIMARY'
ORDER BY ORDINAL_POSITION;

-- 3. ch_config 보조 인덱스 확인
SELECT
    COUNT(*) AS index_column_count,
    CASE
        WHEN COUNT(*) = 2
         AND SUM(CASE WHEN COLUMN_NAME = 'chn_store' AND SEQ_IN_INDEX = 1 THEN 1 ELSE 0 END) = 1
         AND SUM(CASE WHEN COLUMN_NAME = 'chn_nvr_no' AND SEQ_IN_INDEX = 2 THEN 1 ELSE 0 END) = 1
        THEN 'OK'
        ELSE 'FAIL'
    END AS status
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ch_config'
  AND INDEX_NAME = 'idx_ch_config_store_nvr';

SELECT
    TABLE_NAME,
    INDEX_NAME,
    COLUMN_NAME,
    SEQ_IN_INDEX
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ch_config'
  AND INDEX_NAME = 'idx_ch_config_store_nvr'
ORDER BY SEQ_IN_INDEX;

-- 4. 잘못된 NVR 번호 데이터가 없는지 확인
SELECT COUNT(*) AS invalid_nvr_no_count
FROM nvr_configs
WHERE nvr_no IS NULL OR nvr_no <= 0;

SELECT COUNT(*) AS invalid_channel_nvr_no_count
FROM ch_config
WHERE chn_nvr_no IS NULL OR chn_nvr_no <= 0;

-- 5. 채널이 존재하지 않는 NVR 번호를 참조하는지 확인
SELECT COUNT(*) AS orphan_channel_count
FROM ch_config c
LEFT JOIN nvr_configs n
  ON n.nvr_store = c.chn_store
 AND n.nvr_no = c.chn_nvr_no
WHERE n.nvr_store IS NULL;

SELECT
    c.chn_store,
    c.chn_nvr_no,
    c.chn_pos,
    c.chn_screen,
    c.chn_ch
FROM ch_config c
LEFT JOIN nvr_configs n
  ON n.nvr_store = c.chn_store
 AND n.nvr_no = c.chn_nvr_no
WHERE n.nvr_store IS NULL
ORDER BY c.chn_store, c.chn_pos, c.chn_screen;

-- 6. 논리 키 중복 여부 확인
SELECT COUNT(*) AS duplicate_nvr_key_count
FROM (
    SELECT nvr_store, nvr_no
    FROM nvr_configs
    GROUP BY nvr_store, nvr_no
    HAVING COUNT(*) > 1
) d;

SELECT COUNT(*) AS duplicate_channel_key_count
FROM (
    SELECT chn_store, chn_pos, chn_screen
    FROM ch_config
    GROUP BY chn_store, chn_pos, chn_screen
    HAVING COUNT(*) > 1
) d;

-- 7. 동일 매장의 NVR 설정 버전이 서로 다른지 확인
--    정상은 0행이다.
SELECT
    nvr_store,
    COUNT(DISTINCT COALESCE(nvr_version, '')) AS version_count
FROM nvr_configs
GROUP BY nvr_store
HAVING COUNT(DISTINCT COALESCE(nvr_version, '')) > 1;

-- 8. nvr_configs를 참조하는 FK가 새로 생기지 않았는지 확인
--    이번 구현에서는 DB FK를 추가하지 않으므로 0이 정상이다.
SELECT
    COUNT(*) AS referencing_nvr_fk_count,
    CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'REVIEW' END AS status
FROM information_schema.KEY_COLUMN_USAGE
WHERE REFERENCED_TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME = 'nvr_configs';

-- 9. 현재 데이터 확인
SELECT
    nvr_store,
    nvr_no,
    nvr_provider,
    nvr_ip,
    nvr_port,
    nvr_rtsp_port,
    nvr_channels,
    nvr_version
FROM nvr_configs
ORDER BY nvr_store, nvr_no;

SELECT
    chn_store,
    chn_nvr_no,
    chn_pos,
    chn_screen,
    chn_ch
FROM ch_config
ORDER BY chn_store, chn_pos, chn_screen;
