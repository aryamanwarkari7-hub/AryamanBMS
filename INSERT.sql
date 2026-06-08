-- INSERT INTO TableDepartment
-- (
--     DepartmentName,
--     DisplayCode,
--     IsActive
-- )
-- VALUES
-- ('Human Resources', 'HR', 1),
-- ('Information Technology', 'IT', 1),
-- ('Finance', 'FIN', 1),
-- ('Sales', 'SAL', 1),
-- ('Administration', 'ADM', 1),
-- ('Operations', 'OPS', 1);

INSERT INTO TableDesignation
(
    DesignationName,
    DisplayCode,
    DepartmentId,
    IsActive
)
VALUES

-- HR
('HR Executive', 'HRE', 1, 1),
('HR Manager', 'HRM', 1, 1),

-- IT
('Software Developer', 'SD', 2, 1),
('Senior Software Engineer', 'SSE', 2, 1),
('Team Lead', 'TL', 2, 1),
('Project Manager', 'PM', 2, 1),
('Business Analyst', 'BA', 2, 1),
('QA Engineer', 'QA', 2, 1),
('System Administrator', 'SA', 2, 1),

-- Finance
('Accountant', 'ACC', 3, 1),
('Finance Manager', 'FM', 3, 1),

-- Sales
('Sales Executive', 'SE', 4, 1),
('Sales Manager', 'SM', 4, 1),

-- Administration
('Administrator', 'ADM', 5, 1),
('Office Manager', 'OM', 5, 1),

-- Operations
('Operations Executive', 'OE', 6, 1),
('Operations Manager', 'OPM', 6, 1);

