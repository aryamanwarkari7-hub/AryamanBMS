ALTER TABLE tableleaveapplications
    MODIFY COLUMN NumberOfDays DECIMAL(4,2) NOT NULL DEFAULT 1.00,
    ADD COLUMN IsHalfDay TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN HalfDaySession VARCHAR(20) NULL;

ALTER TABLE TableAttendance
    ADD COLUMN AttendanceValue DECIMAL(4,2) NOT NULL DEFAULT 1.00;

UPDATE tableleaveapplications
SET NumberOfDays = 1.00
WHERE NumberOfDays IS NULL OR NumberOfDays <= 0;

UPDATE TableAttendance
SET AttendanceValue = 1.00
WHERE AttendanceValue IS NULL OR AttendanceValue <= 0;
