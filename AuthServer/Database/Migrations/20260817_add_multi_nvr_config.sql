-- POSCAM 다중 NVR 설정 구조 확장
--
-- 목적:
-- 1. nvr_configs를 매장당 여러 NVR 행을 저장할 수 있도록 확장한다.
-- 2. ch_config의 각 화면 매핑이 어느 NVR에 속하는지 저장한다.
--
-- 주의:
-- - 이 파일은 운영 DB에 자동 적용하지 않는다.
-- - 적용 전 운영 DB 백업이 필요하다.
-- - 기존 단일 NVR 데이터는 NVR 번호 1로 유지한다.
-- - 신규 Foreign Key는 이번 단계에서 추가하지 않고 서비스 계층에서 참조 무결성을 검증한다.

-- -----------------------------------------------------------------------------
-- 1. nvr_configs.nvr_no 추가
-- -----------------------------------------------------------------------------
SET @nvr_no_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'nvr_configs'
      AND COLUMN_NAME = 'nvr_no'
);

SET @add_nvr_no_sql := IF(
    @nvr_no_exists = 0,
    'ALTER TABLE nvr_configs ADD COLUMN nvr_no INT NOT NULL DEFAULT 1 COMMENT ''매장 내부 NVR 번호'' AFTER nvr_store',
    'SELECT ''nvr_configs.nvr_no already exists'' AS message'
);

PREPARE add_nvr_no_stmt FROM @add_nvr_no_sql;
EXECUTE add_nvr_no_stmt;
DEALLOCATE PREPARE add_nvr_no_stmt;

UPDATE nvr_configs
SET nvr_no = 1
WHERE nvr_no IS NULL OR nvr_no <= 0;

-- -----------------------------------------------------------------------------
-- 2. nvr_configs PK를 (nvr_store, nvr_no)로 확장
-- -----------------------------------------------------------------------------
SET @nvr_pk_has_nvr_no := (
    SELECT COUNT(*)
    FROM information_schema.KEY_COLUMN_USAGE
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'nvr_configs'
      AND CONSTRAINT_NAME = 'PRIMARY'
      AND COLUMN_NAME = 'nvr_no'
);

SET @alter_nvr_pk_sql := IF(
    @nvr_pk_has_nvr_no = 0,
    'ALTER TABLE nvr_configs DROP PRIMARY KEY, ADD PRIMARY KEY (nvr_store, nvr_no)',
    'SELECT ''nvr_configs primary key already includes nvr_no'' AS message'
);

PREPARE alter_nvr_pk_stmt FROM @alter_nvr_pk_sql;
EXECUTE alter_nvr_pk_stmt;
DEALLOCATE PREPARE alter_nvr_pk_stmt;

-- -----------------------------------------------------------------------------
-- 3. ch_config.chn_nvr_no 추가
-- -----------------------------------------------------------------------------
SET @chn_nvr_no_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ch_config'
      AND COLUMN_NAME = 'chn_nvr_no'
);

SET @add_chn_nvr_no_sql := IF(
    @chn_nvr_no_exists = 0,
    'ALTER TABLE ch_config ADD COLUMN chn_nvr_no INT NOT NULL DEFAULT 1 COMMENT ''채널 매핑이 참조하는 매장 내부 NVR 번호'' AFTER chn_store',
    'SELECT ''ch_config.chn_nvr_no already exists'' AS message'
);

PREPARE add_chn_nvr_no_stmt FROM @add_chn_nvr_no_sql;
EXECUTE add_chn_nvr_no_stmt;
DEALLOCATE PREPARE add_chn_nvr_no_stmt;

UPDATE ch_config
SET chn_nvr_no = 1
WHERE chn_nvr_no IS NULL OR chn_nvr_no <= 0;

-- 조회 및 향후 참조 검증을 위한 보조 인덱스.
SET @chn_nvr_index_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ch_config'
      AND INDEX_NAME = 'idx_ch_config_store_nvr'
);

SET @add_chn_nvr_index_sql := IF(
    @chn_nvr_index_exists = 0,
    'CREATE INDEX idx_ch_config_store_nvr ON ch_config (chn_store, chn_nvr_no)',
    'SELECT ''idx_ch_config_store_nvr already exists'' AS message'
);

PREPARE add_chn_nvr_index_stmt FROM @add_chn_nvr_index_sql;
EXECUTE add_chn_nvr_index_stmt;
DEALLOCATE PREPARE add_chn_nvr_index_stmt;

-- -----------------------------------------------------------------------------
-- 4. 적용 결과 요약
-- -----------------------------------------------------------------------------
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
