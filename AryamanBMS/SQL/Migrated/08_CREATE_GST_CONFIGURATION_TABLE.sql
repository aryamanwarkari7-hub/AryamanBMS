-- GST configuration persistence
-- Run this after Accounts tables are created.

CREATE TABLE IF NOT EXISTS `tablegstconfiguration` (
    `GstConfigurationId` INT NOT NULL AUTO_INCREMENT,
    `CompanyName` VARCHAR(200) NOT NULL,
    `CompanyGstin` VARCHAR(15) NOT NULL,
    `RegisteredState` VARCHAR(50) NOT NULL,
    `CgstRate` DECIMAL(5,2) NOT NULL DEFAULT 9.00,
    `SgstRate` DECIMAL(5,2) NOT NULL DEFAULT 9.00,
    `IgstRate` DECIMAL(5,2) NOT NULL DEFAULT 18.00,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    `UpdatedByUserId` VARCHAR(450) NULL,
    `UpdatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`GstConfigurationId`),
    INDEX `IX_tablegstconfiguration_IsActive` (`IsActive`),
    INDEX `IX_tablegstconfiguration_CompanyGstin` (`CompanyGstin`)
);

-- Optional starter row. Change values before running if needed.
INSERT INTO `tablegstconfiguration`
(
    `CompanyName`,
    `CompanyGstin`,
    `RegisteredState`,
    `CgstRate`,
    `SgstRate`,
    `IgstRate`,
    `IsActive`,
    `UpdatedByUserId`,
    `UpdatedOn`
)
SELECT
    'Aryaman Technologies Private Limited',
    '27AABCA1234A1Z5',
    'MH',
    9.00,
    9.00,
    18.00,
    b'1',
    NULL,
    NOW()
WHERE NOT EXISTS
(
    SELECT 1
    FROM `tablegstconfiguration`
    WHERE `IsActive` = b'1'
);
