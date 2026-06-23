-- ============================================================
-- ARYAMAN BMS
-- FILE    : 02_CREATE_ALL_TABLES.sql
-- PURPOSE : Creates all AryamanBMS tables
-- RUN     : Run after 01_CREATE_DATABASE.sql
-- NOTE    : Structure only; no data is inserted
-- ============================================================

USE `aryamanbms`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
SET UNIQUE_CHECKS = 0;
SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';


-- ============================================================
-- TABLE   : __EFMigrationsHistory
-- MODULE  : System
-- PURPOSE : Stores Entity Framework migration history
-- DEPENDS : None
-- ============================================================

DROP TABLE IF EXISTS `__efmigrationshistory`;

CREATE TABLE `__efmigrationshistory`
(
    `MigrationId` VARCHAR(150) NOT NULL,
    `ProductVersion` VARCHAR(32) NOT NULL,

    PRIMARY KEY (`MigrationId`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetRoles
-- MODULE  : Identity
-- PURPOSE : Stores application roles
-- DEPENDS : None
-- ============================================================

DROP TABLE IF EXISTS `aspnetroles`;

CREATE TABLE `aspnetroles`
(
    `Id` VARCHAR(255) NOT NULL,
    `Name` VARCHAR(256) DEFAULT NULL,
    `NormalizedName` VARCHAR(256) DEFAULT NULL,
    `ConcurrencyStamp` LONGTEXT,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `RoleNameIndex`
        (`NormalizedName`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetUsers
-- MODULE  : Identity
-- PURPOSE : Stores application users
-- DEPENDS : None
-- ============================================================

DROP TABLE IF EXISTS `aspnetusers`;

CREATE TABLE `aspnetusers`
(
    `Id` VARCHAR(255) NOT NULL,
    `FullName` LONGTEXT,
    `UserName` VARCHAR(256) DEFAULT NULL,
    `NormalizedUserName` VARCHAR(256) DEFAULT NULL,
    `Email` VARCHAR(256) DEFAULT NULL,
    `NormalizedEmail` VARCHAR(256) DEFAULT NULL,
    `EmailConfirmed` TINYINT(1) NOT NULL,
    `PasswordHash` LONGTEXT,
    `SecurityStamp` LONGTEXT,
    `ConcurrencyStamp` LONGTEXT,
    `PhoneNumber` LONGTEXT,
    `PhoneNumberConfirmed` TINYINT(1) NOT NULL,
    `TwoFactorEnabled` TINYINT(1) NOT NULL,
    `LockoutEnd` DATETIME(6) DEFAULT NULL,
    `LockoutEnabled` TINYINT(1) NOT NULL,
    `AccessFailedCount` INT NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 0,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UserNameIndex`
        (`NormalizedUserName`),

    KEY `EmailIndex`
        (`NormalizedEmail`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetRoleClaims
-- MODULE  : Identity
-- PURPOSE : Stores claims assigned to roles
-- DEPENDS : AspNetRoles
-- ============================================================

DROP TABLE IF EXISTS `aspnetroleclaims`;

CREATE TABLE `aspnetroleclaims`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RoleId` VARCHAR(255) NOT NULL,
    `ClaimType` LONGTEXT,
    `ClaimValue` LONGTEXT,

    PRIMARY KEY (`Id`),

    KEY `IX_AspNetRoleClaims_RoleId`
        (`RoleId`),

    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId`
        FOREIGN KEY (`RoleId`)
        REFERENCES `aspnetroles` (`Id`)
        ON DELETE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetUserClaims
-- MODULE  : Identity
-- PURPOSE : Stores claims assigned to users
-- DEPENDS : AspNetUsers
-- ============================================================

DROP TABLE IF EXISTS `aspnetuserclaims`;

CREATE TABLE `aspnetuserclaims`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `UserId` VARCHAR(255) NOT NULL,
    `ClaimType` LONGTEXT,
    `ClaimValue` LONGTEXT,

    PRIMARY KEY (`Id`),

    KEY `IX_AspNetUserClaims_UserId`
        (`UserId`),

    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId`
        FOREIGN KEY (`UserId`)
        REFERENCES `aspnetusers` (`Id`)
        ON DELETE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetUserLogins
-- MODULE  : Identity
-- PURPOSE : Stores external login providers for users
-- DEPENDS : AspNetUsers
-- ============================================================

DROP TABLE IF EXISTS `aspnetuserlogins`;

CREATE TABLE `aspnetuserlogins`
(
    `LoginProvider` VARCHAR(255) NOT NULL,
    `ProviderKey` VARCHAR(255) NOT NULL,
    `ProviderDisplayName` LONGTEXT,
    `UserId` VARCHAR(255) NOT NULL,

    PRIMARY KEY (`LoginProvider`, `ProviderKey`),

    KEY `IX_AspNetUserLogins_UserId`
        (`UserId`),

    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId`
        FOREIGN KEY (`UserId`)
        REFERENCES `aspnetusers` (`Id`)
        ON DELETE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetUserRoles
-- MODULE  : Identity
-- PURPOSE : Maps users to roles
-- DEPENDS : AspNetUsers, AspNetRoles
-- ============================================================

DROP TABLE IF EXISTS `aspnetuserroles`;

CREATE TABLE `aspnetuserroles`
(
    `UserId` VARCHAR(255) NOT NULL,
    `RoleId` VARCHAR(255) NOT NULL,

    PRIMARY KEY (`UserId`, `RoleId`),

    KEY `IX_AspNetUserRoles_RoleId`
        (`RoleId`),

    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId`
        FOREIGN KEY (`RoleId`)
        REFERENCES `aspnetroles` (`Id`)
        ON DELETE CASCADE,

    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId`
        FOREIGN KEY (`UserId`)
        REFERENCES `aspnetusers` (`Id`)
        ON DELETE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : AspNetUserTokens
-- MODULE  : Identity
-- PURPOSE : Stores authentication tokens for users
-- DEPENDS : AspNetUsers
-- ============================================================

DROP TABLE IF EXISTS `aspnetusertokens`;

CREATE TABLE `aspnetusertokens`
(
    `UserId` VARCHAR(255) NOT NULL,
    `LoginProvider` VARCHAR(255) NOT NULL,
    `Name` VARCHAR(255) NOT NULL,
    `Value` LONGTEXT,

    PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),

    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId`
        FOREIGN KEY (`UserId`)
        REFERENCES `aspnetusers` (`Id`)
        ON DELETE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableDepartment
-- MODULE  : HR
-- PURPOSE : Stores department master records
-- DEPENDS : None
-- ============================================================

DROP TABLE IF EXISTS `tabledepartment`;

CREATE TABLE `tabledepartment`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DisplayCode` VARCHAR(20) DEFAULT NULL,
    `DepartmentName` VARCHAR(100) NOT NULL,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableDesignation
-- MODULE  : HR
-- PURPOSE : Stores designation master records
-- DEPENDS : TableDepartment
-- ============================================================

DROP TABLE IF EXISTS `tabledesignation`;

CREATE TABLE `tabledesignation`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DisplayCode` VARCHAR(20) DEFAULT NULL,
    `DesignationName` VARCHAR(100) NOT NULL,
    `DepartmentId` INT NOT NULL,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `FK_Designation_Department`
        (`DepartmentId`),

    CONSTRAINT `FK_Designation_Department`
        FOREIGN KEY (`DepartmentId`)
        REFERENCES `tabledepartment` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableEmployee
-- MODULE  : HR
-- PURPOSE : Stores employee master and personal details
-- DEPENDS : TableDepartment, TableDesignation, AspNetUsers
-- ============================================================

DROP TABLE IF EXISTS `tableemployee`;

CREATE TABLE `tableemployee`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeCode` VARCHAR(20) NOT NULL,
    `FirstName` VARCHAR(100) NOT NULL,
    `LastName` VARCHAR(100) DEFAULT NULL,
    `MobileNumber` VARCHAR(20) DEFAULT NULL,
    `JoiningDate` DATE NOT NULL,
    `DepartmentId` INT NOT NULL,
    `DesignationId` INT NOT NULL,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',
    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,
    `ApplicationUserId` VARCHAR(255) DEFAULT NULL,
    `DateOfBirth` DATE DEFAULT NULL,
    `Gender` VARCHAR(20) DEFAULT NULL,
    `PersonalEmail` VARCHAR(150) DEFAULT NULL,
    `OfficialEmail` VARCHAR(150) DEFAULT NULL,
    `PermanentAddress` TEXT,
    `City` VARCHAR(100) DEFAULT NULL,
    `State` VARCHAR(100) DEFAULT NULL,
    `PinCode` VARCHAR(20) DEFAULT NULL,
    `EmergencyContact` VARCHAR(150) DEFAULT NULL,
    `EmergencyPhone` VARCHAR(20) DEFAULT NULL,
    `AadhaarNo` VARCHAR(20) DEFAULT NULL,
    `PanNo` VARCHAR(20) DEFAULT NULL,
    `UanNo` VARCHAR(30) DEFAULT NULL,
    `EmploymentType` VARCHAR(50) DEFAULT NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_EmployeeCode`
        (`EmployeeCode`),

    KEY `FK_Employee_Department`
        (`DepartmentId`),

    KEY `FK_Employee_Designation`
        (`DesignationId`),

    KEY `IX_TableEmployee_ApplicationUserId`
        (`ApplicationUserId`),

    KEY `IX_TableEmployee_AadhaarNo`
        (`AadhaarNo`),

    KEY `IX_TableEmployee_PanNo`
        (`PanNo`),

    KEY `IX_TableEmployee_UanNo`
        (`UanNo`),

    CONSTRAINT `FK_Employee_AspNetUsers`
        FOREIGN KEY (`ApplicationUserId`)
        REFERENCES `aspnetusers` (`Id`)
        ON DELETE SET NULL,

    CONSTRAINT `FK_Employee_Department`
        FOREIGN KEY (`DepartmentId`)
        REFERENCES `tabledepartment` (`Id`),

    CONSTRAINT `FK_Employee_Designation`
        FOREIGN KEY (`DesignationId`)
        REFERENCES `tabledesignation` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableEmployeeAcademic
-- MODULE  : HR
-- PURPOSE : Stores employee academic qualifications
-- DEPENDS : TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableemployeeacademic`;

CREATE TABLE `tableemployeeacademic`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeId` INT NOT NULL,
    `QualificationLevel` VARCHAR(100) NOT NULL,
    `CourseName` VARCHAR(150) DEFAULT NULL,
    `Specialization` VARCHAR(150) DEFAULT NULL,
    `InstituteName` VARCHAR(200) DEFAULT NULL,
    `BoardOrUniversity` VARCHAR(200) DEFAULT NULL,
    `PassingYear` INT DEFAULT NULL,
    `ResultType` VARCHAR(30) DEFAULT NULL,
    `Score` DECIMAL(6,2) DEFAULT NULL,
    `Grade` VARCHAR(20) DEFAULT NULL,
    `IsHighestQualification` TINYINT(1) NOT NULL DEFAULT 0,

    PRIMARY KEY (`Id`),

    KEY `IX_EmployeeAcademic_EmployeeId`
        (`EmployeeId`),

    CONSTRAINT `FK_EmployeeAcademic_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci;


-- ============================================================
-- TABLE   : TableEmployeeDocument
-- MODULE  : HR
-- PURPOSE : Stores employee document metadata
-- DEPENDS : TableEmployee, TableEmployeeAcademic
-- ============================================================

DROP TABLE IF EXISTS `tableemployeedocument`;

CREATE TABLE `tableemployeedocument`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeId` INT NOT NULL,
    `EmployeeAcademicId` INT DEFAULT NULL,
    `DocumentType` VARCHAR(100) NOT NULL,
    `OriginalFileName` VARCHAR(255) NOT NULL,
    `StoredFileName` VARCHAR(255) NOT NULL,
    `StoragePath` VARCHAR(500) NOT NULL,
    `ContentType` VARCHAR(100) DEFAULT NULL,
    `FileSize` BIGINT NOT NULL,
    `UploadedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UploadedBy` VARCHAR(256) DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_EmployeeDocument_EmployeeId`
        (`EmployeeId`),

    KEY `IX_EmployeeDocument_AcademicId`
        (`EmployeeAcademicId`),

    CONSTRAINT `FK_EmployeeDocument_Academic`
        FOREIGN KEY (`EmployeeAcademicId`)
        REFERENCES `tableemployeeacademic` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE,

    CONSTRAINT `FK_EmployeeDocument_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_unicode_ci;


-- ============================================================
-- TABLE   : TableAttendance
-- MODULE  : Attendance
-- PURPOSE : Stores daily employee attendance
-- DEPENDS : TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableattendance`;

CREATE TABLE `tableattendance`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeId` INT NOT NULL,
    `AttendanceDate` DATE NOT NULL,
    `CheckInTime` DATETIME DEFAULT NULL,
    `CheckOutTime` DATETIME DEFAULT NULL,
    `Remarks` VARCHAR(500) DEFAULT NULL,
    `CreatedOn` DATETIME NOT NULL,
    `LocationType` VARCHAR(50) DEFAULT NULL,
    `Status` VARCHAR(10) NOT NULL DEFAULT 'P',

    PRIMARY KEY (`Id`),

    KEY `FK_Attendance_Employee`
        (`EmployeeId`),

    CONSTRAINT `FK_Attendance_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableLeaveTypes
-- MODULE  : Leave
-- PURPOSE : Stores leave type master records
-- DEPENDS : None
-- ============================================================

DROP TABLE IF EXISTS `tableleavetypes`;

CREATE TABLE `tableleavetypes`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `LeaveCode` VARCHAR(20) NOT NULL,
    `LeaveName` VARCHAR(100) NOT NULL,
    `DaysPerYear` INT NOT NULL DEFAULT 0,
    `IsCarryForward` BIT(1) NOT NULL DEFAULT b'0',
    `IsPaidLeave` BIT(1) NOT NULL DEFAULT b'1',
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',

    PRIMARY KEY (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableLeaveBalances
-- MODULE  : Leave
-- PURPOSE : Stores yearly employee leave balances
-- DEPENDS : TableEmployee, TableLeaveTypes
-- ============================================================

DROP TABLE IF EXISTS `tableleavebalances`;

CREATE TABLE `tableleavebalances`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeId` INT NOT NULL,
    `LeaveTypeId` INT NOT NULL,
    `LeaveYear` INT NOT NULL,
    `AllocatedDays` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `UsedDays` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `BalanceDays` DECIMAL(10,2) NOT NULL DEFAULT 0.00,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UK_TableLeaveBalances`
        (`EmployeeId`, `LeaveTypeId`, `LeaveYear`),

    KEY `IX_LeaveBalances_LeaveTypeId`
        (`LeaveTypeId`),

    KEY `IX_LeaveBalances_EmployeeId`
        (`EmployeeId`),

    CONSTRAINT `FK_LeaveBalances_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`),

    CONSTRAINT `FK_LeaveBalances_LeaveType`
        FOREIGN KEY (`LeaveTypeId`)
        REFERENCES `tableleavetypes` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableLeaveApplications
-- MODULE  : Leave
-- PURPOSE : Stores employee leave applications
-- DEPENDS : TableEmployee, TableLeaveTypes
-- ============================================================

DROP TABLE IF EXISTS `tableleaveapplications`;

CREATE TABLE `tableleaveapplications`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ApplicationNumber` VARCHAR(50) NOT NULL,
    `EmployeeId` INT NOT NULL,
    `LeaveTypeId` INT NOT NULL,
    `FromDate` DATE NOT NULL,
    `ToDate` DATE NOT NULL,
    `NumberOfDays` DECIMAL(10,2) NOT NULL,
    `Reason` VARCHAR(1000) DEFAULT NULL,
    `AppliedOn` DATETIME NOT NULL,
    `Status` VARCHAR(20) NOT NULL,
    `ApprovedBy` VARCHAR(100) DEFAULT NULL,
    `ApprovedOn` DATETIME DEFAULT NULL,
    `ApprovalRemarks` VARCHAR(1000) DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_LeaveApplications_LeaveTypeId`
        (`LeaveTypeId`),

    KEY `IX_LeaveApplications_EmployeeId`
        (`EmployeeId`),

    KEY `IX_LeaveApplications_Status`
        (`Status`),

    KEY `IX_LeaveApplications_FromDate`
        (`FromDate`),

    CONSTRAINT `FK_LeaveApplications_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`),

    CONSTRAINT `FK_LeaveApplications_LeaveType`
        FOREIGN KEY (`LeaveTypeId`)
        REFERENCES `tableleavetypes` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableLetters
-- MODULE  : Letters
-- PURPOSE : Stores employee letters and generated documents
-- DEPENDS : TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableletters`;

CREATE TABLE `tableletters`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `LetterNumber` VARCHAR(50) NOT NULL,
    `LetterType` VARCHAR(50) NOT NULL,
    `EmployeeId` INT NOT NULL,
    `Subject` VARCHAR(200) NOT NULL,
    `Body` TEXT NOT NULL,
    `DocumentPath` VARCHAR(500) DEFAULT NULL,
    `IssuedBy` VARCHAR(100) DEFAULT NULL,
    `IssuedOn` DATETIME NOT NULL,
    `IsActive` BIT(1) NOT NULL DEFAULT b'1',

    PRIMARY KEY (`Id`),

    KEY `FK_Letters_Employee`
        (`EmployeeId`),

    CONSTRAINT `FK_Letters_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableSalaryRecord
-- MODULE  : Salary
-- PURPOSE : Stores monthly employee salary records
-- DEPENDS : TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tablesalaryrecord`;

CREATE TABLE `tablesalaryrecord`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `EmployeeId` INT NOT NULL,
    `Month` INT NOT NULL,
    `Year` INT NOT NULL,
    `ActualSalary` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `PayDays` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `BasicSalary` DECIMAL(18,2) NOT NULL,
    `HRA` DECIMAL(18,2) NOT NULL,
    `Conveyance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `MedicalAllowance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `EducationAllowance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `SpecialAllowance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `DA` DECIMAL(18,2) NOT NULL,
    `OtherAllowances` DECIMAL(18,2) NOT NULL,
    `GrossSalary` DECIMAL(18,2) NOT NULL,
    `TotalEarnings` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `GrossMinusConveyance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `PfDeduction` DECIMAL(18,2) NOT NULL,
    `EsicDeduction` DECIMAL(18,2) NOT NULL,
    `ProfessionalTax` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `Advance` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `TotalDeductions` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `TdsDeduction` DECIMAL(18,2) NOT NULL,
    `OtherDeductions` DECIMAL(18,2) NOT NULL,
    `NetSalary` DECIMAL(18,2) NOT NULL,
    `EmployerPf` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `EmployerEsic` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `CTC` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `PaymentStatus` VARCHAR(20) NOT NULL DEFAULT 'Pending',
    `PaidOn` DATETIME DEFAULT NULL,
    `PresentDays` INT NOT NULL DEFAULT 0,
    `LeaveDays` INT NOT NULL DEFAULT 0,
    `AbsentDays` INT NOT NULL DEFAULT 0,
    `SourceFileName` VARCHAR(255) DEFAULT NULL,
    `ImportedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Remark` VARCHAR(500) DEFAULT NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_SalaryRecord_Employee_Month_Year`
        (`EmployeeId`, `Month`, `Year`),

    CONSTRAINT `FK_SalaryRecord_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableProject
-- MODULE  : Project Management
-- PURPOSE : Stores project master records
-- DEPENDS : TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableproject`;

CREATE TABLE `tableproject`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProjectCode` VARCHAR(20) NOT NULL,
    `ProjectName` VARCHAR(150) NOT NULL,
    `ProjectType` VARCHAR(50) NOT NULL,
    `ClientName` VARCHAR(150) DEFAULT NULL,
    `BusinessObjective` TEXT,
    `Scope` TEXT,
    `StartDate` DATE DEFAULT NULL,
    `EndDate` DATE DEFAULT NULL,
    `Budget` DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    `Priority` VARCHAR(20) NOT NULL DEFAULT 'Medium',
    `Status` VARCHAR(30) NOT NULL DEFAULT 'Planning',
    `ProjectManagerId` INT NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_TableProject_ProjectCode`
        (`ProjectCode`),

    KEY `IX_TableProject_ProjectManagerId`
        (`ProjectManagerId`),

    KEY `IX_TableProject_Status`
        (`Status`),

    KEY `IX_TableProject_IsActive`
        (`IsActive`),

    CONSTRAINT `FK_TableProject_ProjectManager`
        FOREIGN KEY (`ProjectManagerId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableProjectMember
-- MODULE  : Project Management
-- PURPOSE : Stores employees assigned to projects
-- DEPENDS : TableProject, TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableprojectmember`;

CREATE TABLE `tableprojectmember`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProjectId` INT NOT NULL,
    `EmployeeId` INT NOT NULL,
    `RoleInProject` VARCHAR(100) DEFAULT NULL,
    `AssignedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_TableProjectMember_Project_Employee`
        (`ProjectId`, `EmployeeId`),

    KEY `IX_TableProjectMember_ProjectId`
        (`ProjectId`),

    KEY `IX_TableProjectMember_EmployeeId`
        (`EmployeeId`),

    CONSTRAINT `FK_TableProjectMember_Project`
        FOREIGN KEY (`ProjectId`)
        REFERENCES `tableproject` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `FK_TableProjectMember_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


SET FOREIGN_KEY_CHECKS = 1;
SET UNIQUE_CHECKS = 1;

-- ============================================================
-- TABLE   : TableProjectTask
-- MODULE  : Project Management
-- PURPOSE : Stores tasks created under projects
-- DEPENDS : TableProject, TableEmployee
-- ============================================================

DROP TABLE IF EXISTS `tableprojecttask`;

CREATE TABLE `tableprojecttask`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `ProjectId` INT NOT NULL,
    `AssignedEmployeeId` INT DEFAULT NULL,

    `TaskCode` VARCHAR(30) NOT NULL,
    `TaskTitle` VARCHAR(200) NOT NULL,
    `Description` TEXT DEFAULT NULL,

    `StartDate` DATE DEFAULT NULL,
    `DueDate` DATE DEFAULT NULL,

    `Priority` VARCHAR(20) NOT NULL DEFAULT 'Medium',
    `Status` VARCHAR(30) NOT NULL DEFAULT 'Not Started',

    `ProgressPercent` INT NOT NULL DEFAULT 0,

    `EstimatedHours` DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    `ActualHours` DECIMAL(10,2) NOT NULL DEFAULT 0.00,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_TableProjectTask_Project_TaskCode`
        (`ProjectId`, `TaskCode`),

    KEY `IX_TableProjectTask_ProjectId`
        (`ProjectId`),

    KEY `IX_TableProjectTask_AssignedEmployeeId`
        (`AssignedEmployeeId`),

    KEY `IX_TableProjectTask_Status`
        (`Status`),

    CONSTRAINT `FK_TableProjectTask_Project`
        FOREIGN KEY (`ProjectId`)
        REFERENCES `tableproject` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `FK_TableProjectTask_AssignedEmployee`
        FOREIGN KEY (`AssignedEmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : TableProjectFlow
-- MODULE  : Project Management
-- PURPOSE : Stores flow of tasks created under projects
-- DEPENDS : TableProject
-- ============================================================

CREATE TABLE IF NOT EXISTS `tableprojectflow`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `ProjectId` INT NOT NULL,

    `StageName` VARCHAR(100) NOT NULL,
    `StageOrder` INT NOT NULL,

    `StageStatus` VARCHAR(30) NOT NULL DEFAULT 'Pending',

    `PlannedStartDate` DATE DEFAULT NULL,
    `PlannedEndDate` DATE DEFAULT NULL,

    `ActualStartDate` DATE DEFAULT NULL,
    `ActualEndDate` DATE DEFAULT NULL,

    `Remarks` VARCHAR(1000) DEFAULT NULL,

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_TableProjectFlow_Project_StageOrder`
        (`ProjectId`, `StageOrder`),

    KEY `IX_TableProjectFlow_ProjectId`
        (`ProjectId`),

    KEY `IX_TableProjectFlow_StageStatus`
        (`StageStatus`),

    CONSTRAINT `FK_TableProjectFlow_Project`
        FOREIGN KEY (`ProjectId`)
        REFERENCES `tableproject` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- TABLE   : Table Project Task Progress
-- MODULE  : Project Management
-- PURPOSE : Stores progress of tasks created under projects
-- DEPENDS : TableProject, TableProjectTasks
-- ============================================================

CREATE TABLE IF NOT EXISTS `tableprojecttaskprogress`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `ProjectTaskId` INT NOT NULL,

    `ProgressDate` DATE NOT NULL,

    `HoursWorked` DECIMAL(5,2) NOT NULL DEFAULT 0.00,

    `CompletionPercentage` INT NOT NULL DEFAULT 0,

    `ProgressNotes` VARCHAR(1000) NOT NULL,

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_ProjectTaskProgress_ProjectTaskId`
        (`ProjectTaskId`),

    KEY `IX_ProjectTaskProgress_ProgressDate`
        (`ProgressDate`),

    CONSTRAINT `FK_ProjectTaskProgress_ProjectTask`
        FOREIGN KEY (`ProjectTaskId`)
        REFERENCES `tableprojecttask` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `CK_ProjectTaskProgress_HoursWorked`
        CHECK (`HoursWorked` >= 0 AND `HoursWorked` <= 24),

    CONSTRAINT `CK_ProjectTaskProgress_CompletionPercentage`
        CHECK (`CompletionPercentage` >= 0
               AND `CompletionPercentage` <= 100)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;

-- ============================================================
-- TABLE   : Table Project MOM Tables
-- MODULE  : Project Management
-- PURPOSE : Stores meeting,attendees under projects
-- DEPENDS : TableProject, TableProjectTasks
-- ============================================================

CREATE TABLE IF NOT EXISTS `tableprojectmeeting`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `ProjectId` INT NOT NULL,

    `MeetingTitle` VARCHAR(200) NOT NULL,

    `MeetingDate` DATE NOT NULL,

    `StartTime` TIME NOT NULL,
    `EndTime` TIME DEFAULT NULL,

    `MeetingMode` VARCHAR(30) NOT NULL DEFAULT 'In Person',

    `LocationOrLink` VARCHAR(300) DEFAULT NULL,

    `Agenda` VARCHAR(2000) NOT NULL,

    `DiscussionSummary` VARCHAR(4000) DEFAULT NULL,

    `DecisionsTaken` VARCHAR(3000) DEFAULT NULL,

    `NextMeetingDate` DATE DEFAULT NULL,

    `MeetingStatus` VARCHAR(30) NOT NULL DEFAULT 'Scheduled',

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_ProjectMeeting_ProjectId` (`ProjectId`),

    KEY `IX_ProjectMeeting_MeetingDate` (`MeetingDate`),

    KEY `IX_ProjectMeeting_Status` (`MeetingStatus`),

    CONSTRAINT `FK_ProjectMeeting_Project`
        FOREIGN KEY (`ProjectId`)
        REFERENCES `tableproject` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `tableprojectmeetingattendee`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `MeetingId` INT NOT NULL,

    `EmployeeId` INT NOT NULL,

    `IsPresent` TINYINT(1) NOT NULL DEFAULT 1,

    `Remarks` VARCHAR(500) DEFAULT NULL,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (`Id`),

    UNIQUE KEY `UQ_MeetingAttendee_Meeting_Employee`
        (`MeetingId`, `EmployeeId`),

    KEY `IX_MeetingAttendee_MeetingId`
        (`MeetingId`),

    KEY `IX_MeetingAttendee_EmployeeId`
        (`EmployeeId`),

    CONSTRAINT `FK_MeetingAttendee_Meeting`
        FOREIGN KEY (`MeetingId`)
        REFERENCES `tableprojectmeeting` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `FK_MeetingAttendee_Employee`
        FOREIGN KEY (`EmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;


CREATE TABLE IF NOT EXISTS `tableprojectmeetingactionitem`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `MeetingId` INT NOT NULL,

    `ActionTitle` VARCHAR(250) NOT NULL,

    `Description` VARCHAR(1500) DEFAULT NULL,

    `AssignedEmployeeId` INT DEFAULT NULL,

    `DueDate` DATE DEFAULT NULL,

    `ActionStatus` VARCHAR(30) NOT NULL DEFAULT 'Pending',

    `CompletedOn` DATE DEFAULT NULL,

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_MeetingActionItem_MeetingId`
        (`MeetingId`),

    KEY `IX_MeetingActionItem_AssignedEmployeeId`
        (`AssignedEmployeeId`),

    KEY `IX_MeetingActionItem_Status`
        (`ActionStatus`),

    KEY `IX_MeetingActionItem_DueDate`
        (`DueDate`),

    CONSTRAINT `FK_MeetingActionItem_Meeting`
        FOREIGN KEY (`MeetingId`)
        REFERENCES `tableprojectmeeting` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `FK_MeetingActionItem_Employee`
        FOREIGN KEY (`AssignedEmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;

-- ============================================================
-- TABLE   : Table Project Risk Register
-- MODULE  : Project Management
-- PURPOSE : Stores risks in projects
-- DEPENDS : TableProject, TableEmployee
-- ============================================================
CREATE TABLE IF NOT EXISTS `tableprojectrisk`
(
    `Id` INT NOT NULL AUTO_INCREMENT,

    `ProjectId` INT NOT NULL,

    `RiskOwnerEmployeeId` INT DEFAULT NULL,

    `RiskTitle` VARCHAR(250) NOT NULL,

    `RiskDescription` VARCHAR(2000) DEFAULT NULL,

    `RiskCategory` VARCHAR(50) NOT NULL DEFAULT 'Technical',

    `Probability` INT NOT NULL DEFAULT 1,

    `Impact` INT NOT NULL DEFAULT 1,

    `RiskScore` INT NOT NULL DEFAULT 1,

    `Severity` VARCHAR(20) NOT NULL DEFAULT 'Low',

    `RiskStatus` VARCHAR(30) NOT NULL DEFAULT 'Open',

    `MitigationPlan` VARCHAR(2000) DEFAULT NULL,

    `ContingencyPlan` VARCHAR(2000) DEFAULT NULL,

    `TargetResolutionDate` DATE DEFAULT NULL,

    `ResolvedOn` DATE DEFAULT NULL,

    `ResolutionRemarks` VARCHAR(1000) DEFAULT NULL,

    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,

    `CreatedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    `UpdatedOn` DATETIME DEFAULT NULL,

    PRIMARY KEY (`Id`),

    KEY `IX_ProjectRisk_ProjectId`
        (`ProjectId`),

    KEY `IX_ProjectRisk_RiskOwnerEmployeeId`
        (`RiskOwnerEmployeeId`),

    KEY `IX_ProjectRisk_RiskStatus`
        (`RiskStatus`),

    KEY `IX_ProjectRisk_Severity`
        (`Severity`),

    KEY `IX_ProjectRisk_TargetResolutionDate`
        (`TargetResolutionDate`),

    CONSTRAINT `FK_ProjectRisk_Project`
        FOREIGN KEY (`ProjectId`)
        REFERENCES `tableproject` (`Id`)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT `FK_ProjectRisk_Employee`
        FOREIGN KEY (`RiskOwnerEmployeeId`)
        REFERENCES `tableemployee` (`Id`)
        ON DELETE SET NULL
        ON UPDATE CASCADE,

    CONSTRAINT `CK_ProjectRisk_Probability`
        CHECK (`Probability` BETWEEN 1 AND 5),

    CONSTRAINT `CK_ProjectRisk_Impact`
        CHECK (`Impact` BETWEEN 1 AND 5),

    CONSTRAINT `CK_ProjectRisk_RiskScore`
        CHECK (`RiskScore` BETWEEN 1 AND 25)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;

-- ============================================================
-- TABLE CREATION COMPLETED
-- ============================================================

SHOW TABLES;