-- Accounts & Finance master data only
-- Do not seed old clients, proposals, invoices, receipts, vouchers, statutory snapshots, users, employees, projects, attendance, salary, or leave data.

INSERT INTO `tablecompanyprofile`
(
    `CompanyName`,
    `GSTIN`,
    `PAN`,
    `Address`,
    `Email`,
    `Phone`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`
)
SELECT
    'Aryaman Technologies Private Limited',
    'ARYCA04561',
    'ARYMA0100',
    'Karve Nagar Pune',
    'Aryaman@gmail.com',
    '7893691587',
    b'1',
    NOW(),
    NULL
WHERE NOT EXISTS
(
    SELECT 1
    FROM `tablecompanyprofile`
    WHERE `GSTIN` = 'ARYCA04561'
       OR `CompanyName` = 'Aryaman Technologies Private Limited'
);

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'GST', 'GST', 'GST Registration & Certificates', 1, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'GST');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'PAN', 'PAN', 'PAN Card & PAN Related Documents', 2, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'PAN');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'MSME', 'MSME', 'MSME Registration Documents', 3, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'MSME');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'COMPANY', 'Company Registration', 'Incorporation, ROC, and company registration documents', 4, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Company Registration');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'BANK', 'Bank', 'Bank account, cancelled cheque, and bank documents', 5, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Bank');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'PF', 'PF', 'Provident Fund registration and related documents', 6, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'PF');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'ESIC', 'ESIC', 'ESIC registration and related documents', 7, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'ESIC');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'PT', 'Professional Tax', 'Professional Tax registration and related documents', 8, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Professional Tax');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'INSURANCE', 'Insurance', 'Company insurance policies and renewals', 9, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Insurance');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'AUDIT', 'Audit', 'Audit reports and financial compliance documents', 10, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Audit');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'LEGAL', 'Legal', 'Legal agreements and notices', 11, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Legal');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'ISO', 'ISO', 'ISO certificates and quality documents', 12, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'ISO');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'RENTAL', 'Rental', 'Office rental and lease documents', 13, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Rental');

INSERT INTO `tablecompanydocumentcategory`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DisplayOrder`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`,
    `HasExpiry`,
    `RequireDocumentNumber`,
    `IsMandatory`,
    `ExpiryReminderDays`,
    `AllowMultipleDocuments`,
    `AllowedExtensions`,
    `MaxFileSizeMB`
)
SELECT 'OTHER', 'Other', 'Other company documents', 99, b'1', NOW(), NULL, b'1', b'0', b'0', 30, b'0', NULL, NULL
WHERE NOT EXISTS (SELECT 1 FROM `tablecompanydocumentcategory` WHERE `CategoryName` = 'Other');

INSERT INTO `tableexpensecategories`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DefaultGSTRate`,
    `ITCEligible`,
    `GLAccountCode`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`
)
SELECT 'TRAVEL', 'Travel Expense', 'Travel, food, lodging, and site visit expenses', 0.00, b'1', '6100', b'1', NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM `tableexpensecategories` WHERE `CategoryCode` = 'TRAVEL');

INSERT INTO `tableexpensecategories`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DefaultGSTRate`,
    `ITCEligible`,
    `GLAccountCode`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`
)
SELECT 'OFFICE', 'Office Expense', 'Office supplies and operational expenses', 0.00, b'1', '6100', b'1', NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM `tableexpensecategories` WHERE `CategoryCode` = 'OFFICE');

INSERT INTO `tableexpensecategories`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DefaultGSTRate`,
    `ITCEligible`,
    `GLAccountCode`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`
)
SELECT 'SOFTWARE', 'Software Subscription', 'Software tools, licenses, and subscriptions', 18.00, b'1', '6100', b'1', NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM `tableexpensecategories` WHERE `CategoryCode` = 'SOFTWARE');

INSERT INTO `tableexpensecategories`
(
    `CategoryCode`,
    `CategoryName`,
    `Description`,
    `DefaultGSTRate`,
    `ITCEligible`,
    `GLAccountCode`,
    `IsActive`,
    `CreatedOn`,
    `UpdatedOn`
)
SELECT 'PROFESSIONAL', 'Professional Fees', 'Consulting, legal, audit, and professional service expenses', 18.00, b'1', '6100', b'1', NOW(), NULL
WHERE NOT EXISTS (SELECT 1 FROM `tableexpensecategories` WHERE `CategoryCode` = 'PROFESSIONAL');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'Proposal', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'Proposal' AND `FinancialYear` = '2026-27');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'PO', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'PO' AND `FinancialYear` = '2026-27');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'WO', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'WO' AND `FinancialYear` = '2026-27');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'Invoice', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'Invoice' AND `FinancialYear` = '2026-27');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'Receipt', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'Receipt' AND `FinancialYear` = '2026-27');

INSERT INTO `tablefinancialsequence`
(
    `DocumentType`,
    `FinancialYear`,
    `LastNumber`,
    `UpdatedOn`
)
SELECT 'ExpenseVoucher', '2026-27', 0, NOW()
WHERE NOT EXISTS (SELECT 1 FROM `tablefinancialsequence` WHERE `DocumentType` = 'ExpenseVoucher' AND `FinancialYear` = '2026-27');
