SELECT
    `Id`,
    `EmployeeId`,
    `EffectiveFrom`,
    `EffectiveTo`,
    `ActualSalary`,
    `IsActive`
FROM `TableEmployeeSalaryStructure`
ORDER BY `EmployeeId`, `EffectiveFrom`;