# 🚀 MySQL Workbench Quick Start (5 Minutes)

Get your database running in 5 minutes using MySQL Workbench!

## ✅ Prerequisites Check

- [ ] MySQL Server installed
- [ ] MySQL Workbench installed
- [ ] .NET 9.0 SDK installed

## 📝 Setup Steps

### 1️⃣ Open MySQL Workbench

- Launch MySQL Workbench
- Connect to your local MySQL instance (usually `localhost`)
- Enter your MySQL root password

### 2️⃣ Create Database (Choose One Method)

**Method A: Run the SQL Script** ⭐ Recommended

1. Click **File** → **Open SQL Script**
2. Select: `scripts/create-database-workbench.sql`
3. **EDIT LINE 13**: Change `your_password_here` to a real password
   ```sql
   IDENTIFIED BY 'MyStrongPass123!';  -- Change this!
   ```
4. Click ⚡ **Execute** button
5. Should see: "Database InsuranceClaimsDB created successfully!"

**Method B: Copy-Paste This** (Quick & Easy)

```sql
-- Create database
CREATE DATABASE IF NOT EXISTS InsuranceClaimsDB
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Option 1: Use root user (simpler)
-- Just use your existing root password in the connection string

-- Option 2: Create dedicated user (more secure)
CREATE USER IF NOT EXISTS 'insurance_user'@'localhost'
IDENTIFIED BY 'ChangeThisPassword123!';

GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user'@'localhost';
FLUSH PRIVILEGES;
```

### 3️⃣ Update Connection String

Edit: `InsuranceClaimsAPI/appsettings.Development.json`

**If using root user:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YOUR_ROOT_PASSWORD;Port=3306;CharSet=utf8mb4;"
  }
}
```

**If using dedicated user:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=insurance_user;Pwd=ChangeThisPassword123!;Port=3306;CharSet=utf8mb4;"
  }
}
```

⚠️ **Replace with YOUR actual password!**

### 4️⃣ Run Your API

```bash
cd InsuranceClaimsAPI
dotnet run
```

The app will automatically:

- ✅ Connect to MySQL
- ✅ Create all tables
- ✅ Add sample data
- ✅ Start the API

### 5️⃣ Verify in Workbench

1. In MySQL Workbench, click 🔄 refresh in the Schemas panel
2. Expand **InsuranceClaimsDB**
3. Expand **Tables** - you should see 8-9 tables created!

## 🎉 Done!

Your database is ready. API should be running at https://localhost:5001

## 🔍 Quick Checks

**View data in Workbench:**

```sql
USE InsuranceClaimsDB;
SELECT * FROM Users;
SELECT * FROM Claims;
```

**Test API:**

- Health check: https://localhost:5001/health
- Swagger: https://localhost:5001/swagger

## 👥 For Your Team

Share these details with team members:

**Database Info:**

- Database Name: `InsuranceClaimsDB`
- Host: `localhost` (or your computer's IP if sharing)
- Port: `3306`
- Username: `insurance_user` (or `root`)
- Password: [share securely, not in git!]

**Setup for team:**

1. Install MySQL Server & Workbench
2. Get connection details from you
3. Update their `appsettings.Development.json`
4. Run `dotnet run`

## 🆘 Quick Troubleshooting

**Can't connect?**

- Check MySQL is running
- Verify password is correct
- Try port 3306

**Database not created?**

- Run the SQL script again
- Check for error messages

**Tables not showing?**

- Run the .NET application first
- Refresh Workbench

## 📚 Full Documentation

For detailed setup and troubleshooting:

- **[MYSQL_WORKBENCH_SETUP.md](MYSQL_WORKBENCH_SETUP.md)** - Complete guide
- **[DATABASE_SETUP.md](DATABASE_SETUP.md)** - Docker alternative

---

**Need help?** See [MYSQL_WORKBENCH_SETUP.md](MYSQL_WORKBENCH_SETUP.md) for detailed instructions!
