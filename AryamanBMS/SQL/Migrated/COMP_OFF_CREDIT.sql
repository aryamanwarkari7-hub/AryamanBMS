USE `aryamanbms`;

CREATE TABLE IF NOT EXISTS `tablecompoffcredit`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `EmployeeId` INT NOT NULL,

    `WorkedDate` DATE NOT NULL,

    `CreditDays` DECIMAL(10,2) NOT NULL DEFAULT 1.00,

    `ExpiryDate` DATE NOT NULL,

    `Status` VARCHAR(20) NOT NULL DEFAULT 'Pending',

    `AttendanceId` INT NULL,

    `RequestedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `RequestedBy` VARCHAR(255) NULL,

    `ApprovedOn` DATETIME NULL,
    `ApprovedBy` VARCHAR(255) NULL,

    `RejectedOn` DATETIME NULL,
    `RejectedBy` VARCHAR(255) NULL,

    `Remarks` VARCHAR(500) NULL,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_CompOffCredit_Employee_WorkedDate`
        (`EmployeeId`, `WorkedDate`),

    KEY `IX_CompOffCredit_EmployeeId`
        (`EmployeeId`),

    KEY `IX_CompOffCredit_Status`
        (`Status`),

    KEY `IX_CompOffCredit_ExpiryDate`
        (`ExpiryDate`),

    KEY `IX_CompOffCredit_AttendanceId`
        (`AttendanceId`),

    CONSTRAINT `FK_CompOffCredit_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT `FK_CompOffCredit_Attendance`
        FOREIGN KEY (`AttendanceId`)
        REFERENCES `tableattendance` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;