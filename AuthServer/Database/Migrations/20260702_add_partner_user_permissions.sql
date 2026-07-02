CREATE TABLE IF NOT EXISTS partner_user_permissions
(
    pup_user       INT      NOT NULL,
    pup_permission INT      NOT NULL,
    pup_created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    pup_created_by INT      NULL,
    PRIMARY KEY (pup_user, pup_permission),
    CONSTRAINT fk_partner_user_permissions_user
        FOREIGN KEY (pup_user)
        REFERENCES users (user_code)
        ON DELETE CASCADE
);

INSERT IGNORE INTO partner_user_permissions
(
    pup_user,
    pup_permission,
    pup_created_at,
    pup_created_by
)
SELECT
    u.user_code,
    p.permission_code,
    NOW(),
    NULL
FROM users u
CROSS JOIN
(
    SELECT 5 AS permission_code
    UNION ALL SELECT 7
    UNION ALL SELECT 9
    UNION ALL SELECT 10
    UNION ALL SELECT 11
    UNION ALL SELECT 13
) p
WHERE u.user_role = 2;
