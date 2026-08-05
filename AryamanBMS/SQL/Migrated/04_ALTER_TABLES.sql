-- ADD MIDDLE NAME--
-- ALTER TABLE `tableemployee`
-- ADD COLUMN `MiddleName` VARCHAR(100) NULL
-- AFTER `FirstName`;

-- ESIC NO. ADD --
-- ALTER TABLE `TableEmployee`
-- ADD COLUMN `EsicNo` VARCHAR(10) NULL
-- AFTER `UanNo`;

-- CREATE INDEX `IX_TableEmployee_EsicNo`
-- ON `TableEmployee` (`EsicNo`);


-- LOCAL ADDRESS ADD --
-- ALTER TABLE `TableEmployee`
-- ADD COLUMN `LocalAddress` TEXT NULL
--     AFTER `PermanentAddress`,

-- ADD COLUMN `LocalCity` VARCHAR(100) NULL
--     AFTER `LocalAddress`,

-- ADD COLUMN `LocalState` VARCHAR(100) NULL
--     AFTER `LocalCity`,

-- ADD COLUMN `LocalPinCode` VARCHAR(6) NULL
--     AFTER `LocalState`;

-- DOC CATEGORY ADD --
-- ALTER TABLE `TableEmployeeDocument`
-- ADD COLUMN `DocumentCategory` VARCHAR(30) NULL
-- AFTER `EmployeeAcademicId`;

-- EXISITING ALL ACADEMIC
-- UPDATE `TableEmployeeDocument`
-- SET `DocumentCategory` = 'Academic'
-- WHERE `DocumentCategory` IS NULL;

-- CATEGORY COMPULSORY --
-- ALTER TABLE `TableEmployeeDocument`
-- MODIFY COLUMN `DocumentCategory` VARCHAR(30) NOT NULL;

-- CREATE INDEX `IX_TableEmployeeDocument_Employee_Category`
-- ON `TableEmployeeDocument`
-- (
--     `EmployeeId`,
--     `DocumentCategory`
-- );

-- ALTER TABLE `TableEmployeeDocument`
-- ADD COLUMN `EmployeePreviousEmploymentId` INT NULL
-- AFTER `EmployeeAcademicId`;

-- CREATE INDEX `IX_EmployeeDocument_PreviousEmploymentId`
-- ON `TableEmployeeDocument`
-- (`EmployeePreviousEmploymentId`);

-- ADD PREVIOUS EMP. DOC --
-- ALTER TABLE `TableEmployeeDocument`
-- ADD CONSTRAINT `FK_EmployeeDocument_PreviousEmployment`
-- FOREIGN KEY (`EmployeePreviousEmploymentId`)
-- REFERENCES `TableEmployeePreviousEmployment` (`Id`)
-- ON DELETE CASCADE;

--  LEAVE  APPLICATION CANCELLATION --

-- ALTER TABLE `tableleaveapplications`
--     ADD COLUMN `CancellationStatus` VARCHAR(20) NULL
--         AFTER `ApprovalRemarks`,

--     ADD COLUMN `CancellationReason` VARCHAR(500) NULL
--         AFTER `CancellationStatus`,

--     ADD COLUMN `CancellationRequestedOn` DATETIME NULL
--         AFTER `CancellationReason`,

--     ADD COLUMN `CancellationRequestedBy` VARCHAR(255) NULL
--         AFTER `CancellationRequestedOn`,

--     ADD COLUMN `CancellationReviewedOn` DATETIME NULL
--         AFTER `CancellationRequestedBy`,

--     ADD COLUMN `CancellationReviewedBy` VARCHAR(255) NULL
--         AFTER `CancellationReviewedOn`,

--     ADD COLUMN `CancellationRemarks` VARCHAR(500) NULL
--         AFTER `CancellationReviewedBy`;

-- CREATE INDEX `IX_LeaveApplications_CancellationStatus`
--     ON `tableleaveapplications` (`CancellationStatus`);

-- CF and YEAR --
-- ALTER TABLE `tableleavebalances`
-- ADD COLUMN `CurrentYearAllocation` DECIMAL(10,2) NOT NULL DEFAULT 0.00
-- AFTER `LeaveYear`,
-- ADD COLUMN `CarryForwardDays` DECIMAL(10,2) NOT NULL DEFAULT 0.00
-- AFTER `CurrentYearAllocation`;

-- REMARK INSTEAD OF BODY --
-- ALTER TABLE tableletters
-- CHANGE COLUMN Body Remark TEXT NULL;

-- ALTER TABLE tablefinancialauditdocuments
-- ADD COLUMN IsActive bit(1) NOT NULL DEFAULT b'1';

-- ALTER TABLE `tableofficeasset`
-- ADD COLUMN `IsActive` bit(1) NOT NULL DEFAULT b'1';

-- ALTER TABLE TableInvoiceMastertableinvoicemaster
-- ADD COLUMN PaymentStatus VARCHAR(30) NOT NULL DEFAULT 'Unpaid'
-- AFTER InvoiceStatus;

-- ALTER TABLE TablePaymentReceipt
-- ADD UNIQUE INDEX UX_TablePaymentReceipt_ReceiptNo (ReceiptNo);

-- ALTER TABLE TableExpenseVouchers
-- MODIFY COLUMN CreatedByUserId VARCHAR(450) NOT NULL,
-- MODIFY COLUMN ApprovedByUserId VARCHAR(450) NULL;

-- ALTER TABLE TableExpenseVouchers
-- ADD UNIQUE INDEX UX_TableExpenseVoucher_VoucherNumber
-- (VoucherNumber);

-- ALTER TABLE TableFinancialSequence
-- ADD UNIQUE INDEX UX_TableFinancialSequence_TypeYear
-- (DocumentType, FinancialYear);

-- ALTER TABLE TableExpenseVouchers
-- ADD COLUMN IsInterState TINYINT(1) NOT NULL DEFAULT 0
-- AFTER GSTRate;

-- ALTER TABLE TableExpenseVouchers
-- ADD COLUMN RejectionReason VARCHAR(500) NULL,
-- ADD COLUMN RejectedByUserId VARCHAR(450) NULL,
-- ADD COLUMN RejectedOn DATETIME NULL;

-- ALTER TABLE TableInvoicemaster
-- ADD COLUMN ProjectId INT NULL AFTER ClientId,
-- ADD CONSTRAINT FK_TableInvoice_TableProject
-- FOREIGN KEY (ProjectId)
-- REFERENCES TableProject(Id)
-- ON DELETE RESTRICT;

-- ALTER TABLE TableGstMonthlySnapshot
-- ADD UNIQUE INDEX UX_TableGstMonthlySnapshot_MonthYear
-- (Month, Year);

-- ALTER TABLE TableGstReturn
-- ADD UNIQUE INDEX UX_TableGstReturn_SnapshotType
-- (SnapshotId, ReturnType);

-- ALTER TABLE TableGstChallan
-- ADD UNIQUE INDEX UX_TableGstChallan_ChallanNumber
-- (ChallanNumber);

-- ALTER TABLE TableInvoicemaster
-- ADD COLUMN IsInterState TINYINT(1) NOT NULL DEFAULT 0
-- AFTER GSTNo;

-- ALTER TABLE TableGstMonthlySnapshot
-- ADD COLUMN FiledByUserId VARCHAR(450) NULL
-- AFTER FiledOn;

-- ALTER TABLE tablegstmonthlysnapshot
-- ADD COLUMN ReopenedByUserId VARCHAR(450) NULL AFTER FiledByUserId,
-- ADD COLUMN ReopenedOn DATETIME NULL AFTER ReopenedByUserId,
-- ADD COLUMN ReopenReason VARCHAR(500) NULL AFTER ReopenedOn;

-- ALTER TABLE tablepfmonthlysnapshot
-- ADD COLUMN FiledByUserId VARCHAR(450) NULL AFTER GeneratedOn,
-- ADD COLUMN FiledOn DATETIME NULL AFTER FiledByUserId,
-- ADD COLUMN PaidByUserId VARCHAR(450) NULL AFTER FiledOn,
-- ADD COLUMN PaidOn DATETIME NULL AFTER PaidByUserId,
-- ADD UNIQUE INDEX UX_PfMonthlySnapshot_MonthYear (Month, Year);

-- ALTER TABLE tableesicmonthlysnapshot
-- ADD COLUMN FiledByUserId VARCHAR(450) NULL AFTER GeneratedOn,
-- ADD COLUMN FiledOn DATETIME NULL AFTER FiledByUserId,
-- ADD COLUMN PaidByUserId VARCHAR(450) NULL AFTER FiledOn,
-- ADD COLUMN PaidOn DATETIME NULL AFTER PaidByUserId,
-- ADD UNIQUE INDEX UX_EsicMonthlySnapshot_MonthYear (Month, Year);

-- ALTER TABLE tableptmonthlysnapshot
-- ADD COLUMN FiledByUserId VARCHAR(450) NULL AFTER GeneratedOn,
-- ADD COLUMN FiledOn DATETIME NULL AFTER FiledByUserId,
-- ADD COLUMN PaidByUserId VARCHAR(450) NULL AFTER FiledOn,
-- ADD COLUMN PaidOn DATETIME NULL AFTER PaidByUserId,
-- ADD UNIQUE INDEX UX_PtMonthlySnapshot_MonthYear (Month, Year);

-- ALTER TABLE tablepaymentreceipt
-- ADD COLUMN CancellationReason VARCHAR(500) NULL AFTER IsCancelled,
-- ADD COLUMN CancelledByUserId VARCHAR(450) NULL AFTER CancellationReason,
-- ADD COLUMN CancelledOn DATETIME NULL AFTER CancelledByUserId;

-- ALTER TABLE `TableEmployeeSalaryStructure`
--     ADD COLUMN `EffectiveTo` DATE NULL AFTER `EffectiveFrom`;

-- ALTER TABLE TableProposal
-- ADD COLUMN ProposalTemplateId INT NULL;

-- ALTER TABLE TableProposal
-- ADD COLUMN RevisionNumber VARCHAR(10) NOT NULL DEFAULT '00',
-- ADD COLUMN PreparedBy VARCHAR(150) NOT NULL DEFAULT '',
-- ADD COLUMN PreparedByDesignation VARCHAR(150) NULL,
-- ADD COLUMN ProblemStatement TEXT NULL,
-- ADD COLUMN Timeline VARCHAR(250) NULL,
-- ADD COLUMN TechnicalSolution TEXT NULL,
-- ADD COLUMN OutOfScope TEXT NULL,
-- ADD COLUMN CustomerResponsibilities TEXT NULL,
-- ADD COLUMN Deliverables TEXT NULL,
-- ADD COLUMN Dependencies TEXT NULL,
-- ADD COLUMN Assumptions TEXT NULL,
-- ADD COLUMN Risks TEXT NULL,
-- ADD COLUMN Warranty TEXT NULL,
-- ADD COLUMN CommercialDescription TEXT NULL,
-- ADD COLUMN PaymentTerms TEXT NULL;

-- ALTER TABLE TableInvoiceMaster
-- ADD COLUMN InvoiceType VARCHAR(30) NOT NULL DEFAULT 'Tax Invoice'
-- AFTER InvoiceNo;

-- ALTER TABLE TableInvoiceMaster
-- ADD COLUMN SACCode VARCHAR(50) NULL AFTER GSTNo,
-- ADD COLUMN KindAttention VARCHAR(200) NULL AFTER SACCode,
-- ADD COLUMN ReceiverName VARCHAR(200) NULL AFTER KindAttention;

-- ALTER TABLE TableInvoiceMaster
-- DROP COLUMN KindAttention,
-- DROP COLUMN ReceiverName;

-- ALTER TABLE TableCompanyProfile
-- ADD COLUMN VendorRegistrationNumber VARCHAR(100) NULL AFTER Phone,
-- ADD COLUMN BankName VARCHAR(150) NULL AFTER VendorRegistrationNumber,
-- ADD COLUMN AccountName VARCHAR(100) NULL AFTER BankName,
-- ADD COLUMN AccountNumber VARCHAR(50) NULL AFTER AccountName,
-- ADD COLUMN IFSCCode VARCHAR(20) NULL AFTER AccountNumber,
-- ADD COLUMN BankBranch VARCHAR(150) NULL AFTER IFSCCode,
-- ADD COLUMN AuthorizedSignatory VARCHAR(150) NULL AFTER BankBranch;

-- ALTER TABLE TableInvoiceMaster
--     ADD COLUMN IssuedByUserId VARCHAR(450) NULL,
--     ADD COLUMN IssuedOn DATETIME NULL,
--     ADD COLUMN CancelledByUserId VARCHAR(450) NULL,
--     ADD COLUMN CancelledOn DATETIME NULL,
--     ADD COLUMN CancellationReason VARCHAR(500) NULL,
--     ADD COLUMN ReplacementInvoiceId INT NULL;

-- ALTER TABLE tableinvoicemaster
-- ADD COLUMN SupplierStateCode VARCHAR(2) NULL,
-- ADD COLUMN CustomerStateCode VARCHAR(2) NULL,
-- ADD COLUMN PlaceOfSupplyStateCode VARCHAR(2) NULL,
-- ADD COLUMN IsGstStateOverride TINYINT(1) NOT NULL DEFAULT 0,
-- ADD COLUMN GstStateOverrideReason VARCHAR(500) NULL;

-- ALTER TABLE tableinvoicemaster
-- ADD COLUMN BillingMilestoneId INT NULL;

-- ALTER TABLE tableproposal
-- ADD AcceptedByUserId VARCHAR(450) NULL,
-- ADD AcceptedOn DATETIME NULL,
-- ADD RejectedByUserId VARCHAR(450) NULL,
-- ADD RejectedOn DATETIME NULL,
-- ADD RejectionReason VARCHAR(500) NULL,
-- ADD ExpiredOn DATETIME NULL,
-- ADD RevisionReason VARCHAR(500) NULL,
-- ADD RevisedByUserId VARCHAR(450) NULL,
-- ADD RevisedOn DATETIME NULL,
-- ADD CustomerApprovalReference VARCHAR(150) NULL,
-- ADD CreatedByUserId VARCHAR(450) NULL,
-- ADD UpdatedByUserId VARCHAR(450) NULL,
-- ADD SubmittedByUserId VARCHAR(450) NULL,
-- ADD SubmittedOn DATETIME NULL,
-- ADD ApprovedByUserId VARCHAR(450) NULL,
-- ADD ApprovedOn DATETIME NULL,
-- ADD IssuedByUserId VARCHAR(450) NULL,
-- ADD IssuedOn DATETIME NULL,
-- ADD CancelledByUserId VARCHAR(450) NULL,
-- ADD CancelledOn DATETIME NULL,
-- ADD CancellationReason VARCHAR(500) NULL;

-- ALTER TABLE tableemployee
-- ADD ResignationDate DATETIME NULL,
-- ADD LastWorkingDate DATETIME NULL,
-- ADD RelievingDate DATETIME NULL;

-- ALTER TABLE aspnetusers
-- ADD ProfilePhotoPath VARCHAR(500) NULL;

-- ALTER TABLE aspnetusers
-- ADD ActivityStatus VARCHAR(30) NOT NULL DEFAULT 'Offline',
-- ADD ActivityStatusMessage VARCHAR(150) NULL,
-- ADD ActivityStatusUpdatedOn DATETIME NULL;

-- ALTER TABLE aspnetusers
-- ADD LastSeenOn DATETIME NULL,
-- ADD IsActivityStatusManual BIT NOT NULL DEFAULT 0;

-- ALTER TABLE aspnetusers
-- ADD EnableRealtimeNotifications BIT NOT NULL DEFAULT 1,
-- ADD EnableNotificationToast BIT NOT NULL DEFAULT 1,
-- ADD EnableNotificationSound BIT NOT NULL DEFAULT 0;

-- ALTER TABLE tablecompanyprofile
-- ADD COLUMN CIN VARCHAR(21) NULL AFTER PAN;

-- ALTER TABLE tableproject
-- ADD COLUMN ClientId INT NULL AFTER ProjectType;

-- ALTER TABLE tableproject
-- ADD INDEX IX_tableproject_ClientId (ClientId);

-- ALTER TABLE tableproject
-- ADD CONSTRAINT FK_tableproject_tableclientmaster_ClientId
-- FOREIGN KEY (ClientId)
-- REFERENCES tableclientmaster(ClientId);

-- ALTER TABLE aspnetusers
-- ADD COLUMN CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP;

-- ALTER TABLE TableProjectCommunications
-- ADD CommunicationType VARCHAR(50) NOT NULL DEFAULT 'Internal';

-- ALTER TABLE TableProjectCommunications
-- ADD Subject VARCHAR(200) NOT NULL DEFAULT '';

-- ALTER TABLE TableProjectCommunications
-- ADD Status VARCHAR(30) NOT NULL DEFAULT 'Open';

-- ALTER TABLE tableclientcommunications
-- ADD COLUMN ShareWithProjectTeam BIT(1) NOT NULL DEFAULT b'0';

-- ALTER TABLE tableclientcommunications
-- ADD COLUMN ProjectSubject VARCHAR(200) NULL;

-- ALTER TABLE tableclientcommunications
-- ADD COLUMN ProjectSummary VARCHAR(5000) NULL;

-- ALTER TABLE tableprojectcommunications
-- ADD COLUMN ClientCommunicationId INT NULL;

-- ALTER TABLE tableprojectcommunications
-- ADD CONSTRAINT FK_ProjectCommunication_ClientCommunication
-- FOREIGN KEY (ClientCommunicationId)
-- REFERENCES tableclientcommunications(Id);

