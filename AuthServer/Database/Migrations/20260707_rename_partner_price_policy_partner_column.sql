-- partner_price_policy 파트너사 컬럼명을 스키마 정책에 맞게 ppp_partner로 통일한다.
-- 기존 운영 DB에 partner_code 컬럼이 남아 있고 ppp_partner가 없을 때만 변경한다.

SET @partner_code_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'partner_price_policy'
      AND COLUMN_NAME = 'partner_code'
);

SET @ppp_partner_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'partner_price_policy'
      AND COLUMN_NAME = 'ppp_partner'
);

SET @rename_sql := IF(
    @partner_code_exists = 1 AND @ppp_partner_exists = 0,
    'ALTER TABLE partner_price_policy CHANGE COLUMN partner_code ppp_partner INT(11) NOT NULL',
    'SELECT ''partner_price_policy.ppp_partner already aligned'' AS message'
);

PREPARE rename_stmt FROM @rename_sql;
EXECUTE rename_stmt;
DEALLOCATE PREPARE rename_stmt;

SET @ppp_partner_index_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'partner_price_policy'
      AND COLUMN_NAME = 'ppp_partner'
);

SET @index_sql := IF(
    @ppp_partner_index_exists = 0,
    'CREATE INDEX ix_partner_price_policy_partner ON partner_price_policy (ppp_partner)',
    'SELECT ''partner_price_policy.ppp_partner index already exists'' AS message'
);

PREPARE index_stmt FROM @index_sql;
EXECUTE index_stmt;
DEALLOCATE PREPARE index_stmt;
