-- ============================================================
-- ARYAMAN BMS
-- FILE    : 02_CREATE_ALL_TABLES.sql
-- PURPOSE : Creates all AryamanBMS tables
-- RUN     : Run after 01_CREATE_DATABASE.sql
-- NOTE    : This file creates structure only; it inserts no data
-- ============================================================

USE `aryamanbms`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
SET UNIQUE_CHECKS = 0;
SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) NOT NULL,
  `FullName` longtext,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `ConcurrencyStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableattendance`
--

DROP TABLE IF EXISTS `tableattendance`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableattendance` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `AttendanceDate` date NOT NULL,
  `CheckInTime` datetime DEFAULT NULL,
  `CheckOutTime` datetime DEFAULT NULL,
  `Remarks` varchar(500) DEFAULT NULL,
  `CreatedOn` datetime NOT NULL,
  `LocationType` varchar(50) DEFAULT NULL,
  `Status` varchar(10) NOT NULL DEFAULT 'P',
  PRIMARY KEY (`Id`),
  KEY `FK_Attendance_Employee` (`EmployeeId`),
  CONSTRAINT `FK_Attendance_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tabledepartment`
--

DROP TABLE IF EXISTS `tabledepartment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tabledepartment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DisplayCode` varchar(20) DEFAULT NULL,
  `DepartmentName` varchar(100) NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tabledesignation`
--

DROP TABLE IF EXISTS `tabledesignation`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tabledesignation` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DisplayCode` varchar(20) DEFAULT NULL,
  `DesignationName` varchar(100) NOT NULL,
  `DepartmentId` int NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_Designation_Department` (`DepartmentId`),
  CONSTRAINT `FK_Designation_Department` FOREIGN KEY (`DepartmentId`) REFERENCES `tabledepartment` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableemployee`
--

DROP TABLE IF EXISTS `tableemployee`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployee` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeCode` varchar(20) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) DEFAULT NULL,
  `MobileNumber` varchar(20) DEFAULT NULL,
  `JoiningDate` date NOT NULL,
  `DepartmentId` int NOT NULL,
  `DesignationId` int NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  `ApplicationUserId` varchar(255) DEFAULT NULL,
  `DateOfBirth` date DEFAULT NULL,
  `Gender` varchar(20) DEFAULT NULL,
  `PersonalEmail` varchar(150) DEFAULT NULL,
  `OfficialEmail` varchar(150) DEFAULT NULL,
  `PermanentAddress` text,
  `City` varchar(100) DEFAULT NULL,
  `State` varchar(100) DEFAULT NULL,
  `PinCode` varchar(20) DEFAULT NULL,
  `EmergencyContact` varchar(150) DEFAULT NULL,
  `EmergencyPhone` varchar(20) DEFAULT NULL,
  `AadhaarNo` varchar(20) DEFAULT NULL,
  `PanNo` varchar(20) DEFAULT NULL,
  `UanNo` varchar(30) DEFAULT NULL,
  `EmploymentType` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_EmployeeCode` (`EmployeeCode`),
  KEY `FK_Employee_Department` (`DepartmentId`),
  KEY `FK_Employee_Designation` (`DesignationId`),
  KEY `IX_TableEmployee_ApplicationUserId` (`ApplicationUserId`),
  KEY `IX_TableEmployee_AadhaarNo` (`AadhaarNo`),
  KEY `IX_TableEmployee_PanNo` (`PanNo`),
  KEY `IX_TableEmployee_UanNo` (`UanNo`),
  CONSTRAINT `FK_Employee_AspNetUsers` FOREIGN KEY (`ApplicationUserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Employee_Department` FOREIGN KEY (`DepartmentId`) REFERENCES `tabledepartment` (`Id`),
  CONSTRAINT `FK_Employee_Designation` FOREIGN KEY (`DesignationId`) REFERENCES `tabledesignation` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableemployeeacademic`
--

DROP TABLE IF EXISTS `tableemployeeacademic`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployeeacademic` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `QualificationLevel` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CourseName` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Specialization` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `InstituteName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `BoardOrUniversity` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PassingYear` int DEFAULT NULL,
  `ResultType` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Score` decimal(6,2) DEFAULT NULL,
  `Grade` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsHighestQualification` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `IX_EmployeeAcademic_EmployeeId` (`EmployeeId`),
  CONSTRAINT `FK_EmployeeAcademic_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableemployeedocument`
--

DROP TABLE IF EXISTS `tableemployeedocument`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployeedocument` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `EmployeeAcademicId` int DEFAULT NULL,
  `DocumentType` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `OriginalFileName` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `StoredFileName` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `StoragePath` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ContentType` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `FileSize` bigint NOT NULL,
  `UploadedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UploadedBy` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EmployeeDocument_EmployeeId` (`EmployeeId`),
  KEY `IX_EmployeeDocument_AcademicId` (`EmployeeAcademicId`),
  CONSTRAINT `FK_EmployeeDocument_Academic` FOREIGN KEY (`EmployeeAcademicId`) REFERENCES `tableemployeeacademic` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_EmployeeDocument_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableleaveapplications`
--

DROP TABLE IF EXISTS `tableleaveapplications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleaveapplications` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ApplicationNumber` varchar(50) NOT NULL,
  `EmployeeId` int NOT NULL,
  `LeaveTypeId` int NOT NULL,
  `FromDate` date NOT NULL,
  `ToDate` date NOT NULL,
  `NumberOfDays` decimal(10,2) NOT NULL,
  `Reason` varchar(1000) DEFAULT NULL,
  `AppliedOn` datetime NOT NULL,
  `Status` varchar(20) NOT NULL,
  `ApprovedBy` varchar(100) DEFAULT NULL,
  `ApprovedOn` datetime DEFAULT NULL,
  `ApprovalRemarks` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_LeaveApplications_tableleaveType` (`LeaveTypeId`),
  KEY `IX_LeaveApplications_EmployeeId` (`EmployeeId`),
  KEY `IX_LeaveApplications_Status` (`Status`),
  KEY `IX_LeaveApplications_FromDate` (`FromDate`),
  CONSTRAINT `FK_LeaveApplications_tableemployee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`),
  CONSTRAINT `FK_LeaveApplications_tableleaveType` FOREIGN KEY (`LeaveTypeId`) REFERENCES `tableleavetypes` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableleavebalances`
--

DROP TABLE IF EXISTS `tableleavebalances`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleavebalances` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `LeaveTypeId` int NOT NULL,
  `LeaveYear` int NOT NULL,
  `AllocatedDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  `UsedDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  `BalanceDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_tableleavebalances` (`EmployeeId`,`LeaveTypeId`,`LeaveYear`),
  KEY `FK_LeaveBalances_tableleavetypes` (`LeaveTypeId`),
  KEY `IX_LeaveBalances_EmployeeId` (`EmployeeId`),
  CONSTRAINT `FK_LeaveBalances_tableemploye` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`),
  CONSTRAINT `FK_LeaveBalances_tableleavetypes` FOREIGN KEY (`LeaveTypeId`) REFERENCES `tableleavetypes` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableleavetypes`
--

DROP TABLE IF EXISTS `tableleavetypes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleavetypes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LeaveCode` varchar(20) NOT NULL,
  `LeaveName` varchar(100) NOT NULL,
  `DaysPerYear` int NOT NULL DEFAULT '0',
  `IsCarryForward` bit(1) NOT NULL DEFAULT b'0',
  `IsPaidLeave` bit(1) NOT NULL DEFAULT b'1',
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableletters`
--

DROP TABLE IF EXISTS `tableletters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableletters` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LetterNumber` varchar(50) NOT NULL,
  `LetterType` varchar(50) NOT NULL,
  `EmployeeId` int NOT NULL,
  `Subject` varchar(200) NOT NULL,
  `Body` text NOT NULL,
  `DocumentPath` varchar(500) DEFAULT NULL,
  `IssuedBy` varchar(100) DEFAULT NULL,
  `IssuedOn` datetime NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  PRIMARY KEY (`Id`),
  KEY `FK_Letters_Employee` (`EmployeeId`),
  CONSTRAINT `FK_Letters_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableproject`
--

DROP TABLE IF EXISTS `tableproject`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableproject` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProjectCode` varchar(20) NOT NULL,
  `ProjectName` varchar(150) NOT NULL,
  `ProjectType` varchar(50) NOT NULL,
  `ClientName` varchar(150) DEFAULT NULL,
  `BusinessObjective` text,
  `Scope` text,
  `StartDate` date DEFAULT NULL,
  `EndDate` date DEFAULT NULL,
  `Budget` decimal(18,2) NOT NULL DEFAULT '0.00',
  `Priority` varchar(20) NOT NULL DEFAULT 'Medium',
  `Status` varchar(30) NOT NULL DEFAULT 'Planning',
  `ProjectManagerId` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_TableProject_ProjectCode` (`ProjectCode`),
  KEY `IX_TableProject_ProjectManagerId` (`ProjectManagerId`),
  KEY `IX_TableProject_Status` (`Status`),
  KEY `IX_TableProject_IsActive` (`IsActive`),
  CONSTRAINT `FK_TableProject_ProjectManager` FOREIGN KEY (`ProjectManagerId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tablesalaryrecord`
--

DROP TABLE IF EXISTS `tablesalaryrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tablesalaryrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `Month` int NOT NULL,
  `Year` int NOT NULL,
  `ActualSalary` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PayDays` decimal(18,2) NOT NULL DEFAULT '0.00',
  `BasicSalary` decimal(18,2) NOT NULL,
  `HRA` decimal(18,2) NOT NULL,
  `Conveyance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `MedicalAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `EducationAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `SpecialAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `DA` decimal(18,2) NOT NULL,
  `OtherAllowances` decimal(18,2) NOT NULL,
  `GrossSalary` decimal(18,2) NOT NULL,
  `TotalEarnings` decimal(18,2) NOT NULL DEFAULT '0.00',
  `GrossMinusConveyance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PfDeduction` decimal(18,2) NOT NULL,
  `EsicDeduction` decimal(18,2) NOT NULL,
  `ProfessionalTax` decimal(18,2) NOT NULL DEFAULT '0.00',
  `Advance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `TotalDeductions` decimal(18,2) NOT NULL DEFAULT '0.00',
  `TdsDeduction` decimal(18,2) NOT NULL,
  `OtherDeductions` decimal(18,2) NOT NULL,
  `NetSalary` decimal(18,2) NOT NULL,
  `EmployerPf` decimal(18,2) NOT NULL DEFAULT '0.00',
  `EmployerEsic` decimal(18,2) NOT NULL DEFAULT '0.00',
  `CTC` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PaymentStatus` varchar(20) NOT NULL DEFAULT 'Pending',
  `PaidOn` datetime DEFAULT NULL,
  `PresentDays` int NOT NULL DEFAULT '0',
  `LeaveDays` int NOT NULL DEFAULT '0',
  `AbsentDays` int NOT NULL DEFAULT '0',
  `SourceFileName` varchar(255) DEFAULT NULL,
  `ImportedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Remark` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_SalaryRecord_Employee_Month_Year` (`EmployeeId`,`Month`,`Year`),
  CONSTRAINT `FK_SalaryRecord_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tableprojectmember`
--

DROP TABLE IF EXISTS `tableprojectmember`;

CREATE TABLE `tableprojectmember`
(
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProjectId` INT NOT NULL,
    `EmployeeId` INT NOT NULL,
    `RoleInProject` VARCHAR(100) DEFAULT NULL,
    `AssignedOn` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsActive` TINYINT(1) NOT NULL DEFAULT '1',

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
-- TABLE CREATION COMPLETED
-- ============================================================

SHOW TABLES;
