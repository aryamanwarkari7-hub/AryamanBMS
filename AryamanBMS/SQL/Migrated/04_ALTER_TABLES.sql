-- ADD MIDDLE NAME--
-- ALTER TABLE `tableemployee`
-- ADD COLUMN `MiddleName` VARCHAR(100) NULL
-- AFTER `FirstName`;

-- ESIC NO. ADD --
-- ALTER TABLE `TableEmployee`
-- ADD COLUMN `EsicNo` VARCHAR(10) NULL
-- AFTER `UanNo`;

-- CREATE INDEX `IX_TableEmployee_EsicNo`
-- ON `TableEmployee` (`EsicNo`);


-- LOCAL ADDRESS ADD --
-- ALTER TABLE `TableEmployee`
-- ADD COLUMN `LocalAddress` TEXT NULL
--     AFTER `PermanentAddress`,

-- ADD COLUMN `LocalCity` VARCHAR(100) NULL
--     AFTER `LocalAddress`,

-- ADD COLUMN `LocalState` VARCHAR(100) NULL
--     AFTER `LocalCity`,

-- ADD COLUMN `LocalPinCode` VARCHAR(6) NULL
--     AFTER `LocalState`;

-- DOC CATEGORY ADD --
-- ALTER TABLE `TableEmployeeDocument`
-- ADD COLUMN `DocumentCategory` VARCHAR(30) NULL
-- AFTER `EmployeeAcademicId`;

-- EXISITING ALL ACADEMIC
-- UPDATE `TableEmployeeDocument`
-- SET `DocumentCategory` = 'Academic'
-- WHERE `DocumentCategory` IS NULL;

-- CATEGORY COMPULSORY --
-- ALTER TABLE `TableEmployeeDocument`
-- MODIFY COLUMN `DocumentCategory` VARCHAR(30) NOT NULL;

-- CREATE INDEX `IX_TableEmployeeDocument_Employee_Category`
-- ON `TableEmployeeDocument`
-- (
--     `EmployeeId`,
--     `DocumentCategory`
-- );

-- ALTER TABLE `TableEmployeeDocument`
-- ADD COLUMN `EmployeePreviousEmploymentId` INT NULL
-- AFTER `EmployeeAcademicId`;

-- CREATE INDEX `IX_EmployeeDocument_PreviousEmploymentId`
-- ON `TableEmployeeDocument`
-- (`EmployeePreviousEmploymentId`);

-- ADD PREVIOUS EMP. DOC --
-- ALTER TABLE `TableEmployeeDocument`
-- ADD CONSTRAINT `FK_EmployeeDocument_PreviousEmployment`
-- FOREIGN KEY (`EmployeePreviousEmploymentId`)
-- REFERENCES `TableEmployeePreviousEmployment` (`Id`)
-- ON DELETE CASCADE;

