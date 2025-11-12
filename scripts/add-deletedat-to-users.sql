-- =====================================================
-- Add DeletedAt column to Users table for soft delete
-- =====================================================
-- Run with:
--   mysql -h localhost -P 3306 -u root -ppassword123 InsuranceClaimsDB < scripts/add-deletedat-to-users.sql
-- =====================================================

USE InsuranceClaimsDB;

-- Add DeletedAt column to Users table if it doesn't exist
SET @colExists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE
        TABLE_SCHEMA = 'InsuranceClaimsDB'
        AND TABLE_NAME = 'Users'
        AND COLUMN_NAME = 'DeletedAt'
);

SET @sql = IF(
    @colExists = 0,
    'ALTER TABLE `Users` ADD COLUMN `DeletedAt` DATETIME NULL AFTER `LastLoginAt`',
    'SELECT "Column DeletedAt already exists in Users table" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Create index on DeletedAt for better query performance
SET @indexExists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE
        TABLE_SCHEMA = 'InsuranceClaimsDB'
        AND TABLE_NAME = 'Users'
        AND INDEX_NAME = 'IX_Users_DeletedAt'
);

SET @sql = IF(
    @indexExists = 0,
    'CREATE INDEX `IX_Users_DeletedAt` ON `Users` (`DeletedAt`)',
    'SELECT "Index IX_Users_DeletedAt already exists" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'DeletedAt column added to Users table successfully' AS message;



