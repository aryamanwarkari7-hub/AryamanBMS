-- ALTER TABLE `tableemployee`
-- ADD COLUMN `MiddleName` VARCHAR(100) NULL
-- AFTER `FirstName`;

-- ALTER TABLE `TableEmployee`
-- ADD COLUMN `EsicNo` VARCHAR(10) NULL
-- AFTER `UanNo`;

-- CREATE INDEX `IX_TableEmployee_EsicNo`
-- ON `TableEmployee` (`EsicNo`);

ALTER TABLE `TableEmployee`
ADD COLUMN `LocalAddress` TEXT NULL
    AFTER `PermanentAddress`,

ADD COLUMN `LocalCity` VARCHAR(100) NULL
    AFTER `LocalAddress`,

ADD COLUMN `LocalState` VARCHAR(100) NULL
    AFTER `LocalCity`,

ADD COLUMN `LocalPinCode` VARCHAR(6) NULL
    AFTER `LocalState`;