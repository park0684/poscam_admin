-- CamViewer NVR 설정을 고정 Provider 코드와 분리된 RTSP 포트 구조로 확장한다.
-- 적용 순서: 운영 DB에 본 마이그레이션 적용 후 AuthServer 신규 버전을 배포한다.
-- 기존 데이터는 모두 Dahua 기준으로 Provider=1, RTSP=554를 사용한다.

SET @nvr_provider_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'nvr_configs'
      AND COLUMN_NAME = 'nvr_provider'
);

SET @add_nvr_provider_sql := IF(
    @nvr_provider_exists = 0,
    'ALTER TABLE nvr_configs ADD COLUMN nvr_provider INT NOT NULL DEFAULT 1 COMMENT ''NVR Provider: 1=Dahua, 2=TP-Link VIGI, 3=KT Telecop'' AFTER nvr_store',
    'SELECT ''nvr_configs.nvr_provider already exists'' AS message'
);

PREPARE add_nvr_provider_stmt FROM @add_nvr_provider_sql;
EXECUTE add_nvr_provider_stmt;
DEALLOCATE PREPARE add_nvr_provider_stmt;

SET @nvr_rtsp_port_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'nvr_configs'
      AND COLUMN_NAME = 'nvr_rtsp_port'
);

SET @add_nvr_rtsp_port_sql := IF(
    @nvr_rtsp_port_exists = 0,
    'ALTER TABLE nvr_configs ADD COLUMN nvr_rtsp_port INT NOT NULL DEFAULT 554 COMMENT ''영상 재생용 RTSP 포트'' AFTER nvr_port',
    'SELECT ''nvr_configs.nvr_rtsp_port already exists'' AS message'
);

PREPARE add_nvr_rtsp_port_stmt FROM @add_nvr_rtsp_port_sql;
EXECUTE add_nvr_rtsp_port_stmt;
DEALLOCATE PREPARE add_nvr_rtsp_port_stmt;

-- 기존 데이터 및 비정상 기본값 보정.
UPDATE nvr_configs
SET nvr_provider = 1
WHERE nvr_provider IS NULL OR nvr_provider = 0;

UPDATE nvr_configs
SET nvr_rtsp_port = 554
WHERE nvr_rtsp_port IS NULL OR nvr_rtsp_port <= 0;

SELECT
    nvr_store,
    nvr_provider,
    nvr_port,
    nvr_rtsp_port
FROM nvr_configs
ORDER BY nvr_store;
