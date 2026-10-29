-- Kør manuelt i MySQL Workbench eller MySQL-klienten før første opstart.
-- Eksisterende databaser og tabeller slettes eller ændres ikke.
CREATE DATABASE IF NOT EXISTS webapp
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE webapp;

CREATE TABLE IF NOT EXISTS users (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(254) NOT NULL,
    password_hash VARCHAR(512) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    UNIQUE KEY uq_users_username (username),
    UNIQUE KEY uq_users_email (email)
) ENGINE=InnoDB DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
