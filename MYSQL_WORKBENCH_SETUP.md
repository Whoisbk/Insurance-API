# MySQL Workbench Setup Guide 🛠️

Complete guide for setting up the Insurance Claims API database using MySQL Workbench.

## Prerequisites

✅ MySQL Server installed (comes with MySQL Workbench)  
✅ MySQL Workbench installed  
✅ .NET 9.0 SDK installed

## 📝 Step-by-Step Setup

### Step 1: Open MySQL Workbench

1. Launch **MySQL Workbench**
2. Connect to your local MySQL instance
   - Usually: `localhost:3306`
   - Use your MySQL root password

### Step 2: Create the Database

**Option A: Using the SQL Script (Recommended)**

1. In MySQL Workbench, click **File** → **Open SQL Script**
2. Navigate to: `scripts/create-database-workbench.sql`
3. **IMPORTANT**: Edit line 13 and replace `your_password_here` with a strong password:
   ```sql
   IDENTIFIED BY 'YourStrongPassword123!';
   ```
4. Click the ⚡ **Execute** button (or press `Ctrl+Shift+Enter`)
5. Check the output panel - you should see "Database InsuranceClaimsDB created successfully!"

**Option B: Manual Creation (Alternative)**

Run these commands one by one in the Query tab:

```sql
-- Create database
CREATE DATABASE IF NOT EXISTS InsuranceClaimsDB
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user
CREATE USER IF NOT EXISTS 'insurance_user'@'localhost'
IDENTIFIED BY 'YourStrongPassword123!';

-- Grant permissions
GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user'@'localhost';
FLUSH PRIVILEGES;
```

### Step 3: Verify Database Creation

In MySQL Workbench:

1. Look at the **Schemas** panel on the left
2. Click the 🔄 refresh icon
3. You should see **InsuranceClaimsDB** in the list
4. Right-click on it → **Set as Default Schema**

### Step 4: Update Connection String

Choose ONE of these options:

**Option A: Using the new dedicated user (Recommended)**

Edit `InsuranceClaimsAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=insurance_user;Pwd=YourStrongPassword123!;Port=3306;CharSet=utf8mb4;"
  }
}
```

**Option B: Using root user (Simpler)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YourRootPassword;Port=3306;CharSet=utf8mb4;"
  }
}
```

⚠️ **Replace the password with YOUR actual password!**

### Step 5: Run the Application

```bash
cd InsuranceClaimsAPI
dotnet restore
dotnet run
```

The application will:

- ✅ Connect to your MySQL database
- ✅ Automatically create all tables using Entity Framework
- ✅ Seed initial data
- ✅ Start the API server

### Step 6: Verify Tables Were Created

Back in MySQL Workbench:

1. Refresh the **InsuranceClaimsDB** schema
2. Expand **Tables** - you should see:
   - `AuditLogs`
   - `ClaimDocuments`
   - `Claims`
   - `Messages`
   - `Notifications`
   - `QuoteDocuments`
   - `Quotes`
   - `Users`
   - `__EFMigrationsHistory`

🎉 **Success!** Your database is ready!

---

## 👥 Team Setup

### For Team Members

**Share this with your team:**

1. **Install MySQL** (if not already installed)

   - Download from: https://dev.mysql.com/downloads/mysql/
   - Or use XAMPP/MAMP which includes MySQL

2. **Get the connection details from team lead:**

   - Database name: `InsuranceClaimsDB`
   - Username: `insurance_user` (or `root`)
   - Password: [team lead provides this]
   - Port: `3306` (default)

3. **Update your local `appsettings.Development.json`** with the connection string

4. **Run the application:**
   ```bash
   cd InsuranceClaimsAPI
   dotnet run
   ```

### For Shared Development Database

If your team wants to share one database server:

1. **Host MySQL on a server** (could be one team member's computer)
2. **Update connection string** to use the server IP instead of `localhost`:
   ```
   Server=192.168.1.100;Database=InsuranceClaimsDB;...
   ```
3. **Configure MySQL to allow remote connections:**
   ```sql
   CREATE USER 'insurance_user'@'%' IDENTIFIED BY 'password';
   GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user'@'%';
   FLUSH PRIVILEGES;
   ```
4. **Update MySQL config** to bind to 0.0.0.0 (not just localhost)
5. **Configure firewall** to allow port 3306

---

## 🗄️ Database Management with MySQL Workbench

### View Data

1. In MySQL Workbench, expand your database
2. Right-click on a table → **Select Rows - Limit 1000**
3. View, edit, and manage data

### Run Queries

```sql
-- View all users
USE InsuranceClaimsDB;
SELECT * FROM Users;

-- View all claims
SELECT * FROM Claims;

-- View claims with user details
SELECT c.*, u.Email, u.FullName
FROM Claims c
JOIN Users u ON c.ServiceProviderId = u.UserId;
```

### Backup Database

1. In MySQL Workbench: **Server** → **Data Export**
2. Select **InsuranceClaimsDB**
3. Choose export location
4. Click **Start Export**

### Restore Database

1. **Server** → **Data Import**
2. Select your backup file
3. Click **Start Import**

---

## 🛠️ Useful MySQL Workbench Features

### Forward Engineer (View Schema)

1. **Database** → **Forward Engineer**
2. Follow wizard to generate SQL schema
3. Great for viewing database structure

### Reverse Engineer (From Existing DB)

1. **Database** → **Reverse Engineer**
2. Creates an EER Diagram of your database
3. Visual representation of tables and relationships

### EER Diagram

1. **Database** → **Reverse Engineer**
2. Complete the wizard
3. View visual diagram of database structure

---

## 🔐 Security Best Practices

### For Development

✅ Use a dedicated database user (not root)  
✅ Use strong passwords  
✅ Keep passwords out of git (use appsettings.Development.json)  
✅ Each developer has their own local database

### For Production

✅ Use environment variables for connection strings  
✅ Enable SSL/TLS connections  
✅ Use strong passwords and rotate regularly  
✅ Limit user privileges (only necessary permissions)  
✅ Regular backups  
✅ Monitor database logs

---

## 🐛 Troubleshooting

### "Access denied for user"

**Problem:** Wrong username or password

**Solution:**

1. Check your MySQL root password
2. Try resetting the user password:
   ```sql
   ALTER USER 'insurance_user'@'localhost' IDENTIFIED BY 'NewPassword123!';
   FLUSH PRIVILEGES;
   ```

### "Unknown database 'InsuranceClaimsDB'"

**Problem:** Database wasn't created

**Solution:**

1. Run the create-database-workbench.sql script again
2. Or manually create: `CREATE DATABASE InsuranceClaimsDB;`

### "Can't connect to MySQL server on 'localhost'"

**Problem:** MySQL server is not running

**Solution:**

- **Windows**: Start MySQL service from Services
- **Mac**: System Preferences → MySQL → Start MySQL Server
- **Linux**: `sudo systemctl start mysql`

### "Table 'InsuranceClaimsDB.Users' doesn't exist"

**Problem:** Entity Framework hasn't created tables yet

**Solution:**

1. Make sure your connection string is correct
2. Run the application - EF creates tables automatically
3. Or run: `dotnet ef database update`

### Connection String Errors

Make sure your connection string format is correct:

```
Server=localhost;Database=InsuranceClaimsDB;Uid=username;Pwd=password;Port=3306;CharSet=utf8mb4;
```

Common issues:

- Missing semicolons between parameters
- Wrong parameter names (Uid not User, Pwd not Password)
- Spaces in password (use quotes if needed)

---

## 📊 Monitoring and Performance

### Check Database Size

```sql
SELECT
    table_schema AS 'Database',
    ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS 'Size (MB)'
FROM information_schema.tables
WHERE table_schema = 'InsuranceClaimsDB'
GROUP BY table_schema;
```

### Check Table Sizes

```sql
SELECT
    table_name AS 'Table',
    ROUND((data_length + index_length) / 1024 / 1024, 2) AS 'Size (MB)'
FROM information_schema.tables
WHERE table_schema = 'InsuranceClaimsDB'
ORDER BY (data_length + index_length) DESC;
```

### View Active Connections

```sql
SHOW PROCESSLIST;
```

---

## 📚 Additional Resources

- [MySQL Workbench Documentation](https://dev.mysql.com/doc/workbench/en/)
- [MySQL 8.0 Reference Manual](https://dev.mysql.com/doc/refman/8.0/en/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)

---

## 💡 Tips for Team

1. **Use consistent connection settings** - Everyone should use the same port (3306)
2. **Share database scripts** - Keep all SQL scripts in the `scripts/` folder
3. **Document changes** - If you modify the database schema, document it
4. **Test migrations** - Always test EF migrations before sharing
5. **Backup regularly** - Especially before major changes

---

**Need Help?** Check the troubleshooting section or contact your team lead!

**Happy Developing! 🚀**
