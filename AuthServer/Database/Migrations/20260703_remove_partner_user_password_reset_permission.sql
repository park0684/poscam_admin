DROP TRIGGER IF EXISTS trg_users_partner_permissions_after_insert;

DELETE FROM partner_user_permissions
WHERE pup_permission = 9;

CREATE TRIGGER trg_users_partner_permissions_after_insert
AFTER INSERT ON users
FOR EACH ROW
INSERT IGNORE INTO partner_user_permissions
(
    pup_user,
    pup_permission,
    pup_created_at,
    pup_created_by
)
SELECT NEW.user_code, p.permission_code, NOW(), NEW.user_code
FROM
(
    SELECT 5 AS permission_code
    UNION ALL SELECT 7
    UNION ALL SELECT 10
    UNION ALL SELECT 11
    UNION ALL SELECT 13
) p
WHERE NEW.user_role = 2;
