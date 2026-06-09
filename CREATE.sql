-- CREATE DATABASE aryamanbms;

-- CREATE TABLE TableDepartment
-- (
--     Id INT PRIMARY KEY AUTO_INCREMENT,

--     DepartmentCode VARCHAR(20) NOT NULL,
--     DepartmentName VARCHAR(100) NOT NULL,

--     IsActive BIT NOT NULL DEFAULT 1,

--     CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     UpdatedOn DATETIME NULL
-- );

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

-- ALTER TABLE TableEmployee
-- ADD CONSTRAINT UQ_EmployeeCode
-- UNIQUE (EmployeeCode);

-- ALTER TABLE TableEmployee
-- ADD CONSTRAINT FK_Employee_AspNetUsers
-- FOREIGN KEY (ApplicationUserId)
-- REFERENCES AspNetUsers(Id)
-- ON DELETE SET NULL;

-- CREATE INDEX IX_TableEmployee_ApplicationUserId
-- ON TableEmployee(ApplicationUserId);

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

-- ALTER TABLE TableAttendance
-- ADD COLUMN LocationType VARCHAR(50) NULL;

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

-- CREATE INDEX IX_TableEmployee_AadhaarNo
-- ON TableEmployee(AadhaarNo);

-- CREATE INDEX IX_TableEmployee_PanNo
-- ON TableEmployee(PanNo);

-- CREATE INDEX IX_TableEmployee_UanNo
-- ON TableEmployee(UanNo);

ALTER TABLE TableAttendance
ADD COLUMN Status VARCHAR(10) NOT NULL DEFAULT 'P';