-- Office Asset lifecycle and assignment history hardening.
-- Run after 06_CREATE_ACCOUNTS_TABLES.sql.

-- Detect duplicate asset codes before applying unique constraint.
SELECT
    `AssetCode`,
    COUNT(*) AS `DuplicateCount`
FROM `tableofficeasset`
WHERE `AssetCode` IS NOT NULL
  AND TRIM(`AssetCode`) <> ''
GROUP BY `AssetCode`
HAVING COUNT(*) > 1;

ALTER TABLE `tableofficeasset`
    ADD COLUMN `AssignedEmployeeId` INT NULL AFTER `AssignedTo`,
    ADD COLUMN `WarrantyStartDate` DATETIME NULL AFTER `Status`,
    ADD COLUMN `WarrantyEndDate` DATETIME NULL AFTER `WarrantyStartDate`,
    ADD COLUMN `DisposalDate` DATETIME NULL AFTER `WarrantyEndDate`;

CREATE TABLE `tableofficeassetassignmenthistory` (
    `OfficeAssetAssignmentHistoryId` INT NOT NULL AUTO_INCREMENT,
    `OfficeAssetId` INT NOT NULL,
    `EmployeeId` INT NOT NULL,
    `AssignedOn` DATETIME NOT NULL,
    `ReturnedOn` DATETIME NULL,
    `AssignedByUserId` VARCHAR(450) NOT NULL,
    `ReturnedByUserId` VARCHAR(450) NULL,
    `ConditionOnAssignment` VARCHAR(200) NULL,
    `ConditionOnReturn` VARCHAR(200) NULL,
    `Remarks` VARCHAR(500) NULL,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    `CreatedOn` DATETIME NOT NULL,
    PRIMARY KEY (`OfficeAssetAssignmentHistoryId`),
    CONSTRAINT `FK_tableofficeassetassignmenthistory_tableofficeasset`
        FOREIGN KEY (`OfficeAssetId`)
        REFERENCES `tableofficeasset` (`OfficeAssetId`)
        ON DELETE CASCADE,
    CONSTRAINT `FK_tableofficeassetassignmenthistory_TableEmployee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `TableEmployee` (`Id`)
        ON DELETE RESTRICT
);

ALTER TABLE `tableofficeasset`
    ADD CONSTRAINT `FK_tableofficeasset_TableEmployee_AssignedEmployeeId`
        FOREIGN KEY (`AssignedEmployeeId`)
        REFERENCES `TableEmployee` (`Id`)
        ON DELETE SET NULL;

CREATE UNIQUE INDEX `UX_tableofficeasset_AssetCode`
    ON `tableofficeasset` (`AssetCode`);

CREATE INDEX `IX_tableofficeasset_IsActive`
    ON `tableofficeasset` (`IsActive`);

CREATE INDEX `IX_tableofficeasset_AssignedEmployeeId`
    ON `tableofficeasset` (`AssignedEmployeeId`);

CREATE INDEX `IX_tableofficeassetassignmenthistory_OfficeAssetId_IsActive`
    ON `tableofficeassetassignmenthistory` (`OfficeAssetId`, `IsActive`);

CREATE INDEX `IX_tableofficeassetassignmenthistory_EmployeeId`
    ON `tableofficeassetassignmenthistory` (`EmployeeId`);
