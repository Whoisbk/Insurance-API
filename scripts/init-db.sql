-- MySQL Initialization Script for Insurance Claims API
-- This script runs automatically when the Docker container is first created

-- Create database if not exists
CREATE DATABASE IF NOT EXISTS InsuranceClaimsDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE InsuranceClaimsDB;

-- Grant privileges
GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user'@'%';
FLUSH PRIVILEGES;

-- Display database info
SELECT 'Database InsuranceClaimsDB created successfully!' AS message;
SHOW DATABASES;

