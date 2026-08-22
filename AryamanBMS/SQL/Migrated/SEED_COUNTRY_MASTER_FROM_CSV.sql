-- ============================================================
-- Aryaman BMS - Country Master Seed Import
-- Source: Data/country_master.csv
-- Expected CSV columns:
-- id,name,iso3,iso2,numeric_code,phonecode,capital,currency,...
-- ============================================================
SET NAMES utf8mb4;

DROP TABLE IF EXISTS `StagingCountry`;

CREATE TABLE `StagingCountry`
(
    `CountryName` VARCHAR(150) NULL,
    `Iso2Code` VARCHAR(2) NULL,
    `Iso3Code` VARCHAR(3) NULL,
    `DefaultCurrencyCode` VARCHAR(3) NULL,
    `PhoneCode` VARCHAR(10) NULL
)
ENGINE = InnoDB
DEFAULT CHARSET = utf8mb4;

LOAD DATA LOCAL INFILE
'D:/CODING/ARYAMAN/AryamanBMS/AryamanBMS/AryamanBMS/Data/country_master.csv'
INTO TABLE `StagingCountry`
CHARACTER SET utf8mb4
FIELDS TERMINATED BY ','
OPTIONALLY ENCLOSED BY '"'
LINES TERMINATED BY '\n'
IGNORE 1 ROWS
(
    @SourceId,
    @CountryName,
    @Iso3Code,
    @Iso2Code,
    @NumericCode,
    @PhoneCode,
    @Capital,
    @Currency,
    @CurrencyName,
    @CurrencySymbol,
    @Tld,
    @Native,
    @Population,
    @Gdp,
    @Region,
    @RegionId,
    @SubRegion,
    @SubRegionId,
    @Nationality,
    @AreaSqKm,
    @PostalCodeFormat,
    @PostalCodeRegex,
    @TimeZones,
    @Latitude,
    @Longitude,
    @Emoji,
    @EmojiU,
    @WikiDataId
)
SET
    `CountryName` = NULLIF(TRIM(@CountryName), ''),
    `Iso2Code` = NULLIF(UPPER(TRIM(@Iso2Code)), ''),
    `Iso3Code` = NULLIF(UPPER(TRIM(@Iso3Code)), ''),
    `DefaultCurrencyCode` = NULLIF(UPPER(TRIM(@Currency)), ''),
    `PhoneCode` = CASE
        WHEN NULLIF(TRIM(@PhoneCode), '') IS NULL THEN NULL
        WHEN LEFT(TRIM(@PhoneCode), 1) = '+'
            THEN TRIM(@PhoneCode)
        ELSE CONCAT('+', TRIM(@PhoneCode))
    END;


SET SQL_SAFE_UPDATES = 0;
DELETE FROM `StagingCountry`
WHERE
    `CountryName` IS NULL
    OR `Iso2Code` IS NULL
    OR `Iso3Code` IS NULL
    OR CHAR_LENGTH(`Iso2Code`) <> 2
    OR CHAR_LENGTH(`Iso3Code`) <> 3;

INSERT INTO `TableCountry`
(
    `CountryName`,
    `Iso2Code`,
    `Iso3Code`,
    `DefaultCurrencyCode`,
    `PhoneCode`,
    `SortOrder`,
    `IsActive`
)
SELECT
    s.`CountryName`,
    s.`Iso2Code`,
    s.`Iso3Code`,
    s.`DefaultCurrencyCode`,
    s.`PhoneCode`,
    CASE WHEN s.`Iso2Code` = 'IN' THEN 1 ELSE 2 END,
    b'1'
FROM `StagingCountry` s
ON DUPLICATE KEY UPDATE
    `CountryName` = VALUES(`CountryName`),
    `DefaultCurrencyCode` = VALUES(`DefaultCurrencyCode`),
    `PhoneCode` = VALUES(`PhoneCode`),
    `SortOrder` = VALUES(`SortOrder`),
    `IsActive` = b'1';

SELECT
    `Id`,
    `CountryName`,
    `Iso2Code`,
    `Iso3Code`,
    `DefaultCurrencyCode`,
    `PhoneCode`
FROM `TableCountry`
ORDER BY `SortOrder`, `CountryName`;

SELECT COUNT(*) AS `CountryCount`
FROM `TableCountry`;

DROP TABLE IF EXISTS `StagingCountry`;

SET SQL_SAFE_UPDATES = 1;