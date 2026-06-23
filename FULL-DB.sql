CREATE DATABASE  IF NOT EXISTS `aryamanbms` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `aryamanbms`;
-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: aryamanbms
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20260608092401_InitialIdentity','8.0.27'),('20260608103317_AddUserIsActive','8.0.27'),('20260609045821_AttendanceSetup','8.0.27');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroleclaims`
--

LOCK TABLES `aspnetroleclaims` WRITE;
/*!40000 ALTER TABLE `aspnetroleclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroleclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroles`
--

LOCK TABLES `aspnetroles` WRITE;
/*!40000 ALTER TABLE `aspnetroles` DISABLE KEYS */;
INSERT INTO `aspnetroles` VALUES ('52521853-eea6-433a-9b36-8137be610b3a','HR','HR',NULL),('898e30e9-af4b-497f-bd28-47fc80e08a2c','Employee','EMPLOYEE',NULL),('b3a619b7-0d6c-4ca1-b49c-97e4400cd856','Admin','ADMIN',NULL);
/*!40000 ALTER TABLE `aspnetroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`),
  KEY `IX_AspNetUserClaims_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserclaims`
--

LOCK TABLES `aspnetuserclaims` WRITE;
/*!40000 ALTER TABLE `aspnetuserclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `IX_AspNetUserLogins_UserId` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserlogins`
--

LOCK TABLES `aspnetuserlogins` WRITE;
/*!40000 ALTER TABLE `aspnetuserlogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserlogins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserroles`
--

LOCK TABLES `aspnetuserroles` WRITE;
/*!40000 ALTER TABLE `aspnetuserroles` DISABLE KEYS */;
INSERT INTO `aspnetuserroles` VALUES ('91409511-95f0-4e1b-b4b8-509cf2c089cf','52521853-eea6-433a-9b36-8137be610b3a'),('11309cea-a14d-4532-b0ae-d23dfe35f543','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('2f1a4ecc-779f-4927-bbfd-510ec27db388','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('450770c4-a815-44c4-a753-65e3e67189b2','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('7c1d767b-acbe-4ed3-a27b-c756272d122c','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('8bc702b7-f0b8-4f8b-85b8-88fdc2909916','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('a0a4634b-6abd-4f0e-b0cf-7e32bfb2d319','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('d844ce86-04a0-4e95-969b-dfa7418caa45','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('e495b45f-cbd0-48ff-95ba-eaf612387d0b','898e30e9-af4b-497f-bd28-47fc80e08a2c'),('fc3b0448-7305-4217-a574-ae61c6f80085','b3a619b7-0d6c-4ca1-b49c-97e4400cd856');
/*!40000 ALTER TABLE `aspnetuserroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) NOT NULL,
  `FullName` longtext,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `ConcurrencyStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`),
  KEY `EmailIndex` (`NormalizedEmail`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusers`
--

LOCK TABLES `aspnetusers` WRITE;
/*!40000 ALTER TABLE `aspnetusers` DISABLE KEYS */;
INSERT INTO `aspnetusers` VALUES ('11309cea-a14d-4532-b0ae-d23dfe35f543','Sharan Negi','negi','NEGI','sn@email.com','SN@EMAIL.COM',1,'AQAAAAIAAYagAAAAEElCVVsDmTG+fksW0nt4zR82QT6+GOwhK0zCDeAWazyN27VdO82e4OzCqaxd4vCTPw==','6CIR25FOHYSAMF25FITIUQLEK6WJGJJK','44f8a829-6e20-436b-9e0c-4f152ad86b47',NULL,0,0,NULL,1,0,1),('2f1a4ecc-779f-4927-bbfd-510ec27db388','Sarang Datar','datar-main','DATAR-MAIN','sd@email.com','SD@EMAIL.COM',1,'AQAAAAIAAYagAAAAEKUiLsOv9tjhVmCxjD534xmq6Ow6A1tS8+c3KX9NPYza9pwCuSLMtJE+Ls6w2zrKHA==','FRFRPI5B2RUMWEQSDRBGGKKRD6ZDEZKM','f7241d8f-f813-4746-98f7-65f35fd005a2',NULL,0,0,NULL,1,0,1),('450770c4-a815-44c4-a753-65e3e67189b2','Ajit Kenjale','Kenjale','KENJALE','ak@email.com','AK@EMAIL.COM',1,'AQAAAAIAAYagAAAAEHZZ5xJXPTMSu1G1xrO2740HlqStFfkP8Oq//HKAVGvGTkpGn48LqQkrIKi1cPsSSg==','5TI4SDR4WZ22UN3V5HCFV6NNGQXTKTG7','e859c72c-2b03-41ef-86c8-d2225a25cf17',NULL,0,0,NULL,1,0,1),('7c1d767b-acbe-4ed3-a27b-c756272d122c','Sakshi Kajale','kajale','KAJALE','sk@email.com','SK@EMAIL.COM',1,'AQAAAAIAAYagAAAAEDawlHfYMHV5jO0sdAoOzl0iIJ0tyQwNfe6RTVWXPdD7ZKK93PBzrIKnK5K3DxOFSA==','47XHQM2AOJTMBGGYUPFLSRGVJLPM56E2','d0193dd9-9d3f-4a40-88f3-25d2258da1e2',NULL,0,0,NULL,1,0,1),('8bc702b7-f0b8-4f8b-85b8-88fdc2909916','Atharv Warkari','warkari','WARKARI','aw@email.com','AW@EMAIL.COM',1,'AQAAAAIAAYagAAAAEF1LkkAXgyOLghMW24FXx29zQHy6cNwQPBlcjGmHOcyu/GG91VpYk9mErjkJuSM56g==','735KI6TKHJRAA6BGXBAHHNERBSDDQM3M','791097b7-ea0e-4d01-8b97-fcefc8ff5087',NULL,0,0,NULL,1,0,1),('91409511-95f0-4e1b-b4b8-509cf2c089cf','HR Manager','HR','HR','hr@aryamanbms.com','HR@ARYAMANBMS.COM',1,'AQAAAAIAAYagAAAAEOMs6812Hekx9hw/EXkkMBmtjgreAyp1QSo36o/+9QGgba9idsrs7VXjJrwvG96V8A==','F42RGUYQYS6EJ3APSWK3THDYM4EITPC7','7919b791-7f39-4ad6-9022-b1c909e00f7d',NULL,0,0,NULL,1,0,1),('a0a4634b-6abd-4f0e-b0cf-7e32bfb2d319','Sameer Bhagwat','bhagwat','BHAGWAT','sb@email.com','SB@EMAIL.COM',1,'AQAAAAIAAYagAAAAEKGXm84lY5X6fKxytD8z2RVv4rIQAy8T2uLFatZ7FnrFFkSFoL3taiLf7U8F8K6/0w==','Y2YMSCAJVOF5Y2LK6JLVQIQMFWXNHQFR','01ff5398-33e3-4e57-b092-1d511bd960a7',NULL,0,0,NULL,1,0,1),('d844ce86-04a0-4e95-969b-dfa7418caa45','Pranoti Patil','Patil-main','PATIL-MAIN','pp@email.com','PP@EMAIL.COM',1,'AQAAAAIAAYagAAAAEFP1LNc9HMnK9MOLrgkZOV7512EIOD7yD5lnrQyPg3olxHch8Ns0+eWOWIbkCxcNzw==','KXSFTDNBPEVAZOCBZCIJWA7AD43CODJM','ba0f9959-269f-4726-8fd3-0b9e9d977b03',NULL,0,0,NULL,1,0,1),('e495b45f-cbd0-48ff-95ba-eaf612387d0b','Pramod Mahajan','mahajan','MAHAJAN','pm@email.com','PM@EMAIL.COM',1,'AQAAAAIAAYagAAAAENRBEiRGxDI8cukrDnjHW4OGsu9LN3SVx4zW9CBq9KJMwxIGiQi8Im8MUQOTuN6V7w==','DAY7TPHXKVADOE4ECMNQDVNSX6PX4MSM','515421a4-7a74-492d-9806-ee924ef215f8',NULL,0,0,NULL,1,0,1),('fc3b0448-7305-4217-a574-ae61c6f80085','System Administrator','admin','ADMIN','admin@aryamanbms.com','ADMIN@ARYAMANBMS.COM',1,'AQAAAAIAAYagAAAAEE62Xx1hg61E2NhaXJEQzKDRvCo9totkeWpRb1L8Rpwx298i1P4wVeHLpKrglmr5dQ==','4MGVX4ZZ6PPTWLS6JNW4JMMAIEW35XEW','8a1101dd-c4a2-47a4-9558-3bfa5a93198b',NULL,0,0,NULL,1,0,1);
/*!40000 ALTER TABLE `aspnetusers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusertokens`
--

LOCK TABLES `aspnetusertokens` WRITE;
/*!40000 ALTER TABLE `aspnetusertokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusertokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableattendance`
--

DROP TABLE IF EXISTS `tableattendance`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableattendance` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `AttendanceDate` date NOT NULL,
  `CheckInTime` datetime DEFAULT NULL,
  `CheckOutTime` datetime DEFAULT NULL,
  `Remarks` varchar(500) DEFAULT NULL,
  `CreatedOn` datetime NOT NULL,
  `LocationType` varchar(50) DEFAULT NULL,
  `Status` varchar(10) NOT NULL DEFAULT 'P',
  PRIMARY KEY (`Id`),
  KEY `FK_Attendance_Employee` (`EmployeeId`),
  CONSTRAINT `FK_Attendance_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=281 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableattendance`
--

LOCK TABLES `tableattendance` WRITE;
/*!40000 ALTER TABLE `tableattendance` DISABLE KEYS */;
INSERT INTO `tableattendance` VALUES (9,1,'2026-05-10','2026-05-10 09:05:00','2026-05-10 18:10:00','Present','2026-06-10 10:52:05','Office','WO'),(10,2,'2026-05-10','2026-05-10 09:10:00','2026-05-10 18:00:00','Present','2026-06-10 10:52:05','Office','WO'),(11,4,'2026-05-10','2026-05-10 09:00:00','2026-05-10 18:15:00','Present','2026-06-10 10:52:05','Office','WO'),(12,5,'2026-05-10','2026-05-10 09:20:00','2026-05-10 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(13,6,'2026-05-10','2026-05-10 08:55:00','2026-05-10 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(14,7,'2026-05-10','2026-05-10 09:15:00','2026-05-10 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(15,8,'2026-05-10','2026-05-10 09:00:00','2026-05-10 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(16,9,'2026-05-10','2026-05-10 09:05:00','2026-05-10 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(17,1,'2026-05-11','2026-05-11 09:05:00','2026-05-11 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(18,2,'2026-05-11','2026-05-11 09:10:00','2026-05-11 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(19,4,'2026-05-11','2026-05-11 09:00:00','2026-05-11 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(20,5,'2026-05-11','2026-05-11 09:20:00','2026-05-11 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(21,6,'2026-05-11','2026-05-11 08:55:00','2026-05-11 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(22,7,'2026-05-11','2026-05-11 09:15:00','2026-05-11 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(23,8,'2026-05-11','2026-05-11 09:00:00','2026-05-11 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(24,9,'2026-05-11','2026-05-11 09:05:00','2026-05-11 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(25,1,'2026-05-12','2026-05-12 09:05:00','2026-05-12 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(26,2,'2026-05-12','2026-05-12 09:10:00','2026-05-12 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(27,4,'2026-05-12','2026-05-12 09:00:00','2026-05-12 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(28,5,'2026-05-12','2026-05-12 09:20:00','2026-05-12 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(29,6,'2026-05-12','2026-05-12 08:55:00','2026-05-12 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(30,7,'2026-05-12','2026-05-12 09:15:00','2026-05-12 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(31,8,'2026-05-12','2026-05-12 09:00:00','2026-05-12 18:00:00','Present','2026-06-10 10:52:05','Office','A'),(32,9,'2026-05-12','2026-05-12 09:05:00','2026-05-12 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(33,1,'2026-05-13','2026-05-13 09:05:00','2026-05-13 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(34,2,'2026-05-13','2026-05-13 09:10:00','2026-05-13 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(35,4,'2026-05-13','2026-05-13 09:00:00','2026-05-13 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(36,5,'2026-05-13','2026-05-13 09:20:00','2026-05-13 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(37,6,'2026-05-13','2026-05-13 08:55:00','2026-05-13 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(38,7,'2026-05-13','2026-05-13 09:15:00','2026-05-13 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(39,8,'2026-05-13','2026-05-13 09:00:00','2026-05-13 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(40,9,'2026-05-13','2026-05-13 09:05:00','2026-05-13 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(41,1,'2026-05-14','2026-05-14 09:05:00','2026-05-14 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(42,2,'2026-05-14','2026-05-14 09:10:00','2026-05-14 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(43,4,'2026-05-14','2026-05-14 09:00:00','2026-05-14 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(44,5,'2026-05-14','2026-05-14 09:20:00','2026-05-14 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(45,6,'2026-05-14','2026-05-14 08:55:00','2026-05-14 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(46,7,'2026-05-14','2026-05-14 09:15:00','2026-05-14 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(47,8,'2026-05-14','2026-05-14 09:00:00','2026-05-14 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(48,9,'2026-05-14','2026-05-14 09:05:00','2026-05-14 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(49,1,'2026-05-15','2026-05-15 09:05:00','2026-05-15 18:10:00','Present','2026-06-10 10:52:05','Office','L'),(50,2,'2026-05-15','2026-05-15 09:10:00','2026-05-15 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(51,4,'2026-05-15','2026-05-15 09:00:00','2026-05-15 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(52,5,'2026-05-15','2026-05-15 09:20:00','2026-05-15 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(53,6,'2026-05-15','2026-05-15 08:55:00','2026-05-15 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(54,7,'2026-05-15','2026-05-15 09:15:00','2026-05-15 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(55,8,'2026-05-15','2026-05-15 09:00:00','2026-05-15 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(56,9,'2026-05-15','2026-05-15 09:05:00','2026-05-15 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(57,1,'2026-05-16','2026-05-16 09:05:00','2026-05-16 18:10:00','Present','2026-06-10 10:52:05','Office','WO'),(58,2,'2026-05-16','2026-05-16 09:10:00','2026-05-16 18:00:00','Present','2026-06-10 10:52:05','Office','WO'),(59,4,'2026-05-16','2026-05-16 09:00:00','2026-05-16 18:15:00','Present','2026-06-10 10:52:05','Office','WO'),(60,5,'2026-05-16','2026-05-16 09:20:00','2026-05-16 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(61,6,'2026-05-16','2026-05-16 08:55:00','2026-05-16 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(62,7,'2026-05-16','2026-05-16 09:15:00','2026-05-16 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(63,8,'2026-05-16','2026-05-16 09:00:00','2026-05-16 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(64,9,'2026-05-16','2026-05-16 09:05:00','2026-05-16 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(65,1,'2026-05-17','2026-05-17 09:05:00','2026-05-17 18:10:00','Present','2026-06-10 10:52:05','Office','WO'),(66,2,'2026-05-17','2026-05-17 09:10:00','2026-05-17 18:00:00','Present','2026-06-10 10:52:05','Office','WO'),(67,4,'2026-05-17','2026-05-17 09:00:00','2026-05-17 18:15:00','Present','2026-06-10 10:52:05','Office','WO'),(68,5,'2026-05-17','2026-05-17 09:20:00','2026-05-17 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(69,6,'2026-05-17','2026-05-17 08:55:00','2026-05-17 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(70,7,'2026-05-17','2026-05-17 09:15:00','2026-05-17 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(71,8,'2026-05-17','2026-05-17 09:00:00','2026-05-17 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(72,9,'2026-05-17','2026-05-17 09:05:00','2026-05-17 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(73,1,'2026-05-18','2026-05-18 09:05:00','2026-05-18 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(74,2,'2026-05-18','2026-05-18 09:10:00','2026-05-18 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(75,4,'2026-05-18','2026-05-18 09:00:00','2026-05-18 18:15:00','Present','2026-06-10 10:52:05','Office','OD'),(76,5,'2026-05-18','2026-05-18 09:20:00','2026-05-18 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(77,6,'2026-05-18','2026-05-18 08:55:00','2026-05-18 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(78,7,'2026-05-18','2026-05-18 09:15:00','2026-05-18 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(79,8,'2026-05-18','2026-05-18 09:00:00','2026-05-18 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(80,9,'2026-05-18','2026-05-18 09:05:00','2026-05-18 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(81,1,'2026-05-19','2026-05-19 09:05:00','2026-05-19 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(82,2,'2026-05-19','2026-05-19 09:10:00','2026-05-19 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(83,4,'2026-05-19','2026-05-19 09:00:00','2026-05-19 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(84,5,'2026-05-19','2026-05-19 09:20:00','2026-05-19 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(85,6,'2026-05-19','2026-05-19 08:55:00','2026-05-19 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(86,7,'2026-05-19','2026-05-19 09:15:00','2026-05-19 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(87,8,'2026-05-19','2026-05-19 09:00:00','2026-05-19 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(88,9,'2026-05-19','2026-05-19 09:05:00','2026-05-19 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(89,1,'2026-05-20','2026-05-20 09:05:00','2026-05-20 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(90,2,'2026-05-20','2026-05-20 09:10:00','2026-05-20 18:00:00','Present','2026-06-10 10:52:05','Office','A'),(91,4,'2026-05-20','2026-05-20 09:00:00','2026-05-20 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(92,5,'2026-05-20','2026-05-20 09:20:00','2026-05-20 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(93,6,'2026-05-20','2026-05-20 08:55:00','2026-05-20 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(94,7,'2026-05-20','2026-05-20 09:15:00','2026-05-20 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(95,8,'2026-05-20','2026-05-20 09:00:00','2026-05-20 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(96,9,'2026-05-20','2026-05-20 09:05:00','2026-05-20 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(97,1,'2026-05-21','2026-05-21 09:05:00','2026-05-21 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(98,2,'2026-05-21','2026-05-21 09:10:00','2026-05-21 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(99,4,'2026-05-21','2026-05-21 09:00:00','2026-05-21 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(100,5,'2026-05-21','2026-05-21 09:20:00','2026-05-21 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(101,6,'2026-05-21','2026-05-21 08:55:00','2026-05-21 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(102,7,'2026-05-21','2026-05-21 09:15:00','2026-05-21 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(103,8,'2026-05-21','2026-05-21 09:00:00','2026-05-21 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(104,9,'2026-05-21','2026-05-21 09:05:00','2026-05-21 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(105,1,'2026-05-22','2026-05-22 09:05:00','2026-05-22 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(106,2,'2026-05-22','2026-05-22 09:10:00','2026-05-22 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(107,4,'2026-05-22','2026-05-22 09:00:00','2026-05-22 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(108,5,'2026-05-22','2026-05-22 09:20:00','2026-05-22 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(109,6,'2026-05-22','2026-05-22 08:55:00','2026-05-22 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(110,7,'2026-05-22','2026-05-22 09:15:00','2026-05-22 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(111,8,'2026-05-22','2026-05-22 09:00:00','2026-05-22 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(112,9,'2026-05-22','2026-05-22 09:05:00','2026-05-22 18:05:00','Present','2026-06-10 10:52:05','Client Site','OD'),(113,1,'2026-05-23','2026-05-23 09:05:00','2026-05-23 18:10:00','Present','2026-06-10 10:52:05','Office','WO'),(114,2,'2026-05-23','2026-05-23 09:10:00','2026-05-23 18:00:00','Present','2026-06-10 10:52:05','Office','WO'),(115,4,'2026-05-23','2026-05-23 09:00:00','2026-05-23 18:15:00','Present','2026-06-10 10:52:05','Office','WO'),(116,5,'2026-05-23','2026-05-23 09:20:00','2026-05-23 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(117,6,'2026-05-23','2026-05-23 08:55:00','2026-05-23 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(118,7,'2026-05-23','2026-05-23 09:15:00','2026-05-23 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(119,8,'2026-05-23','2026-05-23 09:00:00','2026-05-23 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(120,9,'2026-05-23','2026-05-23 09:05:00','2026-05-23 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(121,1,'2026-05-24','2026-05-24 09:05:00','2026-05-24 18:10:00','Present','2026-06-10 10:52:05','Office','WO'),(122,2,'2026-05-24','2026-05-24 09:10:00','2026-05-24 18:00:00','Present','2026-06-10 10:52:05','Office','WO'),(123,4,'2026-05-24','2026-05-24 09:00:00','2026-05-24 18:15:00','Present','2026-06-10 10:52:05','Office','WO'),(124,5,'2026-05-24','2026-05-24 09:20:00','2026-05-24 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(125,6,'2026-05-24','2026-05-24 08:55:00','2026-05-24 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(126,7,'2026-05-24','2026-05-24 09:15:00','2026-05-24 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(127,8,'2026-05-24','2026-05-24 09:00:00','2026-05-24 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(128,9,'2026-05-24','2026-05-24 09:05:00','2026-05-24 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(129,1,'2026-05-25','2026-05-25 09:05:00','2026-05-25 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(130,2,'2026-05-25','2026-05-25 09:10:00','2026-05-25 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(131,4,'2026-05-25','2026-05-25 09:00:00','2026-05-25 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(132,5,'2026-05-25','2026-05-25 09:20:00','2026-05-25 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(133,6,'2026-05-25','2026-05-25 08:55:00','2026-05-25 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(134,7,'2026-05-25','2026-05-25 09:15:00','2026-05-25 18:00:00','Present','2026-06-10 10:52:05','Office','L'),(135,8,'2026-05-25','2026-05-25 09:00:00','2026-05-25 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(136,9,'2026-05-25','2026-05-25 09:05:00','2026-05-25 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(137,1,'2026-05-26','2026-05-26 09:05:00','2026-05-26 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(138,2,'2026-05-26','2026-05-26 09:10:00','2026-05-26 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(139,4,'2026-05-26','2026-05-26 09:00:00','2026-05-26 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(140,5,'2026-05-26','2026-05-26 09:20:00','2026-05-26 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(141,6,'2026-05-26','2026-05-26 08:55:00','2026-05-26 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(142,7,'2026-05-26','2026-05-26 09:15:00','2026-05-26 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(143,8,'2026-05-26','2026-05-26 09:00:00','2026-05-26 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(144,9,'2026-05-26','2026-05-26 09:05:00','2026-05-26 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(145,1,'2026-05-27','2026-05-27 09:05:00','2026-05-27 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(146,2,'2026-05-27','2026-05-27 09:10:00','2026-05-27 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(147,4,'2026-05-27','2026-05-27 09:00:00','2026-05-27 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(148,5,'2026-05-27','2026-05-27 09:20:00','2026-05-27 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(149,6,'2026-05-27','2026-05-27 08:55:00','2026-05-27 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(150,7,'2026-05-27','2026-05-27 09:15:00','2026-05-27 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(151,8,'2026-05-27','2026-05-27 09:00:00','2026-05-27 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(152,9,'2026-05-27','2026-05-27 09:05:00','2026-05-27 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(153,1,'2026-05-28','2026-05-28 09:05:00','2026-05-28 18:10:00','Present','2026-06-10 10:52:05','Office','P'),(154,2,'2026-05-28','2026-05-28 09:10:00','2026-05-28 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(155,4,'2026-05-28','2026-05-28 09:00:00','2026-05-28 18:15:00','Present','2026-06-10 10:52:05','Office','P'),(156,5,'2026-05-28','2026-05-28 09:20:00','2026-05-28 18:05:00','Present','2026-06-10 10:52:05','Office','P'),(157,6,'2026-05-28','2026-05-28 08:55:00','2026-05-28 18:20:00','Present','2026-06-10 10:52:05','Remote','P'),(158,7,'2026-05-28','2026-05-28 09:15:00','2026-05-28 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(159,8,'2026-05-28','2026-05-28 09:00:00','2026-05-28 18:00:00','Present','2026-06-10 10:52:05','Office','P'),(160,9,'2026-05-28','2026-05-28 09:05:00','2026-05-28 18:05:00','Present','2026-06-10 10:52:05','Client Site','P'),(161,1,'2026-05-29','2026-05-29 09:05:00','2026-05-29 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(162,2,'2026-05-29','2026-05-29 09:10:00','2026-05-29 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(163,4,'2026-05-29','2026-05-29 09:00:00','2026-05-29 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(164,5,'2026-05-29','2026-05-29 09:20:00','2026-05-29 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(165,6,'2026-05-29','2026-05-29 08:55:00','2026-05-29 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(166,7,'2026-05-29','2026-05-29 09:15:00','2026-05-29 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(167,8,'2026-05-29','2026-05-29 09:00:00','2026-05-29 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(168,9,'2026-05-29','2026-05-29 09:05:00','2026-05-29 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(169,1,'2026-05-30','2026-05-30 09:05:00','2026-05-30 18:10:00','Present','2026-06-10 10:52:06','Office','WO'),(170,2,'2026-05-30','2026-05-30 09:10:00','2026-05-30 18:00:00','Present','2026-06-10 10:52:06','Office','WO'),(171,4,'2026-05-30','2026-05-30 09:00:00','2026-05-30 18:15:00','Present','2026-06-10 10:52:06','Office','WO'),(172,5,'2026-05-30','2026-05-30 09:20:00','2026-05-30 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(173,6,'2026-05-30','2026-05-30 08:55:00','2026-05-30 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(174,7,'2026-05-30','2026-05-30 09:15:00','2026-05-30 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(175,8,'2026-05-30','2026-05-30 09:00:00','2026-05-30 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(176,9,'2026-05-30','2026-05-30 09:05:00','2026-05-30 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(177,1,'2026-05-31','2026-05-31 09:05:00','2026-05-31 18:10:00','Present','2026-06-10 10:52:06','Office','WO'),(178,2,'2026-05-31','2026-05-31 09:10:00','2026-05-31 18:00:00','Present','2026-06-10 10:52:06','Office','WO'),(179,4,'2026-05-31','2026-05-31 09:00:00','2026-05-31 18:15:00','Present','2026-06-10 10:52:06','Office','WO'),(180,5,'2026-05-31','2026-05-31 09:20:00','2026-05-31 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(181,6,'2026-05-31','2026-05-31 08:55:00','2026-05-31 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(182,7,'2026-05-31','2026-05-31 09:15:00','2026-05-31 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(183,8,'2026-05-31','2026-05-31 09:00:00','2026-05-31 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(184,9,'2026-05-31','2026-05-31 09:05:00','2026-05-31 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(185,1,'2026-06-01','2026-06-01 09:05:00','2026-06-01 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(186,2,'2026-06-01','2026-06-01 09:10:00','2026-06-01 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(187,4,'2026-06-01','2026-06-01 09:00:00','2026-06-01 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(188,5,'2026-06-01','2026-06-01 09:20:00','2026-06-01 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(189,6,'2026-06-01','2026-06-01 08:55:00','2026-06-01 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(190,7,'2026-06-01','2026-06-01 09:15:00','2026-06-01 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(191,8,'2026-06-01','2026-06-01 09:00:00','2026-06-01 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(192,9,'2026-06-01','2026-06-01 09:05:00','2026-06-01 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(193,1,'2026-06-02','2026-06-02 09:05:00','2026-06-02 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(194,2,'2026-06-02','2026-06-02 09:10:00','2026-06-02 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(195,4,'2026-06-02','2026-06-02 09:00:00','2026-06-02 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(196,5,'2026-06-02','2026-06-02 09:20:00','2026-06-02 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(197,6,'2026-06-02','2026-06-02 08:55:00','2026-06-02 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(198,7,'2026-06-02','2026-06-02 09:15:00','2026-06-02 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(199,8,'2026-06-02','2026-06-02 09:00:00','2026-06-02 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(200,9,'2026-06-02','2026-06-02 09:05:00','2026-06-02 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(201,1,'2026-06-03','2026-06-03 09:05:00','2026-06-03 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(202,2,'2026-06-03','2026-06-03 09:10:00','2026-06-03 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(203,4,'2026-06-03','2026-06-03 09:00:00','2026-06-03 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(204,5,'2026-06-03','2026-06-03 09:20:00','2026-06-03 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(205,6,'2026-06-03','2026-06-03 08:55:00','2026-06-03 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(206,7,'2026-06-03','2026-06-03 09:15:00','2026-06-03 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(207,8,'2026-06-03','2026-06-03 09:00:00','2026-06-03 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(208,9,'2026-06-03','2026-06-03 09:05:00','2026-06-03 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(209,1,'2026-06-04','2026-06-04 09:05:00','2026-06-04 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(210,2,'2026-06-04','2026-06-04 09:10:00','2026-06-04 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(211,4,'2026-06-04','2026-06-04 09:00:00','2026-06-04 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(212,5,'2026-06-04','2026-06-04 09:20:00','2026-06-04 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(213,6,'2026-06-04','2026-06-04 08:55:00','2026-06-04 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(214,7,'2026-06-04','2026-06-04 09:15:00','2026-06-04 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(215,8,'2026-06-04','2026-06-04 09:00:00','2026-06-04 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(216,9,'2026-06-04','2026-06-04 09:05:00','2026-06-04 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(217,1,'2026-06-05','2026-06-05 09:05:00','2026-06-05 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(218,2,'2026-06-05','2026-06-05 09:10:00','2026-06-05 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(219,4,'2026-06-05','2026-06-05 09:00:00','2026-06-05 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(220,5,'2026-06-05','2026-06-05 09:20:00','2026-06-05 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(221,6,'2026-06-05','2026-06-05 08:55:00','2026-06-05 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(222,7,'2026-06-05','2026-06-05 09:15:00','2026-06-05 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(223,8,'2026-06-05','2026-06-05 09:00:00','2026-06-05 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(224,9,'2026-06-05','2026-06-05 09:05:00','2026-06-05 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(225,1,'2026-06-06','2026-06-06 09:05:00','2026-06-06 18:10:00','Present','2026-06-10 10:52:06','Office','WO'),(226,2,'2026-06-06','2026-06-06 09:10:00','2026-06-06 18:00:00','Present','2026-06-10 10:52:06','Office','WO'),(227,4,'2026-06-06','2026-06-06 09:00:00','2026-06-06 18:15:00','Present','2026-06-10 10:52:06','Office','WO'),(228,5,'2026-06-06','2026-06-06 09:20:00','2026-06-06 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(229,6,'2026-06-06','2026-06-06 08:55:00','2026-06-06 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(230,7,'2026-06-06','2026-06-06 09:15:00','2026-06-06 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(231,8,'2026-06-06','2026-06-06 09:00:00','2026-06-06 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(232,9,'2026-06-06','2026-06-06 09:05:00','2026-06-06 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(233,1,'2026-06-07','2026-06-07 09:05:00','2026-06-07 18:10:00','Present','2026-06-10 10:52:06','Office','WO'),(234,2,'2026-06-07','2026-06-07 09:10:00','2026-06-07 18:00:00','Present','2026-06-10 10:52:06','Office','WO'),(235,4,'2026-06-07','2026-06-07 09:00:00','2026-06-07 18:15:00','Present','2026-06-10 10:52:06','Office','WO'),(236,5,'2026-06-07','2026-06-07 09:20:00','2026-06-07 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(237,6,'2026-06-07','2026-06-07 08:55:00','2026-06-07 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(238,7,'2026-06-07','2026-06-07 09:15:00','2026-06-07 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(239,8,'2026-06-07','2026-06-07 09:00:00','2026-06-07 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(240,9,'2026-06-07','2026-06-07 09:05:00','2026-06-07 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(241,1,'2026-06-08','2026-06-08 09:05:00','2026-06-08 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(242,2,'2026-06-08','2026-06-08 09:10:00','2026-06-08 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(243,4,'2026-06-08','2026-06-08 09:00:00','2026-06-08 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(244,5,'2026-06-08','2026-06-08 09:20:00','2026-06-08 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(245,6,'2026-06-08','2026-06-08 08:55:00','2026-06-08 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(246,7,'2026-06-08','2026-06-08 09:15:00','2026-06-08 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(247,8,'2026-06-08','2026-06-08 09:00:00','2026-06-08 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(248,9,'2026-06-08','2026-06-08 09:05:00','2026-06-08 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(249,1,'2026-06-09','2026-06-09 09:05:00','2026-06-09 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(250,2,'2026-06-09','2026-06-09 09:10:00','2026-06-09 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(251,4,'2026-06-09','2026-06-09 09:00:00','2026-06-09 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(252,5,'2026-06-09','2026-06-09 09:20:00','2026-06-09 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(253,6,'2026-06-09','2026-06-09 08:55:00','2026-06-09 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(254,7,'2026-06-09','2026-06-09 09:15:00','2026-06-09 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(255,8,'2026-06-09','2026-06-09 09:00:00','2026-06-09 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(256,9,'2026-06-09','2026-06-09 09:05:00','2026-06-09 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(257,1,'2026-06-10','2026-06-10 09:05:00','2026-06-10 18:10:00','Present','2026-06-10 10:52:06','Office','P'),(258,2,'2026-06-10','2026-06-10 09:10:00','2026-06-10 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(259,4,'2026-06-10','2026-06-10 09:00:00','2026-06-10 18:15:00','Present','2026-06-10 10:52:06','Office','P'),(260,5,'2026-06-10','2026-06-10 09:20:00','2026-06-10 18:05:00','Present','2026-06-10 10:52:06','Office','P'),(261,6,'2026-06-10','2026-06-10 08:55:00','2026-06-10 18:20:00','Present','2026-06-10 10:52:06','Remote','P'),(262,7,'2026-06-10','2026-06-10 09:15:00','2026-06-10 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(263,8,'2026-06-10','2026-06-10 09:00:00','2026-06-10 18:00:00','Present','2026-06-10 10:52:06','Office','P'),(264,9,'2026-06-10','2026-06-10 09:05:00','2026-06-10 18:05:00','Present','2026-06-10 10:52:06','Client Site','P'),(265,1,'2026-06-15',NULL,NULL,'Leave approved: LA00003','2026-06-10 14:26:56',NULL,'L'),(266,1,'2026-06-16',NULL,NULL,'Leave approved: LA00003','2026-06-10 14:26:56',NULL,'L'),(267,1,'2026-06-17',NULL,NULL,'Leave approved: LA00003','2026-06-10 14:26:56',NULL,'L'),(268,1,'2026-06-18',NULL,NULL,'Leave approved: LA00003','2026-06-10 14:26:56',NULL,'L'),(269,1,'2026-06-11',NULL,'2026-06-11 15:48:53','Leave approved: LA00004','2026-06-10 14:49:41',NULL,'L'),(270,2,'2026-06-11',NULL,NULL,'Leave approved: LA00005','2026-06-11 16:19:21',NULL,'L'),(271,2,'2026-06-12',NULL,NULL,'Leave approved: LA00005','2026-06-11 16:19:21',NULL,'L'),(272,6,'2026-06-11',NULL,NULL,NULL,'2026-06-11 17:25:53',NULL,'P'),(273,4,'2026-06-11','2026-06-11 17:34:15','2026-06-11 17:34:19',NULL,'2026-06-11 17:26:05','Office','P'),(274,5,'2026-06-11','2026-06-11 17:33:20',NULL,NULL,'2026-06-11 17:26:31','Office','P'),(275,8,'2026-06-11',NULL,NULL,NULL,'2026-06-11 17:26:42',NULL,'P'),(276,7,'2026-06-11',NULL,NULL,NULL,'2026-06-11 17:27:00',NULL,'P'),(277,9,'2026-06-11',NULL,NULL,NULL,'2026-06-11 17:27:09',NULL,'P'),(278,1,'2026-12-24',NULL,NULL,'Auto marked from approved leave','2026-06-12 12:59:49',NULL,'L'),(279,5,'2026-06-12','2026-06-12 16:45:09',NULL,NULL,'2026-06-12 16:45:09','Office','P'),(280,1,'2026-06-30',NULL,NULL,'Leave approved: LA00010','2026-06-12 17:09:53',NULL,'L');
/*!40000 ALTER TABLE `tableattendance` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tabledepartment`
--

DROP TABLE IF EXISTS `tabledepartment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tabledepartment` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DisplayCode` varchar(20) DEFAULT NULL,
  `DepartmentName` varchar(100) NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tabledepartment`
--

LOCK TABLES `tabledepartment` WRITE;
/*!40000 ALTER TABLE `tabledepartment` DISABLE KEYS */;
INSERT INTO `tabledepartment` VALUES (1,'HR','Human Resources',_binary '','2026-06-08 11:58:49',NULL),(2,'IT','Information Technology',_binary '','2026-06-08 11:58:49',NULL),(3,'ACC','Accounts & Finance',_binary '\0','2026-06-08 11:58:49',NULL),(4,'SAL','Sales',_binary '\0','2026-06-08 11:58:49',NULL),(5,'ADM','Administration',_binary '\0','2026-06-08 11:58:49',NULL),(6,'OPS','Operations',_binary '\0','2026-06-08 11:58:49',NULL);
/*!40000 ALTER TABLE `tabledepartment` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tabledesignation`
--

DROP TABLE IF EXISTS `tabledesignation`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tabledesignation` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DisplayCode` varchar(20) DEFAULT NULL,
  `DesignationName` varchar(100) NOT NULL,
  `DepartmentId` int NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_Designation_Department` (`DepartmentId`),
  CONSTRAINT `FK_Designation_Department` FOREIGN KEY (`DepartmentId`) REFERENCES `tabledepartment` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tabledesignation`
--

LOCK TABLES `tabledesignation` WRITE;
/*!40000 ALTER TABLE `tabledesignation` DISABLE KEYS */;
INSERT INTO `tabledesignation` VALUES (1,'HR','HR Executive',1,_binary '\0','2026-06-08 11:59:46',NULL),(2,'HRM','HR Manager',1,_binary '','2026-06-08 11:59:46',NULL),(3,'SD','Software Developer',2,_binary '','2026-06-08 11:59:46',NULL),(4,'SSE','Senior Software Engineer',2,_binary '','2026-06-08 11:59:46',NULL),(5,'TL','Team Lead',2,_binary '\0','2026-06-08 11:59:46',NULL),(6,'PM','Project Manager',2,_binary '','2026-06-08 11:59:46',NULL),(9,'SA','System Administrator',2,_binary '\0','2026-06-08 11:59:46',NULL),(10,'ACC','Accountant',3,_binary '\0','2026-06-08 11:59:46',NULL),(12,'SE','Sales Executive',4,_binary '','2026-06-08 11:59:46',NULL),(13,'SM','Sales Manager',4,_binary '','2026-06-08 11:59:46',NULL),(14,'ADM','Administrator',5,_binary '\0','2026-06-08 11:59:46',NULL),(15,'OM','Office Manager',5,_binary '','2026-06-08 11:59:46',NULL),(17,'OPM','Operations Manager',6,_binary '','2026-06-08 11:59:46',NULL);
/*!40000 ALTER TABLE `tabledesignation` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableemployee`
--

DROP TABLE IF EXISTS `tableemployee`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployee` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeCode` varchar(20) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) DEFAULT NULL,
  `MobileNumber` varchar(20) DEFAULT NULL,
  `JoiningDate` date NOT NULL,
  `DepartmentId` int NOT NULL,
  `DesignationId` int NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  `ApplicationUserId` varchar(255) DEFAULT NULL,
  `DateOfBirth` date DEFAULT NULL,
  `Gender` varchar(20) DEFAULT NULL,
  `PersonalEmail` varchar(150) DEFAULT NULL,
  `OfficialEmail` varchar(150) DEFAULT NULL,
  `PermanentAddress` text,
  `City` varchar(100) DEFAULT NULL,
  `State` varchar(100) DEFAULT NULL,
  `PinCode` varchar(20) DEFAULT NULL,
  `EmergencyContact` varchar(150) DEFAULT NULL,
  `EmergencyPhone` varchar(20) DEFAULT NULL,
  `AadhaarNo` varchar(20) DEFAULT NULL,
  `PanNo` varchar(20) DEFAULT NULL,
  `UanNo` varchar(30) DEFAULT NULL,
  `EmploymentType` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_EmployeeCode` (`EmployeeCode`),
  KEY `FK_Employee_Department` (`DepartmentId`),
  KEY `FK_Employee_Designation` (`DesignationId`),
  KEY `IX_TableEmployee_ApplicationUserId` (`ApplicationUserId`),
  KEY `IX_TableEmployee_AadhaarNo` (`AadhaarNo`),
  KEY `IX_TableEmployee_PanNo` (`PanNo`),
  KEY `IX_TableEmployee_UanNo` (`UanNo`),
  CONSTRAINT `FK_Employee_AspNetUsers` FOREIGN KEY (`ApplicationUserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Employee_Department` FOREIGN KEY (`DepartmentId`) REFERENCES `tabledepartment` (`Id`),
  CONSTRAINT `FK_Employee_Designation` FOREIGN KEY (`DesignationId`) REFERENCES `tabledesignation` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableemployee`
--

LOCK TABLES `tableemployee` WRITE;
/*!40000 ALTER TABLE `tableemployee` DISABLE KEYS */;
INSERT INTO `tableemployee` VALUES (1,'EMP1','Atharv','Warkari','987654321','2026-05-26',2,3,_binary '','2026-06-08 12:01:06',NULL,'8bc702b7-f0b8-4f8b-85b8-88fdc2909916','2002-11-28','Male','aw@email.com','aw@aryaman.com','rherhnfdndbdsvdnbfd','Pune','Maharashtra','411038','Ashwini','9875649281','123456789765','6543567890',NULL,'Intern'),(2,'EMP2','Sakshi','Kajale','923456789','2026-05-26',2,3,_binary '','2026-06-08 13:38:35',NULL,'7c1d767b-acbe-4ed3-a27b-c756272d122c',NULL,NULL,NULL,'sk@email.com',NULL,NULL,NULL,NULL,NULL,NULL,'535675454321',NULL,NULL,'Intern'),(4,'EMP4','Pranoti','Patil','987654321','2024-01-01',2,4,_binary '','2026-06-08 13:42:40',NULL,'d844ce86-04a0-4e95-969b-dfa7418caa45','1998-06-24','Female','pp@gmail.com','pp@email.com','wgwenedbdfjrtjhgervwecsc','Pune','Maharashtra','411052',NULL,NULL,NULL,NULL,NULL,'Permanent'),(5,'EMP5','Sameer','Bhagwat','987654321','2024-01-01',5,15,_binary '','2026-06-08 13:44:42',NULL,'a0a4634b-6abd-4f0e-b0cf-7e32bfb2d319',NULL,'Male','aw@email.com','sb@email.com','qwf2qgqvwqdgwdcwqfqwf','Pune','Maharashtra','411025',NULL,NULL,'123456789765','6543567890',NULL,'Consultant'),(6,'EMP6','Pramod','Mahajan','923456789','2020-01-15',4,13,_binary '','2026-06-08 13:45:26',NULL,'e495b45f-cbd0-48ff-95ba-eaf612387d0b',NULL,'Male',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'Permanent'),(7,'EMP7','Sarang','Datar','95632543678','2020-06-27',3,10,_binary '','2026-06-08 13:46:32',NULL,'2f1a4ecc-779f-4927-bbfd-510ec27db388',NULL,'Male',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'Consultant'),(8,'EMP8','Bhagyashri','Sali','923456789','2018-01-01',1,1,_binary '','2026-06-08 17:06:57',NULL,'91409511-95f0-4e1b-b4b8-509cf2c089cf',NULL,'Female',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'Permanent'),(9,'EMP9','Sharan','Negi','9856472857','2026-06-07',5,15,_binary '\0','2026-06-09 14:06:58',NULL,'11309cea-a14d-4532-b0ae-d23dfe35f543',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL),(10,'EMP10','Ajit','Kenjale','95632543678','2026-06-07',2,4,_binary '','2026-06-12 17:44:58',NULL,'450770c4-a815-44c4-a753-65e3e67189b2','1994-06-27','Male',NULL,'ak@email.com',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'Permanent'),(11,'EMP11','Arunima','Sinha','8654321567','0026-07-01',2,5,_binary '','2026-06-22 12:37:13',NULL,NULL,'2004-07-31','Female',NULL,'as@email.com','ebsebnsdb dshshegt gedgtwe','Bangalore','Karnataka','211567',NULL,NULL,NULL,NULL,NULL,'Contract');
/*!40000 ALTER TABLE `tableemployee` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableemployeeacademic`
--

DROP TABLE IF EXISTS `tableemployeeacademic`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployeeacademic` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `QualificationLevel` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `CourseName` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Specialization` varchar(150) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `InstituteName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `BoardOrUniversity` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PassingYear` int DEFAULT NULL,
  `ResultType` varchar(30) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Score` decimal(6,2) DEFAULT NULL,
  `Grade` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `IsHighestQualification` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `IX_EmployeeAcademic_EmployeeId` (`EmployeeId`),
  CONSTRAINT `FK_EmployeeAcademic_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableemployeeacademic`
--

LOCK TABLES `tableemployeeacademic` WRITE;
/*!40000 ALTER TABLE `tableemployeeacademic` DISABLE KEYS */;
INSERT INTO `tableemployeeacademic` VALUES (1,11,'Graduation','BE Computer Engineering',NULL,NULL,NULL,NULL,'Percentage',8.04,'A',1),(2,1,'Graduation','Bachelors of Engineering','Computer Engineering','MMCOE','SPPU',2025,'CGPA',8.04,'A',1),(3,1,'12th','Science',' NA','MES Abasaheb Garware','MSBSHSE',2021,'Percentage',88.67,'A',0);
/*!40000 ALTER TABLE `tableemployeeacademic` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableemployeedocument`
--

DROP TABLE IF EXISTS `tableemployeedocument`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableemployeedocument` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `EmployeeAcademicId` int DEFAULT NULL,
  `DocumentType` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `OriginalFileName` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `StoredFileName` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `StoragePath` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ContentType` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `FileSize` bigint NOT NULL,
  `UploadedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UploadedBy` varchar(256) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EmployeeDocument_EmployeeId` (`EmployeeId`),
  KEY `IX_EmployeeDocument_AcademicId` (`EmployeeAcademicId`),
  CONSTRAINT `FK_EmployeeDocument_Academic` FOREIGN KEY (`EmployeeAcademicId`) REFERENCES `tableemployeeacademic` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_EmployeeDocument_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableemployeedocument`
--

LOCK TABLES `tableemployeedocument` WRITE;
/*!40000 ALTER TABLE `tableemployeedocument` DISABLE KEYS */;
INSERT INTO `tableemployeedocument` VALUES (1,11,1,'Degree Certificate','building-insurance.png','075af3f33ed84c3f8a5acc14d96d8c1f.png','EmployeeDocuments\\EMP11\\075af3f33ed84c3f8a5acc14d96d8c1f.png','image/png',22855,'2026-06-22 12:37:13','admin'),(3,1,2,'Marksheet','lively_t.jpg','462dd7cf7e874ee397d1113fff440659.jpg','EmployeeDocuments\\EMP1\\462dd7cf7e874ee397d1113fff440659.jpg','image/jpeg',79853,'2026-06-22 13:00:15','admin'),(4,1,3,'Degree Certificate','building-insurance.png','55c250d42d2c44a2b76fbf39737f3af4.png','EmployeeDocuments\\EMP1\\55c250d42d2c44a2b76fbf39737f3af4.png','image/png',22855,'2026-06-22 13:06:05','admin');
/*!40000 ALTER TABLE `tableemployeedocument` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableleaveapplications`
--

DROP TABLE IF EXISTS `tableleaveapplications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleaveapplications` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ApplicationNumber` varchar(50) NOT NULL,
  `EmployeeId` int NOT NULL,
  `LeaveTypeId` int NOT NULL,
  `FromDate` date NOT NULL,
  `ToDate` date NOT NULL,
  `NumberOfDays` decimal(10,2) NOT NULL,
  `Reason` varchar(1000) DEFAULT NULL,
  `AppliedOn` datetime NOT NULL,
  `Status` varchar(20) NOT NULL,
  `ApprovedBy` varchar(100) DEFAULT NULL,
  `ApprovedOn` datetime DEFAULT NULL,
  `ApprovalRemarks` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_LeaveApplications_tableleaveType` (`LeaveTypeId`),
  KEY `IX_LeaveApplications_EmployeeId` (`EmployeeId`),
  KEY `IX_LeaveApplications_Status` (`Status`),
  KEY `IX_LeaveApplications_FromDate` (`FromDate`),
  CONSTRAINT `FK_LeaveApplications_tableemployee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`),
  CONSTRAINT `FK_LeaveApplications_tableleaveType` FOREIGN KEY (`LeaveTypeId`) REFERENCES `tableleavetypes` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableleaveapplications`
--

LOCK TABLES `tableleaveapplications` WRITE;
/*!40000 ALTER TABLE `tableleaveapplications` DISABLE KEYS */;
INSERT INTO `tableleaveapplications` VALUES (1,'LA00001',1,2,'2026-06-11','2026-06-12',2.00,'Fever','2026-06-10 14:16:00','Approved','admin','2026-06-10 14:17:33',NULL),(2,'LA00001',1,2,'2026-06-11','2026-06-12',2.00,'Fever','2026-06-10 14:16:00','Rejected','admin','2026-06-10 14:17:30',NULL),(3,'LA00003',1,1,'2026-06-15','2026-06-18',4.00,'Family function','2026-06-10 14:26:05','Approved','admin','2026-06-10 14:26:56',NULL),(4,'LA00004',1,3,'2026-06-11','2026-06-11',1.00,'Family Emergency','2026-06-10 14:49:05','Approved','admin','2026-06-10 14:49:41',NULL),(5,'LA00005',2,2,'2026-06-11','2026-06-12',2.00,'Cough and Cold','2026-06-10 14:57:19','Approved','admin','2026-06-11 16:19:21',NULL),(6,'LA00006',1,3,'2026-12-24','2026-12-24',1.00,'Carry Forward','2026-06-12 12:59:18','Approved','admin','2026-06-12 12:59:49',NULL),(7,'LA00007',2,8,'2026-06-30','2026-06-30',1.00,'Family Emergency','2026-06-12 13:05:23','Rejected','admin','2026-06-12 13:13:00',NULL),(8,'LA00008',5,7,'2026-06-17','2026-06-18',2.00,'Ok','2026-06-12 13:14:18','Rejected','admin','2026-06-12 13:15:18',NULL),(9,'LA00009',5,2,'2026-06-13','2026-06-13',1.00,'fever','2026-06-12 16:48:22','Cancelled','admin','2026-06-12 16:49:06','Cancelled by user.'),(10,'LA00010',1,9,'2026-06-30','2026-06-30',1.00,'Birthday','2026-06-12 17:09:39','Approved','admin','2026-06-12 17:09:53',NULL);
/*!40000 ALTER TABLE `tableleaveapplications` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableleavebalances`
--

DROP TABLE IF EXISTS `tableleavebalances`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleavebalances` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `LeaveTypeId` int NOT NULL,
  `LeaveYear` int NOT NULL,
  `AllocatedDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  `UsedDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  `BalanceDays` decimal(10,2) NOT NULL DEFAULT '0.00',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_tableleavebalances` (`EmployeeId`,`LeaveTypeId`,`LeaveYear`),
  KEY `FK_LeaveBalances_tableleavetypes` (`LeaveTypeId`),
  KEY `IX_LeaveBalances_EmployeeId` (`EmployeeId`),
  CONSTRAINT `FK_LeaveBalances_tableemploye` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`),
  CONSTRAINT `FK_LeaveBalances_tableleavetypes` FOREIGN KEY (`LeaveTypeId`) REFERENCES `tableleavetypes` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableleavebalances`
--

LOCK TABLES `tableleavebalances` WRITE;
/*!40000 ALTER TABLE `tableleavebalances` DISABLE KEYS */;
INSERT INTO `tableleavebalances` VALUES (1,1,1,2026,12.00,0.00,12.00),(2,1,2,2026,12.00,0.00,12.00),(3,1,3,2026,15.00,2.00,13.00),(4,1,4,2026,0.00,0.00,0.00),(5,1,5,2026,15.00,0.00,15.00),(6,1,6,2026,0.00,0.00,0.00),(7,2,1,2026,12.00,0.00,12.00),(8,2,2,2026,12.00,2.00,10.00),(9,2,3,2026,15.00,0.00,15.00),(10,2,4,2026,0.00,0.00,0.00),(11,2,5,2026,15.00,0.00,15.00),(12,2,6,2026,0.00,0.00,0.00),(13,4,1,2026,12.00,0.00,12.00),(14,4,2,2026,12.00,0.00,12.00),(15,4,3,2026,15.00,0.00,15.00),(16,4,4,2026,0.00,0.00,0.00),(17,4,5,2026,15.00,0.00,15.00),(18,4,6,2026,0.00,0.00,0.00),(19,5,1,2026,12.00,0.00,12.00),(20,5,2,2026,12.00,0.00,12.00),(21,5,3,2026,15.00,0.00,15.00),(22,5,4,2026,0.00,0.00,0.00),(23,5,5,2026,15.00,0.00,15.00),(24,5,6,2026,0.00,0.00,0.00),(25,6,1,2026,12.00,0.00,12.00),(26,6,2,2026,12.00,0.00,12.00),(27,6,3,2026,15.00,0.00,15.00),(28,6,4,2026,0.00,0.00,0.00),(29,6,5,2026,15.00,0.00,15.00),(30,6,6,2026,0.00,0.00,0.00),(31,7,1,2026,12.00,0.00,12.00),(32,7,2,2026,12.00,0.00,12.00),(33,7,3,2026,15.00,0.00,15.00),(34,7,4,2026,0.00,0.00,0.00),(35,7,5,2026,15.00,0.00,15.00),(36,7,6,2026,0.00,0.00,0.00),(37,8,1,2026,12.00,0.00,12.00),(38,8,2,2026,12.00,0.00,12.00),(39,8,3,2026,15.00,0.00,15.00),(40,8,4,2026,0.00,0.00,0.00),(41,8,5,2026,15.00,0.00,15.00),(42,8,6,2026,0.00,0.00,0.00),(43,9,1,2026,12.00,0.00,12.00),(44,9,2,2026,12.00,0.00,12.00),(45,9,3,2026,15.00,0.00,15.00),(46,9,4,2026,0.00,0.00,0.00),(47,9,5,2026,15.00,0.00,15.00),(48,9,6,2026,0.00,0.00,0.00),(49,1,8,2026,0.00,0.00,0.00),(50,1,9,2026,1.00,1.00,0.00),(51,2,8,2026,0.00,0.00,0.00),(52,2,9,2026,1.00,0.00,1.00),(53,4,8,2026,0.00,0.00,0.00),(54,4,9,2026,1.00,0.00,1.00),(55,5,8,2026,0.00,0.00,0.00),(56,5,9,2026,1.00,0.00,1.00),(57,6,8,2026,0.00,0.00,0.00),(58,6,9,2026,1.00,0.00,1.00),(59,7,8,2026,0.00,0.00,0.00),(60,7,9,2026,1.00,0.00,1.00),(61,8,8,2026,0.00,0.00,0.00),(62,8,9,2026,1.00,0.00,1.00),(63,9,8,2026,0.00,0.00,0.00),(64,9,9,2026,1.00,0.00,1.00);
/*!40000 ALTER TABLE `tableleavebalances` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableleavetypes`
--

DROP TABLE IF EXISTS `tableleavetypes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableleavetypes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LeaveCode` varchar(20) NOT NULL,
  `LeaveName` varchar(100) NOT NULL,
  `DaysPerYear` int NOT NULL DEFAULT '0',
  `IsCarryForward` bit(1) NOT NULL DEFAULT b'0',
  `IsPaidLeave` bit(1) NOT NULL DEFAULT b'1',
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableleavetypes`
--

LOCK TABLES `tableleavetypes` WRITE;
/*!40000 ALTER TABLE `tableleavetypes` DISABLE KEYS */;
INSERT INTO `tableleavetypes` VALUES (1,'CL','Casual Leave',12,_binary '\0',_binary '',_binary ''),(2,'SL','Sick Leave',12,_binary '\0',_binary '',_binary ''),(3,'PL','Privilege Leave',15,_binary '',_binary '',_binary ''),(4,'ML','Maternity Leave',0,_binary '\0',_binary '\0',_binary ''),(5,'PTL','Paternity Leave',15,_binary '\0',_binary '',_binary ''),(6,'COMP','Comp Off',5,_binary '\0',_binary '',_binary ''),(7,'LWP','Leave Without Pay',5,_binary '\0',_binary '\0',_binary ''),(8,'UPL','Unplanned Leave',3,_binary '\0',_binary '',_binary ''),(9,'BDL','Birthday Leave',1,_binary '\0',_binary '',_binary '');
/*!40000 ALTER TABLE `tableleavetypes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableletters`
--

DROP TABLE IF EXISTS `tableletters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableletters` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `LetterNumber` varchar(50) NOT NULL,
  `LetterType` varchar(50) NOT NULL,
  `EmployeeId` int NOT NULL,
  `Subject` varchar(200) NOT NULL,
  `Body` text NOT NULL,
  `DocumentPath` varchar(500) DEFAULT NULL,
  `IssuedBy` varchar(100) DEFAULT NULL,
  `IssuedOn` datetime NOT NULL,
  `IsActive` bit(1) NOT NULL DEFAULT b'1',
  PRIMARY KEY (`Id`),
  KEY `FK_Letters_Employee` (`EmployeeId`),
  CONSTRAINT `FK_Letters_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableletters`
--

LOCK TABLES `tableletters` WRITE;
/*!40000 ALTER TABLE `tableletters` DISABLE KEYS */;
INSERT INTO `tableletters` VALUES (1,'LTR-2026-001','Offer',1,'Internship Offer Letter','Please find attached your internship offer letter as per our discussion.\r\n\r\nKindly review the details mentioned in the document and send me your acceptance of the same.','/documents/letters/9cb04f0c-cdd6-4f0e-a394-a49f55787400_Aryaman Offer Letter.pdf','admin','2026-06-11 12:55:12',_binary '');
/*!40000 ALTER TABLE `tableletters` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tableproject`
--

DROP TABLE IF EXISTS `tableproject`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tableproject` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ProjectCode` varchar(20) NOT NULL,
  `ProjectName` varchar(150) NOT NULL,
  `ProjectType` varchar(50) NOT NULL,
  `ClientName` varchar(150) DEFAULT NULL,
  `BusinessObjective` text,
  `Scope` text,
  `StartDate` date DEFAULT NULL,
  `EndDate` date DEFAULT NULL,
  `Budget` decimal(18,2) NOT NULL DEFAULT '0.00',
  `Priority` varchar(20) NOT NULL DEFAULT 'Medium',
  `Status` varchar(30) NOT NULL DEFAULT 'Planning',
  `ProjectManagerId` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedOn` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_TableProject_ProjectCode` (`ProjectCode`),
  KEY `IX_TableProject_ProjectManagerId` (`ProjectManagerId`),
  KEY `IX_TableProject_Status` (`Status`),
  KEY `IX_TableProject_IsActive` (`IsActive`),
  CONSTRAINT `FK_TableProject_ProjectManager` FOREIGN KEY (`ProjectManagerId`) REFERENCES `tableemployee` (`Id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tableproject`
--

LOCK TABLES `tableproject` WRITE;
/*!40000 ALTER TABLE `tableproject` DISABLE KEYS */;
INSERT INTO `tableproject` VALUES (1,'PRJ001','Website Build','Software','Aryaman Technologies PrivateLimited',NULL,NULL,'2026-06-07','2026-09-29',48000.00,'High','Planning',1,1,'2026-06-22 16:51:19','2026-06-22 17:53:51');
/*!40000 ALTER TABLE `tableproject` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tablesalaryrecord`
--

DROP TABLE IF EXISTS `tablesalaryrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tablesalaryrecord` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `EmployeeId` int NOT NULL,
  `Month` int NOT NULL,
  `Year` int NOT NULL,
  `ActualSalary` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PayDays` decimal(18,2) NOT NULL DEFAULT '0.00',
  `BasicSalary` decimal(18,2) NOT NULL,
  `HRA` decimal(18,2) NOT NULL,
  `Conveyance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `MedicalAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `EducationAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `SpecialAllowance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `DA` decimal(18,2) NOT NULL,
  `OtherAllowances` decimal(18,2) NOT NULL,
  `GrossSalary` decimal(18,2) NOT NULL,
  `TotalEarnings` decimal(18,2) NOT NULL DEFAULT '0.00',
  `GrossMinusConveyance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PfDeduction` decimal(18,2) NOT NULL,
  `EsicDeduction` decimal(18,2) NOT NULL,
  `ProfessionalTax` decimal(18,2) NOT NULL DEFAULT '0.00',
  `Advance` decimal(18,2) NOT NULL DEFAULT '0.00',
  `TotalDeductions` decimal(18,2) NOT NULL DEFAULT '0.00',
  `TdsDeduction` decimal(18,2) NOT NULL,
  `OtherDeductions` decimal(18,2) NOT NULL,
  `NetSalary` decimal(18,2) NOT NULL,
  `EmployerPf` decimal(18,2) NOT NULL DEFAULT '0.00',
  `EmployerEsic` decimal(18,2) NOT NULL DEFAULT '0.00',
  `CTC` decimal(18,2) NOT NULL DEFAULT '0.00',
  `PaymentStatus` varchar(20) NOT NULL DEFAULT 'Pending',
  `PaidOn` datetime DEFAULT NULL,
  `PresentDays` int NOT NULL DEFAULT '0',
  `LeaveDays` int NOT NULL DEFAULT '0',
  `AbsentDays` int NOT NULL DEFAULT '0',
  `SourceFileName` varchar(255) DEFAULT NULL,
  `ImportedOn` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Remark` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_SalaryRecord_Employee_Month_Year` (`EmployeeId`,`Month`,`Year`),
  CONSTRAINT `FK_SalaryRecord_Employee` FOREIGN KEY (`EmployeeId`) REFERENCES `tableemployee` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tablesalaryrecord`
--

LOCK TABLES `tablesalaryrecord` WRITE;
/*!40000 ALTER TABLE `tablesalaryrecord` DISABLE KEYS */;
INSERT INTO `tablesalaryrecord` VALUES (1,1,6,2026,38550.00,18.00,11192.00,4477.00,2798.00,1679.00,1119.00,1119.00,5000.00,2000.00,22383.87,22384.00,19586.00,1800.00,84.00,200.00,0.00,2084.00,0.00,0.00,20300.00,1950.00,364.00,24698.00,'Paid','2026-06-22 11:50:34',0,0,0,'Salary_Template_6_2026.xlsx','2026-06-12 18:15:59',''),(2,2,6,2026,43050.00,12.00,8332.00,3333.00,2083.00,1250.00,833.00,833.00,5000.00,2000.00,16664.52,16664.00,14581.00,1750.00,63.00,0.00,0.00,1813.00,0.00,0.00,14851.00,1896.00,271.00,18831.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(3,4,6,2026,40550.00,11.00,7194.00,2878.00,1799.00,1079.00,719.00,719.00,5000.00,2000.00,14388.71,14388.00,12589.00,1511.00,54.00,0.00,0.00,1565.00,0.00,0.00,12823.00,1637.00,234.00,16259.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(4,5,6,2026,28050.00,12.00,5429.00,2172.00,1357.00,814.00,543.00,543.00,5000.00,2000.00,10858.06,10858.00,9501.00,1140.00,41.00,200.00,0.00,1381.00,0.00,0.00,9477.00,1235.00,177.00,12270.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(5,6,6,2026,20000.00,11.00,3548.00,1419.00,887.00,532.00,355.00,355.00,5000.00,2000.00,7096.77,7096.00,6209.00,745.00,27.00,0.00,0.00,772.00,0.00,0.00,6324.00,807.00,116.00,8019.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(6,7,6,2026,32050.00,11.00,5686.00,2274.00,1422.00,853.00,569.00,569.00,5000.00,2000.00,11372.58,11373.00,9951.00,1194.00,43.00,200.00,0.00,1437.00,0.00,0.00,9936.00,1294.00,185.00,12852.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(7,8,6,2026,32050.00,11.00,5686.00,2274.00,1422.00,853.00,569.00,569.00,5000.00,2000.00,11372.58,11373.00,9951.00,1194.00,43.00,0.00,0.00,1237.00,0.00,0.00,10136.00,1294.00,185.00,12852.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16',''),(8,9,6,2026,40550.00,11.00,7434.00,2974.00,1859.00,1115.00,743.00,743.00,5000.00,2000.00,14868.00,14868.00,13009.00,1561.00,56.00,0.00,0.00,1617.00,0.00,0.00,13251.00,1691.00,242.00,16801.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (2).xlsx','2026-06-12 16:14:34',''),(17,10,6,2026,42500.00,12.00,8226.00,3290.00,2057.00,1234.00,823.00,823.00,0.00,0.00,16451.61,16453.00,14396.00,1728.00,62.00,200.00,0.00,1990.00,0.00,0.00,14463.00,1871.00,268.00,18592.00,'Pending',NULL,0,0,0,'Salary_Template_6_2026 (1).xlsx','2026-06-22 11:52:16','');
/*!40000 ALTER TABLE `tablesalaryrecord` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'aryamanbms'
--

--
-- Dumping routines for database 'aryamanbms'
--
/*!50003 DROP PROCEDURE IF EXISTS `POMELO_AFTER_ADD_PRIMARY_KEY` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `POMELO_AFTER_ADD_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255), IN `COLUMN_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID INT(11);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
			AND `COLUMN_TYPE` LIKE '%int%'
			AND `COLUMN_KEY` = 'PRI';
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_NAME` = COLUMN_NAME_ARGUMENT
				AND `COLUMN_TYPE` LIKE '%int%'
				AND `COLUMN_KEY` = 'PRI';
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL AUTO_INCREMENT;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `POMELO_BEFORE_DROP_PRIMARY_KEY` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `POMELO_BEFORE_DROP_PRIMARY_KEY`(IN `SCHEMA_NAME_ARGUMENT` VARCHAR(255), IN `TABLE_NAME_ARGUMENT` VARCHAR(255))
BEGIN
	DECLARE HAS_AUTO_INCREMENT_ID TINYINT(1);
	DECLARE PRIMARY_KEY_COLUMN_NAME VARCHAR(255);
	DECLARE PRIMARY_KEY_TYPE VARCHAR(255);
	DECLARE SQL_EXP VARCHAR(1000);
	SELECT COUNT(*)
		INTO HAS_AUTO_INCREMENT_ID
		FROM `information_schema`.`COLUMNS`
		WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
			AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
			AND `Extra` = 'auto_increment'
			AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
	IF HAS_AUTO_INCREMENT_ID THEN
		SELECT `COLUMN_TYPE`
			INTO PRIMARY_KEY_TYPE
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SELECT `COLUMN_NAME`
			INTO PRIMARY_KEY_COLUMN_NAME
			FROM `information_schema`.`COLUMNS`
			WHERE `TABLE_SCHEMA` = (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA()))
				AND `TABLE_NAME` = TABLE_NAME_ARGUMENT
				AND `COLUMN_KEY` = 'PRI'
			LIMIT 1;
		SET SQL_EXP = CONCAT('ALTER TABLE `', (SELECT IFNULL(SCHEMA_NAME_ARGUMENT, SCHEMA())), '`.`', TABLE_NAME_ARGUMENT, '` MODIFY COLUMN `', PRIMARY_KEY_COLUMN_NAME, '` ', PRIMARY_KEY_TYPE, ' NOT NULL;');
		SET @SQL_EXP = SQL_EXP;
		PREPARE SQL_EXP_EXECUTE FROM @SQL_EXP;
		EXECUTE SQL_EXP_EXECUTE;
		DEALLOCATE PREPARE SQL_EXP_EXECUTE;
	END IF;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-06-23 10:02:27
