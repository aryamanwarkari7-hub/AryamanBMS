-- Expense Voucher secure document storage
-- Run after 06_CREATE_ACCOUNTS_TABLES.sql.

CREATE TABLE IF NOT EXISTS `tableexpensevoucherdocument` (
    `ExpenseVoucherDocumentId` INT NOT NULL AUTO_INCREMENT,
    `ExpenseVoucherId` INT NOT NULL,
    `DocumentType` VARCHAR(50) NOT NULL,
    `OriginalFileName` VARCHAR(255) NOT NULL,
    `StoredFilePath` VARCHAR(500) NOT NULL,
    `Remarks` VARCHAR(500) NULL,
    `UploadedByUserId` VARCHAR(450) NOT NULL,
    `UploadedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    PRIMARY KEY (`ExpenseVoucherDocumentId`),
    INDEX `IX_tableexpensevoucherdocument_ExpenseVoucherId` (`ExpenseVoucherId`),
    INDEX `IX_tableexpensevoucherdocument_DocumentType` (`DocumentType`),
    INDEX `IX_tableexpensevoucherdocument_IsActive` (`IsActive`),
    CONSTRAINT `FK_tableexpensevoucherdocument_tableexpensevouchers_ExpenseVoucherId`
        FOREIGN KEY (`ExpenseVoucherId`)
        REFERENCES `tableexpensevouchers` (`ExpenseVoucherId`)
        ON DELETE RESTRICT
);