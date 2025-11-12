-- =====================================================
-- Create triggers for User updates and deletes to log to user_audit_log table
-- =====================================================
-- Run with:
--   mysql -h 127.0.0.1 -P 3306 -u root -ppassword123 InsuranceClaimsDB < scripts/create-user-audit-triggers.sql
-- =====================================================

USE InsuranceClaimsDB;

-- Drop existing triggers if they exist
DROP TRIGGER IF EXISTS `users_update_audit`;
DROP TRIGGER IF EXISTS `users_delete_audit`;

-- Create trigger for UPDATE on Users table
DELIMITER $$

CREATE TRIGGER `users_update_audit`
AFTER UPDATE ON `Users`
FOR EACH ROW
BEGIN
    -- Only log if DeletedAt is NULL (not soft-deleted) and something actually changed
    IF NEW.DeletedAt IS NULL AND (
        OLD.FirstName != NEW.FirstName OR
        OLD.LastName != NEW.LastName OR
        OLD.Email != NEW.Email OR
        OLD.CompanyName != NEW.CompanyName OR
        OLD.PhoneNumber != NEW.PhoneNumber OR
        OLD.Address != NEW.Address OR
        OLD.City != NEW.City OR
        OLD.PostalCode != NEW.PostalCode OR
        OLD.Country != NEW.Country OR
        OLD.Role != NEW.Role OR
        OLD.Status != NEW.Status OR
        OLD.ProfileImageUrl != NEW.ProfileImageUrl
    ) THEN
        INSERT INTO `user_audit_log` (
            `user_id`,
            `action`,
            `changed_by`,
            `before_state`,
            `after_state`,
            `change_diff`
        ) VALUES (
            NEW.id,
            'UPDATE',
            NULL, -- You can set this to a user ID if you track who made the change
            JSON_OBJECT(
                'id', OLD.id,
                'email', OLD.email,
                'firstName', OLD.firstName,
                'lastName', OLD.lastName,
                'companyName', IFNULL(OLD.companyName, ''),
                'phoneNumber', IFNULL(OLD.phoneNumber, ''),
                'role', OLD.role,
                'status', OLD.status,
                'address', IFNULL(OLD.address, ''),
                'city', IFNULL(OLD.city, ''),
                'postalCode', IFNULL(OLD.postalCode, ''),
                'country', IFNULL(OLD.country, ''),
                'profileImageUrl', IFNULL(OLD.profileImageUrl, '')
            ),
            JSON_OBJECT(
                'id', NEW.id,
                'email', NEW.email,
                'firstName', NEW.firstName,
                'lastName', NEW.lastName,
                'companyName', IFNULL(NEW.companyName, ''),
                'phoneNumber', IFNULL(NEW.phoneNumber, ''),
                'role', NEW.role,
                'status', NEW.status,
                'address', IFNULL(NEW.address, ''),
                'city', IFNULL(NEW.city, ''),
                'postalCode', IFNULL(NEW.postalCode, ''),
                'country', IFNULL(NEW.country, ''),
                'profileImageUrl', IFNULL(NEW.profileImageUrl, '')
            ),
            JSON_OBJECT(
                'id', JSON_OBJECT('from', OLD.id, 'to', NEW.id),
                'email', JSON_OBJECT('from', OLD.email, 'to', NEW.email),
                'firstName', JSON_OBJECT('from', OLD.firstName, 'to', NEW.firstName),
                'lastName', JSON_OBJECT('from', OLD.lastName, 'to', NEW.lastName),
                'companyName', JSON_OBJECT('from', IFNULL(OLD.companyName, ''), 'to', IFNULL(NEW.companyName, '')),
                'phoneNumber', JSON_OBJECT('from', IFNULL(OLD.phoneNumber, ''), 'to', IFNULL(NEW.phoneNumber, '')),
                'role', JSON_OBJECT('from', OLD.role, 'to', NEW.role),
                'status', JSON_OBJECT('from', OLD.status, 'to', NEW.status),
                'address', JSON_OBJECT('from', IFNULL(OLD.address, ''), 'to', IFNULL(NEW.address, '')),
                'city', JSON_OBJECT('from', IFNULL(OLD.city, ''), 'to', IFNULL(NEW.city, '')),
                'postalCode', JSON_OBJECT('from', IFNULL(OLD.postalCode, ''), 'to', IFNULL(NEW.postalCode, '')),
                'country', JSON_OBJECT('from', IFNULL(OLD.country, ''), 'to', IFNULL(NEW.country, '')),
                'profileImageUrl', JSON_OBJECT('from', IFNULL(OLD.profileImageUrl, ''), 'to', IFNULL(NEW.profileImageUrl, ''))
            )
        );
    END IF;
END$$

DELIMITER ;

-- Create trigger for DELETE on Users table (for soft deletes - when DeletedAt is set)
DELIMITER $$

CREATE TRIGGER `users_delete_audit`
AFTER UPDATE ON `Users`
FOR EACH ROW
BEGIN
    -- Log when a user is soft-deleted (DeletedAt changes from NULL to a value)
    IF OLD.DeletedAt IS NULL AND NEW.DeletedAt IS NOT NULL THEN
        INSERT INTO `user_audit_log` (
            `user_id`,
            `action`,
            `changed_by`,
            `before_state`,
            `after_state`,
            `change_diff`
        ) VALUES (
            NEW.id,
            'DELETE',
            NULL, -- You can set this to a user ID if you track who made the change
            JSON_OBJECT(
                'id', NEW.id,
                'email', NEW.email,
                'firstName', NEW.firstName,
                'lastName', NEW.lastName,
                'companyName', IFNULL(NEW.companyName, ''),
                'phoneNumber', IFNULL(NEW.phoneNumber, ''),
                'role', NEW.role,
                'status', NEW.status,
                'address', IFNULL(NEW.address, ''),
                'city', IFNULL(NEW.city, ''),
                'postalCode', IFNULL(NEW.postalCode, ''),
                'country', IFNULL(NEW.country, ''),
                'profileImageUrl', IFNULL(NEW.profileImageUrl, ''),
                'deletedAt', NULL
            ),
            JSON_OBJECT(
                'id', NEW.id,
                'email', NEW.email,
                'firstName', NEW.firstName,
                'lastName', NEW.lastName,
                'companyName', IFNULL(NEW.companyName, ''),
                'phoneNumber', IFNULL(NEW.phoneNumber, ''),
                'role', NEW.role,
                'status', NEW.status,
                'address', IFNULL(NEW.address, ''),
                'city', IFNULL(NEW.city, ''),
                'postalCode', IFNULL(NEW.postalCode, ''),
                'country', IFNULL(NEW.country, ''),
                'profileImageUrl', IFNULL(NEW.profileImageUrl, ''),
                'deletedAt', NEW.DeletedAt
            ),
            JSON_OBJECT(
                'deletedAt', JSON_OBJECT('from', NULL, 'to', NEW.DeletedAt)
            )
        );
    END IF;
END$$

DELIMITER ;

-- Verify triggers were created
SELECT 
    TRIGGER_NAME,
    EVENT_MANIPULATION,
    EVENT_OBJECT_TABLE,
    ACTION_TIMING
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = 'InsuranceClaimsDB'
    AND EVENT_OBJECT_TABLE = 'Users'
ORDER BY TRIGGER_NAME;

SELECT 'User audit triggers created successfully!' AS message;
