-- USE aryamanbms;

-- CREATE TABLE IF NOT EXISTS TableProject
-- (
--     Id INT NOT NULL AUTO_INCREMENT,

--     ProjectCode VARCHAR(20) NOT NULL,
--     ProjectName VARCHAR(150) NOT NULL,
--     ProjectType VARCHAR(50) NOT NULL,

--     ClientName VARCHAR(150) NULL,
--     BusinessObjective TEXT NULL,
--     Scope TEXT NULL,

--     StartDate DATE NULL,
--     EndDate DATE NULL,

--     Budget DECIMAL(18,2) NOT NULL DEFAULT 0.00,

--     Priority VARCHAR(20) NOT NULL DEFAULT 'Medium',
--     Status VARCHAR(30) NOT NULL DEFAULT 'Planning',

--     ProjectManagerId INT NOT NULL,

--     IsActive TINYINT(1) NOT NULL DEFAULT 1,

--     CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     UpdatedOn DATETIME NULL,

--     CONSTRAINT PK_TableProject
--         PRIMARY KEY (Id),

--     CONSTRAINT UQ_TableProject_ProjectCode
--         UNIQUE (ProjectCode),

--     CONSTRAINT FK_TableProject_ProjectManager
--         FOREIGN KEY (ProjectManagerId)
--         REFERENCES TableEmployee(Id)
--         ON DELETE RESTRICT
--         ON UPDATE CASCADE
-- );

-- CREATE INDEX IX_TableProject_ProjectManagerId
-- ON TableProject(ProjectManagerId);

-- CREATE INDEX IX_TableProject_Status
-- ON TableProject(Status);

-- CREATE INDEX IX_TableProject_IsActive
-- ON TableProject(IsActive);