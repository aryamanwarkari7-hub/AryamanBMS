-- ============================================================
-- ARYAMAN BMS
-- FILE    : 01_CREATE_DATABASE.sql
-- PURPOSE : Creates the AryamanBMS database only
-- RUN     : Run this file before 02_CREATE_ALL_TABLES.sql
-- ============================================================

CREATE DATABASE IF NOT EXISTS `aryamanbms`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE `aryamanbms`;

SELECT DATABASE() AS CurrentDatabase;