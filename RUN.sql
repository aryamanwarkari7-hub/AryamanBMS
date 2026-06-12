-- SELECT * from tableemployee;
-- SELECT * from tabledesignation;
-- SELECT * from tabledepartment;
-- SELECT * from aspnetusers;

-- SELECT *
-- FROM TableAttendance;

-- DESCRIBE TableEmployee;

-- ALTER TABLE TableEmployee
-- DROP COLUMN Email;


-- SELECT * from tableattendance;

-- DESCRIBE TableSalaryRecord;

-- SELECT 
--     sr.Id,
--     e.EmployeeCode,
--     e.FirstName,
--     e.LastName,
--     sr.Month,
--     sr.Year,
--     sr.ActualSalary,
--     sr.PayDays,
--     sr.GrossSalary,
--     sr.BasicSalary,
--     sr.HRA,
--     sr.Conveyance,
--     sr.TotalEarnings,
--     sr.PfDeduction,
--     sr.EsicDeduction,
--     sr.ProfessionalTax,
--     sr.Advance,
--     sr.TotalDeductions,
--     sr.NetSalary,
--     sr.EmployerPf,
--     sr.EmployerEsic,
--     sr.CTC,
--     sr.PaymentStatus,
--     sr.SourceFileName,
--     sr.ImportedOn
-- FROM TableSalaryRecord sr
-- INNER JOIN TableEmployee e 
--     ON sr.EmployeeId = e.Id
-- WHERE Month=6
-- ORDER BY sr.Id DESC;

DELETE FROM TableSalaryRecord
WHERE Month = 5
AND Year = 2026;

-- SELECT * FROM tablesalaryrecord WHERE Month=5;