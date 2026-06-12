-- CREATE DATABASE aryamanbms;

-- DEPARTMENT --
-- CREATE TABLE TableDepartment
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     DepartmentCode VARCHAR(20) NOT NULL,
--     DepartmentName VARCHAR(100) NOT NULL,

--     IsActive BIT NOT NULL DEFAULT 1,

--     CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     UpdatedOn DATETIME NULL
-- );

-- DESIGNATION --
-- CREATE TABLE TableDesignation
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     DesignationCode VARCHAR(20) NOT NULL,
--     DesignationName VARCHAR(100) NOT NULL,

--     DepartmentId INT NOT NULL,

--     IsActive BIT NOT NULL DEFAULT 1,

--     CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     UpdatedOn DATETIME NULL,

--     CONSTRAINT FK_Designation_Department
--         FOREIGN KEY (DepartmentId)
--         REFERENCES TableDepartment(Id)
-- );

-- EMPLOYEE --
-- CREATE TABLE TableEmployee
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     EmployeeCode VARCHAR(20) NOT NULL,

--     FirstName VARCHAR(100) NOT NULL,
--     LastName VARCHAR(100) NULL,

--     Email VARCHAR(150) NULL,
--     MobileNumber VARCHAR(20) NULL,

--     JoiningDate DATE NOT NULL,

--     DepartmentId INT NOT NULL,
--     DesignationId INT NOT NULL,

--     IsActive BIT NOT NULL DEFAULT 1,

--     CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     UpdatedOn DATETIME NULL,

--     CONSTRAINT FK_Employee_Department
--         FOREIGN KEY (DepartmentId)
--         REFERENCES TableDepartment(Id),

--     CONSTRAINT FK_Employee_Designation
--         FOREIGN KEY (DesignationId)
--         REFERENCES TableDesignation(Id)
-- );

-- ALTER EMPLOYEE --
-- ALTER TABLE TableEmployee
-- ADD CONSTRAINT UQ_EmployeeCode
-- UNIQUE (EmployeeCode);

-- ALTER EMPLOYEE --
-- ALTER TABLE TableEmployee
-- ADD CONSTRAINT FK_Employee_AspNetUsers
-- FOREIGN KEY (ApplicationUserId)
-- REFERENCES AspNetUsers(Id)
-- ON DELETE SET NULL;

-- CREATE INDEX IX_TableEmployee_ApplicationUserId
-- ON TableEmployee(ApplicationUserId);

-- CREATE INDEX IX_TableEmployee_AadhaarNo
-- ON TableEmployee(AadhaarNo);

-- CREATE INDEX IX_TableEmployee_PanNo
-- ON TableEmployee(PanNo);

-- CREATE INDEX IX_TableEmployee_UanNo
-- ON TableEmployee(UanNo);

-- ALTER EMPLOYEE --
-- ALTER TABLE TableEmployee
-- ADD COLUMN DateOfBirth DATE NULL,
-- ADD COLUMN Gender VARCHAR(20) NULL,
-- ADD COLUMN PersonalEmail VARCHAR(150) NULL,
-- ADD COLUMN OfficialEmail VARCHAR(150) NULL,
-- ADD COLUMN PermanentAddress TEXT NULL,
-- ADD COLUMN City VARCHAR(100) NULL,
-- ADD COLUMN State VARCHAR(100) NULL,
-- ADD COLUMN PinCode VARCHAR(20) NULL,
-- ADD COLUMN EmergencyContact VARCHAR(150) NULL,
-- ADD COLUMN EmergencyPhone VARCHAR(20) NULL,
-- ADD COLUMN AadhaarNo VARCHAR(20) NULL,
-- ADD COLUMN PanNo VARCHAR(20) NULL,
-- ADD COLUMN UanNo VARCHAR(30) NULL,
-- ADD COLUMN EmploymentType VARCHAR(50) NULL;


-- ATTENDANCE --
-- CREATE TABLE TableAttendance
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     EmployeeId INT NOT NULL,

--     AttendanceDate DATE NOT NULL,

--     CheckInTime DATETIME NULL,

--     CheckOutTime DATETIME NULL,

--     Remarks VARCHAR(500) NULL,

--     CreatedOn DATETIME NOT NULL,

--     CONSTRAINT FK_Attendance_Employee
--         FOREIGN KEY (EmployeeId)
--         REFERENCES TableEmployee(Id)
-- );


-- ALTER ATTENDANCE --
-- ALTER TABLE TableAttendance
-- ADD COLUMN LocationType VARCHAR(50) NULL;



-- ALTER TABLE TableAttendance
-- ADD COLUMN Status VARCHAR(10) NOT NULL DEFAULT 'P';


-- LEAVE TYPES --

-- CREATE TABLE tableleavetypes
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     LeaveCode VARCHAR(20) NOT NULL,
--     LeaveName VARCHAR(100) NOT NULL,

--     DaysPerYear INT NULL,

--     IsCarryForward BIT NOT NULL DEFAULT b'0',
--     IsPaidLeave BIT NOT NULL DEFAULT b'1',
--     IsActive BIT NOT NULL DEFAULT b'1'
-- );

-- ALTER TABLE tableleavetypes
-- MODIFY COLUMN DaysPerYear INT NOT NULL DEFAULT 0;

-- UPDATE TableLeaveTypes
-- SET IsPaidLeave = 0
-- WHERE Id=4;

-- INSERT INTO TableLeaveTypes
-- (
--     LeaveCode,
--     LeaveName,
--     DaysPerYear,
--     IsCarryForward,
--     IsPaidLeave,
--     IsActive
-- )
-- SELECT
--     'UPL',
--     'Unplanned Leave',
--     0,
--     0,
--     1,
--     1
-- WHERE NOT EXISTS
-- (
--     SELECT 1
--     FROM TableLeaveTypes
--     WHERE LeaveCode = 'UPL'
-- );

-- INSERT INTO TableLeaveTypes
-- (
--     LeaveCode,
--     LeaveName,
--     DaysPeryear,
--     IsCarryForward,
--     IsPaidLeave,
--     IsActive
-- )
-- SELECT
--     'BDL',
--     'Birthday Leave',
--     1,
--     0,
--     1,
--     1
-- WHERE NOT EXISTS
-- (
--     SELECT 1
--     FROM TableLeaveTypes
--     WHERE LeaveCode = 'BDL'
-- );

-- SELECT * from tableleavetypes;

-- LEAVE APPLICATIONS --

-- CREATE TABLE tableleaveapplications
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     ApplicationNumber VARCHAR(50) NOT NULL,

--     EmployeeId INT NOT NULL,
--     LeaveTypeId INT NOT NULL,

--     FromDate DATE NOT NULL,
--     ToDate DATE NOT NULL,

--     NumberOfDays DECIMAL(10,2) NOT NULL,

--     Reason VARCHAR(1000) NULL,

--     AppliedOn DATETIME NOT NULL,

--     Status VARCHAR(20) NOT NULL,

--     ApprovedBy VARCHAR(100) NULL,
--     ApprovedOn DATETIME NULL,

--     ApprovalRemarks VARCHAR(1000) NULL,

--     CONSTRAINT FK_LeaveApplications_tableemployee
--         FOREIGN KEY(EmployeeId)
--         REFERENCES tableemployee(Id),

--     CONSTRAINT FK_LeaveApplications_tableleaveType
--         FOREIGN KEY(LeaveTypeId)
--         REFERENCES tableleavetypes(Id)
-- );

-- LEAVE BALANCES --

-- CREATE TABLE tableleavebalances
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     EmployeeId INT NOT NULL,
--     LeaveTypeId INT NOT NULL,

--     LeaveYear INT NOT NULL,

--     AllocatedDays DECIMAL(10,2) NOT NULL DEFAULT 0,
--     UsedDays DECIMAL(10,2) NOT NULL DEFAULT 0,
--     BalanceDays DECIMAL(10,2) NOT NULL DEFAULT 0,

--     CONSTRAINT FK_LeaveBalances_tableemploye
--         FOREIGN KEY(EmployeeId)
--         REFERENCES tableemployee(Id),

--     CONSTRAINT FK_LeaveBalances_tableleavetypes
--         FOREIGN KEY(LeaveTypeId)
--         REFERENCES tableleavetypes(Id),

--     UNIQUE KEY UK_tableleavebalances
--     (
--         EmployeeId,
--         LeaveTypeId,
--         LeaveYear
--     )
-- );

-- LEAVE INDEXES --
-- CREATE INDEX IX_LeaveApplications_EmployeeId
-- ON tableleaveapplications(EmployeeId);

-- CREATE INDEX IX_LeaveApplications_Status
-- ON tableleaveapplications(Status);

-- CREATE INDEX IX_LeaveApplications_FromDate
-- ON tableleaveapplications(FromDate);

-- CREATE INDEX IX_LeaveBalances_EmployeeId
-- ON tableleavebalances(EmployeeId);

-- SALARY --
-- CREATE TABLE TableSalaryRecord
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     EmployeeId INT NOT NULL,

--     Month INT NOT NULL,

--     Year INT NOT NULL,

--     BasicSalary DECIMAL(18,2) NOT NULL,

--     HRA DECIMAL(18,2) NOT NULL,

--     DA DECIMAL(18,2) NOT NULL,

--     OtherAllowances DECIMAL(18,2) NOT NULL,

--     GrossSalary DECIMAL(18,2) NOT NULL,

--     PfDeduction DECIMAL(18,2) NOT NULL,

--     EsicDeduction DECIMAL(18,2) NOT NULL,

--     TdsDeduction DECIMAL(18,2) NOT NULL,

--     OtherDeductions DECIMAL(18,2) NOT NULL,

--     NetSalary DECIMAL(18,2) NOT NULL,

--     PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Pending',

--     PaidOn DATETIME NULL,

--     CONSTRAINT FK_SalaryRecord_Employee
--     FOREIGN KEY(EmployeeId)
--     REFERENCES TableEmployee(Id)
-- );

-- ALTER TABLE TableSalaryRecord
-- ADD COLUMN ActualSalary DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER Year,
-- ADD COLUMN PayDays DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER ActualSalary,

-- ADD COLUMN Conveyance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER HRA,
-- ADD COLUMN MedicalAllowance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER Conveyance,
-- ADD COLUMN EducationAllowance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER MedicalAllowance,
-- ADD COLUMN SpecialAllowance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER EducationAllowance,

-- ADD COLUMN TotalEarnings DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER GrossSalary,
-- ADD COLUMN GrossMinusConveyance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER TotalEarnings,

-- ADD COLUMN ProfessionalTax DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER EsicDeduction,
-- ADD COLUMN Advance DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER ProfessionalTax,
-- ADD COLUMN TotalDeductions DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER Advance,

-- ADD COLUMN EmployerPf DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER NetSalary,
-- ADD COLUMN EmployerEsic DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER EmployerPf,
-- ADD COLUMN CTC DECIMAL(18,2) NOT NULL DEFAULT 0 AFTER EmployerEsic,

-- ADD COLUMN SourceFileName VARCHAR(255) NULL AFTER AbsentDays,
-- ADD COLUMN ImportedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP AFTER SourceFileName,
-- ADD COLUMN Remark VARCHAR(500) NULL AFTER ImportedOn;

-- ALTER TABLE TableSalaryRecord
-- ADD CONSTRAINT UQ_SalaryRecord_Employee_Month_Year
-- UNIQUE (EmployeeId, Month, Year);

-- LETTER Table --

-- CREATE TABLE TableLetters
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     LetterNumber VARCHAR(50) NOT NULL,

--     LetterType VARCHAR(50) NOT NULL,

--     EmployeeId INT NOT NULL,

--     Subject VARCHAR(200) NOT NULL,

--     Body TEXT NOT NULL,

--     DocumentPath VARCHAR(500),

--     IssuedBy VARCHAR(100),

--     IssuedOn DATETIME NOT NULL,

--     IsActive BIT NOT NULL DEFAULT 1,

--     CONSTRAINT FK_Letters_Employee
--     FOREIGN KEY(EmployeeId)
--     REFERENCES TableEmployee(Id)
-- );
