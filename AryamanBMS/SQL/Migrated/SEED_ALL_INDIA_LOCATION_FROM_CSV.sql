-- ============================================================
-- Aryaman BMS - Complete All India Location Seed Import
-- Source format: Government of India All India Pincode Directory CSV
-- Expected CSV columns:
-- circlename,regionname,divisionname,officename,pincode,
-- officetype,delivery,district,statename,latitude,longitude
--
-- IMPORTANT:
-- 1. Download the official CSV from data.gov.in.
-- 2. Update the file path below.
-- 3. Enable LOCAL INFILE in MySQL if required.
-- 4. In this design, the official "district" field is stored as CityName,
--    because the current official postal dataset does not provide a
--    dependable nationwide city field.
-- ============================================================
SET GLOBAL local_infile = 1;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `StagingIndiaPincode`;

CREATE TABLE `StagingIndiaPincode`
(
    `CircleName` VARCHAR(150) NULL,
    `RegionName` VARCHAR(150) NULL,
    `DivisionName` VARCHAR(150) NULL,
    `OfficeName` VARCHAR(200) NULL,
    `Pincode` VARCHAR(20) NULL,
    `OfficeType` VARCHAR(50) NULL,
    `Delivery` VARCHAR(50) NULL,
    `DistrictName` VARCHAR(150) NULL,
    `StateName` VARCHAR(150) NULL,
    `Latitude` VARCHAR(50) NULL,
    `Longitude` VARCHAR(50) NULL
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8mb4
COLLATE=utf8mb4_0900_ai_ci;

-- Change this path to the downloaded CSV file.
-- Use forward slashes on Windows.
LOAD DATA LOCAL INFILE
'D:/CODING/ARYAMAN/AryamanBMS/AryamanBMS/AryamanBMS/Data/all_india_pincode_directory.csv'
INTO TABLE `StagingIndiaPincode`
CHARACTER SET utf8mb4
FIELDS TERMINATED BY ','
OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(
    @CircleName,
    @RegionName,
    @DivisionName,
    @OfficeName,
    @Pincode,
    @OfficeType,
    @Delivery,
    @DistrictName,
    @StateName,
    @Latitude,
    @Longitude
)
SET
    `CircleName`   = NULLIF(TRIM(@CircleName), ''),
    `RegionName`   = NULLIF(TRIM(@RegionName), ''),
    `DivisionName` = NULLIF(TRIM(@DivisionName), ''),
    `OfficeName`   = NULLIF(TRIM(@OfficeName), ''),
    `Pincode`      = NULLIF(TRIM(@Pincode), ''),
    `OfficeType`   = NULLIF(TRIM(@OfficeType), ''),
    `Delivery`     = NULLIF(TRIM(@Delivery), ''),
    `DistrictName` = NULLIF(TRIM(@DistrictName), ''),
    `StateName`    = NULLIF(TRIM(@StateName), ''),
    `Latitude`     = NULLIF(TRIM(@Latitude), ''),
    `Longitude`    = NULLIF(TRIM(TRAILING '\r' FROM @Longitude), '');

SET SQL_SAFE_UPDATES = 0;

-- Normalize common whitespace.
UPDATE `StagingIndiaPincode`
SET
    `StateName` = TRIM(REGEXP_REPLACE(`StateName`, '[[:space:]]+', ' ')),
    `DistrictName` = TRIM(REGEXP_REPLACE(`DistrictName`, '[[:space:]]+', ' ')),
    `OfficeName` = TRIM(REGEXP_REPLACE(`OfficeName`, '[[:space:]]+', ' ')),
    `Pincode` = LEFT(TRIM(`Pincode`), 6);

-- Remove unusable rows.
DELETE FROM `StagingIndiaPincode`
WHERE
    `StateName` IS NULL
    OR `DistrictName` IS NULL
    OR `Pincode` IS NULL
    OR `Pincode` NOT REGEXP '^[0-9]{6}$';

-- ------------------------------------------------------------
-- Seed States / Union Territories
-- ------------------------------------------------------------
INSERT INTO `TableState`
(
    `StateName`,
    `StateCode`,
    `IsActive`
)
SELECT DISTINCT
    s.`StateName`,
    NULL,
    b'1'
FROM `StagingIndiaPincode` s
WHERE NOT EXISTS
(
    SELECT 1
    FROM `TableState` ts
    WHERE LOWER(TRIM(ts.`StateName`)) =
          LOWER(TRIM(s.`StateName`))
);

-- Optional State/UT codes.
UPDATE `TableState`
SET `StateCode` = CASE UPPER(TRIM(`StateName`))
    WHEN 'ANDAMAN AND NICOBAR ISLANDS' THEN 'AN'
    WHEN 'ANDHRA PRADESH' THEN 'AP'
    WHEN 'ARUNACHAL PRADESH' THEN 'AR'
    WHEN 'ASSAM' THEN 'AS'
    WHEN 'BIHAR' THEN 'BR'
    WHEN 'CHANDIGARH' THEN 'CH'
    WHEN 'CHHATTISGARH' THEN 'CG'
    WHEN 'DADRA AND NAGAR HAVELI AND DAMAN AND DIU' THEN 'DH'
    WHEN 'DELHI' THEN 'DL'
    WHEN 'GOA' THEN 'GA'
    WHEN 'GUJARAT' THEN 'GJ'
    WHEN 'HARYANA' THEN 'HR'
    WHEN 'HIMACHAL PRADESH' THEN 'HP'
    WHEN 'JAMMU AND KASHMIR' THEN 'JK'
    WHEN 'JHARKHAND' THEN 'JH'
    WHEN 'KARNATAKA' THEN 'KA'
    WHEN 'KERALA' THEN 'KL'
    WHEN 'LADAKH' THEN 'LA'
    WHEN 'LAKSHADWEEP' THEN 'LD'
    WHEN 'MADHYA PRADESH' THEN 'MP'
    WHEN 'MAHARASHTRA' THEN 'MH'
    WHEN 'MANIPUR' THEN 'MN'
    WHEN 'MEGHALAYA' THEN 'ML'
    WHEN 'MIZORAM' THEN 'MZ'
    WHEN 'NAGALAND' THEN 'NL'
    WHEN 'ODISHA' THEN 'OD'
    WHEN 'PUDUCHERRY' THEN 'PY'
    WHEN 'PUNJAB' THEN 'PB'
    WHEN 'RAJASTHAN' THEN 'RJ'
    WHEN 'SIKKIM' THEN 'SK'
    WHEN 'TAMIL NADU' THEN 'TN'
    WHEN 'TELANGANA' THEN 'TS'
    WHEN 'TRIPURA' THEN 'TR'
    WHEN 'UTTAR PRADESH' THEN 'UP'
    WHEN 'UTTARAKHAND' THEN 'UK'
    WHEN 'WEST BENGAL' THEN 'WB'
    ELSE `StateCode`
END;

-- ------------------------------------------------------------
-- Seed Cities
-- Current mapping: DistrictName -> CityName
-- ------------------------------------------------------------
INSERT INTO `TableCity`
(
    `StateId`,
    `CityName`,
    `IsActive`
)
SELECT DISTINCT
    ts.`Id`,
    s.`DistrictName`,
    b'1'
FROM `StagingIndiaPincode` s
INNER JOIN `TableState` ts
    ON LOWER(TRIM(ts.`StateName`)) =
       LOWER(TRIM(s.`StateName`))
WHERE NOT EXISTS
(
    SELECT 1
    FROM `TableCity` tc
    WHERE tc.`StateId` = ts.`Id`
      AND LOWER(TRIM(tc.`CityName`)) =
          LOWER(TRIM(s.`DistrictName`))
);

-- ------------------------------------------------------------
-- Seed Pincodes / Post Offices
-- One row per City + PIN + OfficeName.
-- ------------------------------------------------------------
INSERT INTO `TablePincode`
(
    `CityId`,
    `Pincode`,
    `AreaName`,
    `IsActive`
)
SELECT DISTINCT
    tc.`Id`,
    s.`Pincode`,
    s.`OfficeName`,
    b'1'
FROM `StagingIndiaPincode` s
INNER JOIN `TableState` ts
    ON LOWER(TRIM(ts.`StateName`)) =
       LOWER(TRIM(s.`StateName`))
INNER JOIN `TableCity` tc
    ON tc.`StateId` = ts.`Id`
   AND LOWER(TRIM(tc.`CityName`)) =
       LOWER(TRIM(s.`DistrictName`))
WHERE NOT EXISTS
(
    SELECT 1
    FROM `TablePincode` tp
    WHERE tp.`CityId` = tc.`Id`
      AND tp.`Pincode` = s.`Pincode`
      AND COALESCE(LOWER(TRIM(tp.`AreaName`)), '') =
          COALESCE(LOWER(TRIM(s.`OfficeName`)), '')
);

-- ------------------------------------------------------------
-- Verification
-- ------------------------------------------------------------
SELECT COUNT(*) AS `StateCount`
FROM `TableState`;

SELECT COUNT(*) AS `CityCount`
FROM `TableCity`;

SELECT COUNT(*) AS `PincodeOfficeCount`
FROM `TablePincode`;

SELECT
    ts.`StateName`,
    COUNT(DISTINCT tc.`Id`) AS `CityCount`,
    COUNT(tp.`Id`) AS `PincodeOfficeCount`
FROM `TableState` ts
LEFT JOIN `TableCity` tc
    ON tc.`StateId` = ts.`Id`
LEFT JOIN `TablePincode` tp
    ON tp.`CityId` = tc.`Id`
GROUP BY ts.`Id`, ts.`StateName`
ORDER BY ts.`StateName`;

DROP TABLE IF EXISTS `StagingIndiaPincode`;

SET FOREIGN_KEY_CHECKS = 1;
