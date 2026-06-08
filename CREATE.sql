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