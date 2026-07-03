-- GST input credit carry-forward balance
-- Run after tablegstmonthlysnapshot exists.

ALTER TABLE `tablegstmonthlysnapshot`
ADD COLUMN `InputCreditCarryForward` DECIMAL(18,2) NOT NULL DEFAULT 0.00;

-- Backfill existing snapshots so payable is never negative.
UPDATE `tablegstmonthlysnapshot`
SET
    `InputCreditCarryForward` =
        CASE
            WHEN (`TotalOutputGST` - `TotalInputGST`) < 0
            THEN ABS(`TotalOutputGST` - `TotalInputGST`)
            ELSE 0.00
        END,
    `NetGSTPayable` =
        CASE
            WHEN (`TotalOutputGST` - `TotalInputGST`) > 0
            THEN (`TotalOutputGST` - `TotalInputGST`)
            ELSE 0.00
        END;
