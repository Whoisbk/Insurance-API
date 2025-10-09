-- =====================================================
-- MySQL Database Setup Script for Insurance Claims API
-- Run this script in MySQL Workbench
-- =====================================================

-- Step 1: Create the database
CREATE DATABASE IF NOT EXISTS InsuranceClaimsDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Step 2: Create a dedicated user for the application (recommended)
-- Replace 'your_password_here' with a strong password
CREATE USER IF NOT EXISTS 'insurance_user' @'localhost' IDENTIFIED BY 'your_password_here';

-- Step 3: Grant all privileges on the database to the user
GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user' @'localhost';

-- Step 4: Apply the changes
FLUSH PRIVILEGES;

-- Step 5: Verify the database was created
USE InsuranceClaimsDB;

SELECT 'Database InsuranceClaimsDB created successfully!' AS Status;

-- Display all databases to verify
SHOW DATABASES LIKE 'InsuranceClaimsDB';

-- Display user privileges
SHOW GRANTS FOR 'insurance_user' @'localhost';

-- =====================================================
-- ALTERNATIVE: Use root user (simpler but less secure)
-- =====================================================
-- If you prefer to use the root user instead of creating a new user,
-- comment out Steps 2-3 above and just create the database (Step 1).
-- Then use root credentials in your connection string.
-- =====================================================

-- =====================================================
-- NEXT STEPS:
-- 1. Update the connection string in appsettings.Development.json
-- 2. Run your ASP.NET Core application
-- 3. Entity Framework will create all tables automatically
-- =====================================================