-- Manual Office Asset module changes
-- Run only statements for columns/tables that do not already exist.

ALTER TABLE tableofficeasset
ADD COLUMN SerialNumber VARCHAR(100) NULL,
ADD COLUMN ModelNumber VARCHAR(100) NULL,
ADD COLUMN Manufacturer VARCHAR(100) NULL,
ADD COLUMN Brand VARCHAR(100) NULL,
ADD COLUMN ConfigurationDetails VARCHAR(1000) NULL,
ADD COLUMN Barcode VARCHAR(100) NULL,
ADD COLUMN VendorId INT NULL,
ADD COLUMN ExpenseVoucherId INT NULL,
ADD COLUMN PurchaseOrderId INT NULL,
ADD COLUMN VendorInvoiceNumber VARCHAR(100) NULL,
ADD COLUMN VendorInvoiceDate DATETIME NULL,
ADD COLUMN LocationName VARCHAR(100) NULL,
ADD COLUMN Building VARCHAR(100) NULL,
ADD COLUMN Floor VARCHAR(100) NULL,
ADD COLUMN RoomOrSeat VARCHAR(100) NULL,
ADD COLUMN TaxableAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN CGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN SGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN IGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN TotalGSTAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN ITCEligible BIT NOT NULL DEFAULT 0,
ADD COLUMN ITCStatus VARCHAR(50) NOT NULL DEFAULT 'Not Applicable',
ADD COLUMN CapitalizedValue DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN IsCapitalized BIT NOT NULL DEFAULT 0,
ADD COLUMN CapitalizedOn DATETIME NULL,
ADD COLUMN CapitalizedByUserId VARCHAR(450) NULL,
ADD COLUMN HasAmc BIT NOT NULL DEFAULT 0,
ADD COLUMN AmcVendorName VARCHAR(150) NULL,
ADD COLUMN AmcStartDate DATETIME NULL,
ADD COLUMN AmcEndDate DATETIME NULL,
ADD COLUMN DepreciationRate DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN AccumulatedDepreciation DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN WrittenDownValue DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN DisposalValue DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD COLUMN DisposalReason VARCHAR(500) NULL,
ADD COLUMN LostOrDamagedOn DATETIME NULL,
ADD COLUMN LostOrDamagedReason VARCHAR(500) NULL,
ADD COLUMN LastVerifiedByUserId VARCHAR(450) NULL,
ADD COLUMN LastVerifiedOn DATETIME NULL,
ADD COLUMN LastVerificationStatus VARCHAR(50) NULL,
ADD COLUMN ArchivedByUserId VARCHAR(450) NULL,
ADD COLUMN ArchivedOn DATETIME NULL,
ADD COLUMN ArchiveReason VARCHAR(500) NULL;

UPDATE tableofficeasset
SET TaxableAmount = PurchaseValue,
    CapitalizedValue = PurchaseValue,
    WrittenDownValue = PurchaseValue
WHERE OfficeAssetId > 0
  AND TaxableAmount = 0;

CREATE TABLE IF NOT EXISTS tableofficeassetdocument (
    OfficeAssetDocumentId INT NOT NULL AUTO_INCREMENT,
    OfficeAssetId INT NOT NULL,
    DocumentType VARCHAR(50) NOT NULL,
    OriginalFileName VARCHAR(255) NOT NULL,
    StoredFilePath VARCHAR(500) NOT NULL,
    UploadedByUserId VARCHAR(450) NOT NULL,
    UploadedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Remarks VARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (OfficeAssetDocumentId),
    KEY IX_tableofficeassetdocument_OfficeAssetId (OfficeAssetId)
);

CREATE TABLE IF NOT EXISTS tableofficeassetmaintenance (
    OfficeAssetMaintenanceId INT NOT NULL AUTO_INCREMENT,
    OfficeAssetId INT NOT NULL,
    MaintenanceDate DATETIME NOT NULL,
    MaintenanceType VARCHAR(50) NOT NULL,
    ServiceVendorName VARCHAR(150) NULL,
    Cost DECIMAL(18,2) NOT NULL DEFAULT 0,
    IssueDescription VARCHAR(500) NULL,
    Resolution VARCHAR(500) NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'Completed',
    CreatedByUserId VARCHAR(450) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (OfficeAssetMaintenanceId),
    KEY IX_tableofficeassetmaintenance_OfficeAssetId (OfficeAssetId)
);

CREATE TABLE IF NOT EXISTS tableofficeassetverification (
    OfficeAssetVerificationId INT NOT NULL AUTO_INCREMENT,
    OfficeAssetId INT NOT NULL,
    VerificationDate DATETIME NOT NULL,
    VerificationStatus VARCHAR(50) NOT NULL,
    VerifiedLocation VARCHAR(100) NULL,
    VerifiedByUserId VARCHAR(450) NOT NULL,
    Remarks VARCHAR(500) NULL,
    CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (OfficeAssetVerificationId),
    KEY IX_tableofficeassetverification_OfficeAssetId (OfficeAssetId)
);

CREATE INDEX IX_tableofficeasset_VendorId ON tableofficeasset (VendorId);
CREATE INDEX IX_tableofficeasset_ExpenseVoucherId ON tableofficeasset (ExpenseVoucherId);
CREATE INDEX IX_tableofficeasset_Status ON tableofficeasset (Status);
CREATE INDEX IX_tableofficeasset_LocationName ON tableofficeasset (LocationName);
