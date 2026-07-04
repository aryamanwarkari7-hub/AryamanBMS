-- Group 15 hardening for Company Documents, Notices and PF/ESIC/PT documents.
-- Run statements individually if your MySQL version does not support IF NOT EXISTS.

ALTER TABLE `tablecompanydocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL
AFTER `IsActive`;

ALTER TABLE `tablenotice`
ADD COLUMN `IsActive` BIT(1) NOT NULL DEFAULT b'1'
AFTER `Remarks`;

ALTER TABLE `tablenoticedocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL
AFTER `Remarks`;

ALTER TABLE `tablenoticedocument`
ADD COLUMN `IsActive` BIT(1) NOT NULL DEFAULT b'1'
AFTER `UploadedOn`;

ALTER TABLE `tablepfdocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL
AFTER `Remarks`;

ALTER TABLE `tablepfdocument`
ADD COLUMN `IsActive` BIT(1) NOT NULL DEFAULT b'1'
AFTER `UploadedOn`;

ALTER TABLE `tableesicdocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL
AFTER `Remarks`;

ALTER TABLE `tableesicdocument`
ADD COLUMN `IsActive` BIT(1) NOT NULL DEFAULT b'1'
AFTER `UploadedOn`;

ALTER TABLE `tableptdocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL
AFTER `Remarks`;

ALTER TABLE `tableptdocument`
ADD COLUMN `IsActive` BIT(1) NOT NULL DEFAULT b'1'
AFTER `UploadedOn`;

CREATE INDEX `IX_tablecompanydocument_IsActive`
ON `tablecompanydocument` (`IsActive`);

CREATE INDEX `IX_tablenotice_IsActive`
ON `tablenotice` (`IsActive`);

CREATE INDEX `IX_tablenoticedocument_IsActive`
ON `tablenoticedocument` (`IsActive`);

CREATE INDEX `IX_tablepfdocument_IsActive`
ON `tablepfdocument` (`IsActive`);

CREATE INDEX `IX_tableesicdocument_IsActive`
ON `tableesicdocument` (`IsActive`);

CREATE INDEX `IX_tableptdocument_IsActive`
ON `tableptdocument` (`IsActive`);
