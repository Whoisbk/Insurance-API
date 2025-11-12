-- =====================================================
-- Create Insurers table and relate to ServiceProviders
-- =====================================================
-- Run with:
--   mysql -h localhost -P 3306 -u root -ppassword123 InsuranceClaimsDB < scripts/create-insurers-table.sql
-- =====================================================

USE InsuranceClaimsDB;

-- Create Insurers table (stores extended details for insurer users)
CREATE TABLE IF NOT EXISTS `Insurers` (
    `InsurerId` INT NOT NULL AUTO_INCREMENT,
    `UserId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Email` VARCHAR(255) NOT NULL,
    `PhoneNumber` VARCHAR(20) NULL,
    `Address` VARCHAR(500) NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`InsurerId`),
    UNIQUE KEY `UX_Insurers_UserId` (`UserId`),
    CONSTRAINT `FK_Insurers_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE = InnoDB;

-- Add InsurerId column to ServiceProviders if missing
SET
    @colExists := (
        SELECT COUNT(*)
        FROM information_schema.COLUMNS
        WHERE
            TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = 'ServiceProviders'
            AND COLUMN_NAME = 'InsurerId'
    );

SET
    @sql := IF(
        @colExists = 0,
        'ALTER TABLE `ServiceProviders` ADD COLUMN `InsurerId` INT NULL AFTER `UserId`;',
        'SELECT ''InsurerId column already exists on ServiceProviders.'' AS Info;'
    );

PREPARE stmt FROM @sql;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

-- Add index on InsurerId if missing
SET
    @indexExists := (
        SELECT COUNT(*)
        FROM information_schema.STATISTICS
        WHERE
            TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = 'ServiceProviders'
            AND INDEX_NAME = 'IX_ServiceProviders_InsurerId'
    );

SET
    @sql := IF(
        @indexExists = 0,
        'CREATE INDEX `IX_ServiceProviders_InsurerId` ON `ServiceProviders` (`InsurerId`);',
        'SELECT ''Index IX_ServiceProviders_InsurerId already exists.'' AS Info;'
    );

PREPARE stmt FROM @sql;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

-- Add foreign key constraint if missing
SET
    @fkExists := (
        SELECT COUNT(*)
        FROM information_schema.REFERENTIAL_CONSTRAINTS
        WHERE
            CONSTRAINT_SCHEMA = DATABASE()
            AND CONSTRAINT_NAME = 'FK_ServiceProviders_Insurers'
    );

SET
    @sql := IF(
        @fkExists = 0,
        'ALTER TABLE `ServiceProviders` ADD CONSTRAINT `FK_ServiceProviders_Insurers` FOREIGN KEY (`InsurerId`) REFERENCES `Insurers` (`InsurerId`) ON DELETE SET NULL;',
        'SELECT ''Foreign key FK_ServiceProviders_Insurers already exists.'' AS Info;'
    );

PREPARE stmt FROM @sql;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

-- Summary message
SELECT 'Insurers table created / verified and linked to ServiceProviders.' AS Status;