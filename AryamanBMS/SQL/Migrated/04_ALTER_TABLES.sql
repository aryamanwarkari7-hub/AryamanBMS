-- ALTER TABLE `tableemployee`
-- ADD COLUMN `MiddleName` VARCHAR(100) NULL
-- AFTER `FirstName`;

ALTER TABLE `TableEmployee`
ADD COLUMN `EsicNo` VARCHAR(10) NULL
AFTER `UanNo`;

CREATE INDEX `IX_TableEmployee_EsicNo`
ON `TableEmployee` (`EsicNo`);