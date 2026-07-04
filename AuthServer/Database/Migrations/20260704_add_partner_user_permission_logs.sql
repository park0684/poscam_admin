CREATE TABLE IF NOT EXISTS partner_user_permission_logs
(
    pupl_code BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    pupl_user INT NOT NULL,
    pupl_changed_by INT NOT NULL,
    pupl_before_permissions VARCHAR(200) NULL,
    pupl_after_permissions VARCHAR(200) NULL,
    pupl_changed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    INDEX ix_partner_user_permission_logs_user (pupl_user),
    INDEX ix_partner_user_permission_logs_changed_at (pupl_changed_at)
);
