-- Manual Expenses module changes
-- Run only the statements for columns/tables that do not already exist.

CREATE TABLE IF NOT EXISTS tablevendor (
    VendorId INT NOT NULL AUTO_INCREMENT,
    VendorCode VARCHAR(30) NOT NULL,
    VendorName VARCHAR(150) NOT NULL,
    GSTIN VARCHAR(15) NULL,
    PAN VARCHAR(10) NULL,
    State VARCHAR(100) NULL,
    StateCode VARCHAR(2) NULL,
    Address VARCHAR(500) NULL,
    RegistrationType VARCHAR(50) NULL,
    PaymentTerms VARCHAR(100) NULL,
    BankDetails VARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedByUserId VARCHAR(450) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedOn DATETIME NULL,
    PRIMARY KEY (VendorId),
    UNIQUE KEY UX_tablevendor_VendorCode (VendorCode),
    KEY IX_tablevendor_GSTIN (GSTIN),
    KEY IX_tablevendor_IsActive (IsActive)
);

ALTER TABLE tableexpensecategories
ADD COLUMN ExpenseType VARCHAR(50) NOT NULL DEFAULT 'General',
ADD COLUMN PayableGLAccountCode VARCHAR(50) NULL,
ADD COLUMN InputGSTGLAccountCode VARCHAR(50) NULL,
ADD COLUMN IsCapitalExpense BIT NOT NULL DEFAULT 0;

ALTER TABLE tableexpensevouchers
ADD COLUMN VendorId INT NULL,
ADD COLUMN ProjectId INT NULL,
ADD COLUMN DepartmentId INT NULL,
ADD COLUMN CostCentreId INT NULL,
ADD COLUMN ExpenseClassification VARCHAR(30) NOT NULL DEFAULT 'General',
ADD COLUMN TaxableAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
ADD COLUMN VendorInvoiceDate DATETIME NULL,
ADD COLUMN ApprovalStatus VARCHAR(20) NOT NULL DEFAULT 'Draft',
ADD COLUMN PaymentStatus VARCHAR(30) NOT NULL DEFAULT 'Unpaid',
ADD COLUMN PaidAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
ADD COLUMN BalanceAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
ADD COLUMN ITCStatus VARCHAR(50) NOT NULL DEFAULT 'Pending Verification',
ADD COLUMN Gstr2BMatchStatus VARCHAR(50) NOT NULL DEFAULT 'Pending',
ADD COLUMN Gstr2BMatchedOn DATETIME NULL,
ADD COLUMN Gstr2BMatchedByUserId VARCHAR(450) NULL,
ADD COLUMN Gstr2BMismatchReason VARCHAR(500) NULL,
ADD COLUMN ITCClaimMonth INT NULL,
ADD COLUMN ITCClaimYear INT NULL,
ADD COLUMN CompanyStateCode VARCHAR(2) NULL,
ADD COLUMN VendorStateCode VARCHAR(2) NULL,
ADD COLUMN PlaceOfSupplyStateCode VARCHAR(2) NULL,
ADD COLUMN IsGstStateOverride BIT NOT NULL DEFAULT 0,
ADD COLUMN GstStateOverrideReason VARCHAR(500) NULL,
ADD COLUMN IsEmployeeReimbursement BIT NOT NULL DEFAULT 0,
ADD COLUMN ReimbursementEmployeeId INT NULL,
ADD COLUMN ReimbursementStatus VARCHAR(30) NOT NULL DEFAULT 'Not Applicable',
ADD COLUMN BusinessPurpose VARCHAR(500) NULL,
ADD COLUMN BeneficiaryName VARCHAR(150) NULL,
ADD COLUMN SupportingReference VARCHAR(100) NULL,
ADD COLUMN GLAccountCode VARCHAR(50) NULL,
ADD COLUMN PayableGLAccountCode VARCHAR(50) NULL,
ADD COLUMN InputGSTGLAccountCode VARCHAR(50) NULL,
ADD COLUMN AccountingPeriod VARCHAR(50) NULL,
ADD COLUMN PostingReference VARCHAR(100) NULL,
ADD COLUMN JournalEntryId INT NULL,
ADD COLUMN SubmittedByUserId VARCHAR(450) NULL,
ADD COLUMN SubmittedOn DATETIME NULL,
ADD COLUMN PostedByUserId VARCHAR(450) NULL,
ADD COLUMN PostedOn DATETIME NULL,
ADD COLUMN ReopenedByUserId VARCHAR(450) NULL,
ADD COLUMN ReopenedOn DATETIME NULL,
ADD COLUMN ReopenReason VARCHAR(500) NULL,
ADD COLUMN IsReversed BIT NOT NULL DEFAULT 0,
ADD COLUMN ReversedByUserId VARCHAR(450) NULL,
ADD COLUMN ReversedOn DATETIME NULL,
ADD COLUMN ReversalReason VARCHAR(500) NULL;

UPDATE tableexpensevouchers
SET TaxableAmount = Amount,
    BalanceAmount = TotalAmount,
    ApprovalStatus = COALESCE(Status, 'Draft')
WHERE ExpenseVoucherId > 0
  AND TaxableAmount = 0;

CREATE INDEX IX_tableexpensevouchers_VendorInvoiceFy
ON tableexpensevouchers (VendorId, InvoiceNumber, FinancialYear);

CREATE TABLE IF NOT EXISTS tablevendorpayment (
    VendorPaymentId INT NOT NULL AUTO_INCREMENT,
    PaymentNo VARCHAR(50) NOT NULL,
    VendorId INT NOT NULL,
    ExpenseVoucherId INT NOT NULL,
    PaymentDate DATETIME NOT NULL,
    AmountPaid DECIMAL(12,2) NOT NULL,
    PaymentMode VARCHAR(50) NOT NULL,
    BankAccountId INT NULL,
    TransactionReference VARCHAR(100) NULL,
    PaidByUserId VARCHAR(450) NOT NULL,
    Remarks VARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (VendorPaymentId),
    UNIQUE KEY UX_tablevendorpayment_PaymentNo (PaymentNo),
    KEY IX_tablevendorpayment_TransactionReference (TransactionReference),
    KEY IX_tablevendorpayment_VendorId (VendorId),
    KEY IX_tablevendorpayment_ExpenseVoucherId (ExpenseVoucherId)
);
