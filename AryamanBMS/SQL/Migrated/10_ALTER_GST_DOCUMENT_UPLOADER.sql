-- GST document uploader audit field
-- Run after tablegstdocument exists.

ALTER TABLE `tablegstdocument`
ADD COLUMN `UploadedByUserId` VARCHAR(450) NULL;

CREATE INDEX `IX_tablegstdocument_SnapshotId`
ON `tablegstdocument` (`SnapshotId`);

CREATE INDEX `IX_tablegstdocument_DocumentType`
ON `tablegstdocument` (`DocumentType`);
