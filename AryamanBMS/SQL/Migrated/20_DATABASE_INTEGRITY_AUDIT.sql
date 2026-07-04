-- Group 20 - Accounts database integrity audit.
-- Run the duplicate-detection section first.
-- Create indexes only after duplicate queries return zero rows.

-- =========================================================
-- A. Existing Index Check
-- =========================================================

SHOW INDEX FROM `tableinvoicemaster`;
SHOW INDEX FROM `tablepaymentreceipt`;
SHOW INDEX FROM `tableproposal`;
SHOW INDEX FROM `tablepurchaseorder`;
SHOW INDEX FROM `tableexpensevouchers`;
SHOW INDEX FROM `tablefinancialsequence`;
SHOW INDEX FROM `tablegstmonthlysnapshot`;
SHOW INDEX FROM `tablegstreturn`;
SHOW INDEX FROM `tablegstchallan`;
SHOW INDEX FROM `tablepfmonthlysnapshot`;
SHOW INDEX FROM `tableesicmonthlysnapshot`;
SHOW INDEX FROM `tableptmonthlysnapshot`;
SHOW INDEX FROM `tableofficeasset`;
SHOW INDEX FROM `tableofficeassetassignmenthistory`;
SHOW INDEX FROM `TableEmployeeSalaryStructure`;

-- =========================================================
-- B. Duplicate Detection - Unique Business Keys
-- =========================================================

SELECT `InvoiceNo`, COUNT(*) AS `DuplicateCount`
FROM `tableinvoicemaster`
WHERE `InvoiceNo` IS NOT NULL AND TRIM(`InvoiceNo`) <> ''
GROUP BY `InvoiceNo`
HAVING COUNT(*) > 1;

SELECT `ReceiptNo`, COUNT(*) AS `DuplicateCount`
FROM `tablepaymentreceipt`
WHERE `ReceiptNo` IS NOT NULL AND TRIM(`ReceiptNo`) <> ''
GROUP BY `ReceiptNo`
HAVING COUNT(*) > 1;

SELECT `ProposalNumber`, COUNT(*) AS `DuplicateCount`
FROM `tableproposal`
WHERE `ProposalNumber` IS NOT NULL AND TRIM(`ProposalNumber`) <> ''
GROUP BY `ProposalNumber`
HAVING COUNT(*) > 1;

SELECT `OrderNumber`, COUNT(*) AS `DuplicateCount`
FROM `tablepurchaseorder`
WHERE `OrderNumber` IS NOT NULL AND TRIM(`OrderNumber`) <> ''
GROUP BY `OrderNumber`
HAVING COUNT(*) > 1;

SELECT `VoucherNumber`, COUNT(*) AS `DuplicateCount`
FROM `tableexpensevouchers`
WHERE `VoucherNumber` IS NOT NULL AND TRIM(`VoucherNumber`) <> ''
GROUP BY `VoucherNumber`
HAVING COUNT(*) > 1;

SELECT `DocumentType`, `FinancialYear`, COUNT(*) AS `DuplicateCount`
FROM `tablefinancialsequence`
GROUP BY `DocumentType`, `FinancialYear`
HAVING COUNT(*) > 1;

SELECT `Month`, `Year`, COUNT(*) AS `DuplicateCount`
FROM `tablegstmonthlysnapshot`
GROUP BY `Month`, `Year`
HAVING COUNT(*) > 1;

SELECT `SnapshotId`, `ReturnType`, COUNT(*) AS `DuplicateCount`
FROM `tablegstreturn`
WHERE `ReturnType` IS NOT NULL AND TRIM(`ReturnType`) <> ''
GROUP BY `SnapshotId`, `ReturnType`
HAVING COUNT(*) > 1;

SELECT `ChallanNumber`, COUNT(*) AS `DuplicateCount`
FROM `tablegstchallan`
WHERE `ChallanNumber` IS NOT NULL AND TRIM(`ChallanNumber`) <> ''
GROUP BY `ChallanNumber`
HAVING COUNT(*) > 1;

SELECT `Month`, `Year`, COUNT(*) AS `DuplicateCount`
FROM `tablepfmonthlysnapshot`
GROUP BY `Month`, `Year`
HAVING COUNT(*) > 1;

SELECT `Month`, `Year`, COUNT(*) AS `DuplicateCount`
FROM `tableesicmonthlysnapshot`
GROUP BY `Month`, `Year`
HAVING COUNT(*) > 1;

SELECT `Month`, `Year`, COUNT(*) AS `DuplicateCount`
FROM `tableptmonthlysnapshot`
GROUP BY `Month`, `Year`
HAVING COUNT(*) > 1;

SELECT `AssetCode`, COUNT(*) AS `DuplicateCount`
FROM `tableofficeasset`
WHERE `AssetCode` IS NOT NULL AND TRIM(`AssetCode`) <> ''
GROUP BY `AssetCode`
HAVING COUNT(*) > 1;

SELECT `OfficeAssetId`, COUNT(*) AS `ActiveAssignmentCount`
FROM `tableofficeassetassignmenthistory`
WHERE COALESCE(`IsActive`, b'1') = b'1'
GROUP BY `OfficeAssetId`
HAVING COUNT(*) > 1;

SELECT
    s1.`Id` AS `StructureId1`,
    s1.`EmployeeId`,
    s1.`EffectiveFrom` AS `From1`,
    s1.`EffectiveTo` AS `To1`,
    s2.`Id` AS `StructureId2`,
    s2.`EffectiveFrom` AS `From2`,
    s2.`EffectiveTo` AS `To2`
FROM `TableEmployeeSalaryStructure` s1
JOIN `TableEmployeeSalaryStructure` s2
  ON s1.`EmployeeId` = s2.`EmployeeId`
 AND s1.`Id` < s2.`Id`
 AND COALESCE(s1.`IsActive`, b'1') = b'1'
 AND COALESCE(s2.`IsActive`, b'1') = b'1'
 AND s1.`EffectiveFrom` <= COALESCE(s2.`EffectiveTo`, '9999-12-31')
 AND s2.`EffectiveFrom` <= COALESCE(s1.`EffectiveTo`, '9999-12-31');

-- =========================================================
-- C. Foreign Key Orphan Detection
-- =========================================================

SELECT p.`ProposalId`, p.`ClientId`
FROM `tableproposal` p
LEFT JOIN `tableclientmaster` c ON c.`ClientId` = p.`ClientId`
WHERE c.`ClientId` IS NULL;

SELECT p.`ProposalId`, p.`ProjectId`
FROM `tableproposal` p
LEFT JOIN `TableProject` pr ON pr.`Id` = p.`ProjectId`
WHERE p.`ProjectId` IS NOT NULL AND pr.`Id` IS NULL;

SELECT po.`PurchaseOrderId`, po.`ClientId`
FROM `tablepurchaseorder` po
LEFT JOIN `tableclientmaster` c ON c.`ClientId` = po.`ClientId`
WHERE c.`ClientId` IS NULL;

SELECT po.`PurchaseOrderId`, po.`ProposalId`
FROM `tablepurchaseorder` po
LEFT JOIN `tableproposal` p ON p.`ProposalId` = po.`ProposalId`
WHERE po.`ProposalId` IS NOT NULL AND p.`ProposalId` IS NULL;

SELECT i.`InvoiceId`, i.`ClientId`
FROM `tableinvoicemaster` i
LEFT JOIN `tableclientmaster` c ON c.`ClientId` = i.`ClientId`
WHERE c.`ClientId` IS NULL;

SELECT i.`InvoiceId`, i.`ProjectId`
FROM `tableinvoicemaster` i
LEFT JOIN `TableProject` p ON p.`Id` = i.`ProjectId`
WHERE i.`ProjectId` IS NOT NULL AND p.`Id` IS NULL;

SELECT r.`PaymentReceiptId`, r.`InvoiceId`
FROM `tablepaymentreceipt` r
LEFT JOIN `tableinvoicemaster` i ON i.`InvoiceId` = r.`InvoiceId`
WHERE i.`InvoiceId` IS NULL;

SELECT r.`PaymentReceiptId`, r.`ClientId`
FROM `tablepaymentreceipt` r
LEFT JOIN `tableclientmaster` c ON c.`ClientId` = r.`ClientId`
WHERE c.`ClientId` IS NULL;

SELECT ev.`ExpenseVoucherId`, ev.`ExpenseCategoryId`
FROM `tableexpensevouchers` ev
LEFT JOIN `tableexpensecategories` ec
  ON ec.`ExpenseCategoryId` = ev.`ExpenseCategoryId`
WHERE ec.`ExpenseCategoryId` IS NULL;

SELECT gr.`GstReturnId`, gr.`SnapshotId`
FROM `tablegstreturn` gr
LEFT JOIN `tablegstmonthlysnapshot` gs ON gs.`SnapshotId` = gr.`SnapshotId`
WHERE gs.`SnapshotId` IS NULL;

SELECT gc.`ChallanId`, gc.`SnapshotId`
FROM `tablegstchallan` gc
LEFT JOIN `tablegstmonthlysnapshot` gs ON gs.`SnapshotId` = gc.`SnapshotId`
WHERE gs.`SnapshotId` IS NULL;

SELECT pc.`PfChallanId`, pc.`PfSnapshotId`
FROM `tablepfchallan` pc
LEFT JOIN `tablepfmonthlysnapshot` ps ON ps.`PfSnapshotId` = pc.`PfSnapshotId`
WHERE ps.`PfSnapshotId` IS NULL;

SELECT ec.`EsicChallanId`, ec.`EsicSnapshotId`
FROM `tableesicchallan` ec
LEFT JOIN `tableesicmonthlysnapshot` es ON es.`EsicSnapshotId` = ec.`EsicSnapshotId`
WHERE es.`EsicSnapshotId` IS NULL;

SELECT ptc.`PtChallanId`, ptc.`PtSnapshotId`
FROM `tableptchallan` ptc
LEFT JOIN `tableptmonthlysnapshot` pts ON pts.`PtSnapshotId` = ptc.`PtSnapshotId`
WHERE pts.`PtSnapshotId` IS NULL;

SELECT ah.`OfficeAssetAssignmentHistoryId`, ah.`OfficeAssetId`
FROM `tableofficeassetassignmenthistory` ah
LEFT JOIN `tableofficeasset` a ON a.`OfficeAssetId` = ah.`OfficeAssetId`
WHERE a.`OfficeAssetId` IS NULL;

SELECT ah.`OfficeAssetAssignmentHistoryId`, ah.`EmployeeId`
FROM `tableofficeassetassignmenthistory` ah
LEFT JOIN `TableEmployee` e ON e.`Id` = ah.`EmployeeId`
WHERE e.`Id` IS NULL;

SELECT ss.`Id`, ss.`EmployeeId`
FROM `TableEmployeeSalaryStructure` ss
LEFT JOIN `TableEmployee` e ON e.`Id` = ss.`EmployeeId`
WHERE e.`Id` IS NULL;

-- =========================================================
-- D. Final Unique Indexes - apply only if duplicate checks pass
-- =========================================================

-- Already expected from 06/EF:
-- tableinvoicemaster.InvoiceNo
-- tablepaymentreceipt.ReceiptNo
-- tableproposal.ProposalNumber
-- tablepurchaseorder.OrderNumber
-- tableexpensevouchers.VoucherNumber
-- tablefinancialsequence(DocumentType, FinancialYear)
-- tablegstmonthlysnapshot(Month, Year)
-- tablepfmonthlysnapshot(Month, Year)
-- tableesicmonthlysnapshot(Month, Year)
-- tableptmonthlysnapshot(Month, Year)
-- tableofficeasset.AssetCode

-- Add if missing and duplicate check is clean:
-- CREATE UNIQUE INDEX `UX_tablegstreturn_Snapshot_ReturnType`
--     ON `tablegstreturn` (`SnapshotId`, `ReturnType`);

-- Add if missing and duplicate check is clean:
-- CREATE UNIQUE INDEX `UX_tablegstchallan_ChallanNumber`
--     ON `tablegstchallan` (`ChallanNumber`);

-- Add if missing and active-assignment duplicate check is clean.
-- MySQL has no filtered unique index, so this enforces only one active
-- and also only one inactive history row per asset if used directly.
-- Prefer enforcing one-active-assignment in repository logic unless you add
-- a generated column for active-only uniqueness.
-- CREATE UNIQUE INDEX `UX_tableofficeassetassignmenthistory_Asset_IsActive`
--     ON `tableofficeassetassignmenthistory` (`OfficeAssetId`, `IsActive`);

-- =========================================================
-- E. Useful Non-Unique Indexes - apply only if missing
-- =========================================================

-- CREATE INDEX `IX_tableproposal_Status`
--     ON `tableproposal` (`Status`);

-- CREATE INDEX `IX_tableproposal_IsActive`
--     ON `tableproposal` (`IsActive`);

-- CREATE INDEX `IX_tablepurchaseorder_Status`
--     ON `tablepurchaseorder` (`Status`);

-- CREATE INDEX `IX_tablepurchaseorder_IsActive`
--     ON `tablepurchaseorder` (`IsActive`);

-- CREATE INDEX `IX_tableinvoicemaster_Client_Date`
--     ON `tableinvoicemaster` (`ClientId`, `InvoiceDate`);

-- CREATE INDEX `IX_tablepaymentreceipt_Client_Date`
--     ON `tablepaymentreceipt` (`ClientId`, `ReceiptDate`);

-- CREATE INDEX `IX_tablepaymentreceipt_Invoice_Cancelled`
--     ON `tablepaymentreceipt` (`InvoiceId`, `IsCancelled`);

-- CREATE INDEX `IX_tableexpensevouchers_Status`
--     ON `tableexpensevouchers` (`Status`);

-- CREATE INDEX `IX_tableexpensevouchers_Category_Date`
--     ON `tableexpensevouchers` (`ExpenseCategoryId`, `VoucherDate`);

-- CREATE INDEX `IX_tablegstreturn_Snapshot_Status`
--     ON `tablegstreturn` (`SnapshotId`, `Status`);

-- CREATE INDEX `IX_tablegstchallan_Snapshot_Status`
--     ON `tablegstchallan` (`SnapshotId`, `Status`);

-- CREATE INDEX `IX_tablepfmonthlysnapshot_Status`
--     ON `tablepfmonthlysnapshot` (`Status`);

-- CREATE INDEX `IX_tableesicmonthlysnapshot_Status`
--     ON `tableesicmonthlysnapshot` (`Status`);

-- CREATE INDEX `IX_tableptmonthlysnapshot_Status`
--     ON `tableptmonthlysnapshot` (`Status`);

-- CREATE INDEX `IX_tableofficeasset_Status`
--     ON `tableofficeasset` (`Status`);
