-- POSCAM 다중 NVR 설정 마이그레이션 사전 점검
--
-- 실행 순서:
--   1. 운영 DB 백업
--   2. 이 파일 실행
--   3. 모든 BLOCKING 항목이 0인지 확인
--   4. 20260817_add_multi_nvr_config.sql 실행
--   5. 20260817_verify_multi_nvr_config.sql 실행
--
-- 중요:
-- - 이 스크립트는 DB 구조를 변경하지 않는다.
-- - BLOCKING 상태가 하나라도 나오면 마이그레이션을 실행하지 않는다.
-- - 특히 nvr_configs를 참조하는 기존 FK가 있으면 PK 변경 전에 별도 설계 검토가 필요하다.

-- -----------------------------------------------------------------------------
-- 1. 대상 테이블 존재 여부
-- -----------------------------------------------------------------------------
SELECT
    'TABLE:nvr_configs' AS check_name,
    COUNT(*) AS actual_count,
    CASE WHEN COUNT(*) = 1 THEN 'OK' ELSE 'BLOCKING' END AS status
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs';

SELECT
    'TABLE:ch_config' AS check_name,
    COUNT(*) AS actual_count,
    CASE WHEN COUNT(*) = 1 THEN 'OK' ELSE 'BLOCKING' END AS status
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ch_config';

-- -----------------------------------------------------------------------------
-- 2. 기존 nvr_configs PK 형태 확인
--    기존 운영 구조는 nvr_store 단일 PK이거나,
--    이미 마이그레이션된 경우 (nvr_store, nvr_no)여야 한다.
-- -----------------------------------------------------------------------------
SELECT
    kcu.TABLE_NAME,
    kcu.CONSTRAINT_NAME,
    kcu.ORDINAL_POSITION,
    kcu.COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = DATABASE()
  AND kcu.TABLE_NAME = 'nvr_configs'
  AND kcu.CONSTRAINT_NAME = 'PRIMARY'
ORDER BY kcu.ORDINAL_POSITION;

-- -----------------------------------------------------------------------------
-- 3. nvr_configs를 참조하는 기존 Foreign Key 확인
--    1건 이상이면 BLOCKING. PK 변경 전에 FK 설계를 별도로 검토한다.
-- -----------------------------------------------------------------------------
SELECT
    'FK_REFERENCING:nvr_configs' AS check_name,
    COUNT(*) AS actual_count,
    CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'BLOCKING' END AS status
FROM information_schema.KEY_COLUMN_USAGE
WHERE REFERENCED_TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME = 'nvr_configs';

SELECT
    TABLE_NAME,
    CONSTRAINT_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE REFERENCED_TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME = 'nvr_configs'
ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;

-- -----------------------------------------------------------------------------
-- 4. nvr_configs 자체 FK 확인
--    참고용. 매장 FK 등 기존 제약을 확인한다.
-- -----------------------------------------------------------------------------
SELECT
    TABLE_NAME,
    CONSTRAINT_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'nvr_configs'
  AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY CONSTRAINT_NAME, ORDINAL_POSITION;

-- -----------------------------------------------------------------------------
-- 5. 현재 행 수와 매장별 NVR 행 수 확인
--    마이그레이션 전에는 일반적으로 매장당 최대 1행이어야 한다.
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS nvr_config_row_count
FROM nvr_configs;

SELECT
    nvr_store,
    COUNT(*) AS row_count
FROM nvr_configs
GROUP BY nvr_store
HAVING COUNT(*) > 1
ORDER BY nvr_store;

-- 위 쿼리가 마이그레이션 전에 행을 반환한다면,
-- 현재 DB가 이미 다른 방식으로 다중 NVR을 저장 중인지 먼저 확인한다.

-- -----------------------------------------------------------------------------
-- 6. 채널 설정 PK 형태와 중복 여부 확인
--    기존 화면 식별 키 (chn_store, chn_pos, chn_screen)는 유지할 계획이다.
-- -----------------------------------------------------------------------------
SELECT
    kcu.TABLE_NAME,
    kcu.CONSTRAINT_NAME,
    kcu.ORDINAL_POSITION,
    kcu.COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = DATABASE()
  AND kcu.TABLE_NAME = 'ch_config'
  AND kcu.CONSTRAINT_NAME = 'PRIMARY'
ORDER BY kcu.ORDINAL_POSITION;

SELECT
    chn_store,
    chn_pos,
    chn_screen,
    COUNT(*) AS duplicate_count
FROM ch_config
GROUP BY chn_store, chn_pos, chn_screen
HAVING COUNT(*) > 1
ORDER BY chn_store, chn_pos, chn_screen;

-- -----------------------------------------------------------------------------
-- 7. 기존 컬럼 존재 여부
--    이미 일부 적용된 DB인지 판단하기 위한 참고 정보.
-- -----------------------------------------------------------------------------
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND (
        (TABLE_NAME = 'nvr_configs' AND COLUMN_NAME IN ('nvr_store', 'nvr_no'))
        OR
        (TABLE_NAME = 'ch_config' AND COLUMN_NAME IN ('chn_store', 'chn_nvr_no', 'chn_pos', 'chn_screen'))
      )
ORDER BY TABLE_NAME, ORDINAL_POSITION;

-- -----------------------------------------------------------------------------
-- 판정 기준
-- -----------------------------------------------------------------------------
-- 진행 가능:
-- - nvr_configs / ch_config 모두 존재
-- - FK_REFERENCING:nvr_configs = 0
-- - 마이그레이션 전 nvr_store별 중복 행 없음
-- - ch_config의 (chn_store, chn_pos, chn_screen) 중복 없음
--
-- 진행 중단:
-- - 위 조건 중 하나라도 만족하지 않음
-- - 이미 일부 컬럼/PK가 다른 형태로 변경되어 있음
