-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: trivia_game
-- ------------------------------------------------------
-- Server version	8.0.44

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
-- Table structure for table `player_answers`
--

DROP TABLE IF EXISTS `player_answers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `player_answers` (
  `player_answer_id` int NOT NULL AUTO_INCREMENT,
  `room_id` int NOT NULL,
  `room_player_id` int NOT NULL,
  `question_id` int NOT NULL,
  `selected_option_id` int DEFAULT NULL,
  `answer_text` text,
  `is_correct` tinyint(1) NOT NULL DEFAULT '0',
  `answered_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`player_answer_id`),
  KEY `fk_panswers_question` (`question_id`),
  KEY `fk_panswers_option` (`selected_option_id`),
  KEY `ix_panswers_room_question` (`room_id`,`question_id`),
  KEY `ix_panswers_rplayer` (`room_player_id`),
  CONSTRAINT `fk_panswers_option` FOREIGN KEY (`selected_option_id`) REFERENCES `question_options` (`option_id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_panswers_question` FOREIGN KEY (`question_id`) REFERENCES `questions` (`question_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_panswers_room` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`room_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_panswers_rplayer` FOREIGN KEY (`room_player_id`) REFERENCES `room_players` (`room_player_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `player_answers`
--

LOCK TABLES `player_answers` WRITE;
/*!40000 ALTER TABLE `player_answers` DISABLE KEYS */;
/*!40000 ALTER TABLE `player_answers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `question_options`
--

DROP TABLE IF EXISTS `question_options`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `question_options` (
  `option_id` int NOT NULL AUTO_INCREMENT,
  `question_id` int NOT NULL,
  `option_text` varchar(255) NOT NULL,
  `is_correct` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`option_id`),
  KEY `ix_qoptions_qid` (`question_id`),
  KEY `ix_qoptions_qid_correct` (`question_id`,`is_correct`),
  CONSTRAINT `fk_qoptions_question` FOREIGN KEY (`question_id`) REFERENCES `questions` (`question_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `question_options`
--

LOCK TABLES `question_options` WRITE;
/*!40000 ALTER TABLE `question_options` DISABLE KEYS */;
/*!40000 ALTER TABLE `question_options` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `question_types`
--

DROP TABLE IF EXISTS `question_types`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `question_types` (
  `question_type_id` int NOT NULL AUTO_INCREMENT,
  `type_name` varchar(50) NOT NULL,
  PRIMARY KEY (`question_type_id`),
  UNIQUE KEY `uq_question_types_type` (`type_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `question_types`
--

LOCK TABLES `question_types` WRITE;
/*!40000 ALTER TABLE `question_types` DISABLE KEYS */;
/*!40000 ALTER TABLE `question_types` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `questions`
--

DROP TABLE IF EXISTS `questions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `questions` (
  `question_id` int NOT NULL AUTO_INCREMENT,
  `question_text` text NOT NULL,
  `question_type_id` int NOT NULL,
  `difficulty` varchar(20) NOT NULL DEFAULT 'easy',
  `created_by` int NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`question_id`),
  KEY `ix_questions_creator` (`created_by`),
  KEY `ix_questions_type` (`question_type_id`),
  CONSTRAINT `fk_questions_creator` FOREIGN KEY (`created_by`) REFERENCES `users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_questions_type` FOREIGN KEY (`question_type_id`) REFERENCES `question_types` (`question_type_id`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `questions`
--

LOCK TABLES `questions` WRITE;
/*!40000 ALTER TABLE `questions` DISABLE KEYS */;
/*!40000 ALTER TABLE `questions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `room_players`
--

DROP TABLE IF EXISTS `room_players`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `room_players` (
  `room_player_id` int NOT NULL AUTO_INCREMENT,
  `room_id` int NOT NULL,
  `user_id` int NOT NULL,
  `nickname` varchar(50) NOT NULL,
  `joined_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`room_player_id`),
  UNIQUE KEY `uq_room_players` (`room_id`,`user_id`),
  KEY `fk_room_players_user` (`user_id`),
  CONSTRAINT `fk_room_players_room` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`room_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_room_players_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `room_players`
--

LOCK TABLES `room_players` WRITE;
/*!40000 ALTER TABLE `room_players` DISABLE KEYS */;
/*!40000 ALTER TABLE `room_players` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `room_questions`
--

DROP TABLE IF EXISTS `room_questions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `room_questions` (
  `room_question_id` int NOT NULL AUTO_INCREMENT,
  `room_id` int NOT NULL,
  `question_id` int NOT NULL,
  `question_order` int NOT NULL,
  `time_limit_sec` int NOT NULL DEFAULT '15',
  `started_at` datetime DEFAULT NULL,
  PRIMARY KEY (`room_question_id`),
  UNIQUE KEY `uq_room_question` (`room_id`,`question_id`),
  KEY `fk_room_questions_question` (`question_id`),
  KEY `ix_room_questions_order` (`room_id`,`question_order`),
  CONSTRAINT `fk_room_questions_question` FOREIGN KEY (`question_id`) REFERENCES `questions` (`question_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_room_questions_room` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`room_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `room_questions`
--

LOCK TABLES `room_questions` WRITE;
/*!40000 ALTER TABLE `room_questions` DISABLE KEYS */;
/*!40000 ALTER TABLE `room_questions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `game_results`
--

DROP TABLE IF EXISTS `game_results`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `game_results` (
  `game_result_id` int NOT NULL AUTO_INCREMENT,
  `room_id` int NULL,
  `user_id` int NOT NULL,
  `correct_count` int NOT NULL,
  `answered_count` int NOT NULL,
  `is_winner` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`game_result_id`),
  UNIQUE KEY `uq_game_results_room_user` (`room_id`,`user_id`),
  KEY `ix_game_results_user` (`user_id`),
  CONSTRAINT `fk_game_results_room` FOREIGN KEY (`room_id`) REFERENCES `rooms` (`room_id`) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `fk_game_results_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `game_results`
--

LOCK TABLES `game_results` WRITE;
/*!40000 ALTER TABLE `game_results` DISABLE KEYS */;
/*!40000 ALTER TABLE `game_results` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rooms`
--

DROP TABLE IF EXISTS `rooms`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rooms` (
  `room_id` int NOT NULL AUTO_INCREMENT,
  `room_code` varchar(10) NOT NULL,
  `room_name` varchar(100) NOT NULL,
  `host_id` int NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `is_public` tinyint(1) NOT NULL DEFAULT '0',
  `question_type_id` int DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`room_id`),
  UNIQUE KEY `uq_rooms_code` (`room_code`),
  KEY `fk_rooms_host` (`host_id`),
  KEY `fk_rooms_question_type` (`question_type_id`),
  CONSTRAINT `fk_rooms_host` FOREIGN KEY (`host_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_rooms_question_type` FOREIGN KEY (`question_type_id`) REFERENCES `question_types` (`question_type_id`) ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rooms`
--

LOCK TABLES `rooms` WRITE;
/*!40000 ALTER TABLE `rooms` DISABLE KEYS */;
/*!40000 ALTER TABLE `rooms` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `full_name` varchar(100) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` char(64) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `uq_users_username` (`username`),
  UNIQUE KEY `uq_users_email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','Admin','admin@example.com','240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','2025-11-13 00:02:48'),(2,'123','123','123','a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3','2025-11-16 09:00:13'),(3,'KJBK',',KN','1231','96cae35ce8a9b0244178bf28e4966c2ce1b8385723a96a6b838858cdd6ca0a1e','2025-11-16 09:01:09'),(4,'S','S','S','8de0b3c47f112c59745f717a626932264c422a7563954872e237b223af4ad643','2025-12-25 11:57:40'),(15,'yosi','yooosi','yos','ee79976c9380d5e337fc1c095ece8c8f22f91f306ceeb161fa51fecede2c4ba1','2025-12-25 11:58:13'),(16,'12','122','1222','0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c','2025-12-25 12:02:01'),(17,'1','111','11','f6e0a1e2ac41945a9aa7ff8a8aaa0cebc12a3bcc981a929ad5cf810a090e11ae','2025-12-25 12:02:30'),(19,'yoseff','yos','yosimmm','f6e0a1e2ac41945a9aa7ff8a8aaa0cebc12a3bcc981a929ad5cf810a090e11ae','2025-12-25 12:08:22'),(20,'pop','popop','mish','03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4','2025-12-26 01:08:42');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-12-31 10:42:43
