# MySQL Database Setup Guide for Insurance Claims API

This guide will help you and your team set up the MySQL database for the Insurance Claims API project.

## 🚀 Quick Start (Recommended for Team)

### Prerequisites
- Docker Desktop installed ([Download Docker](https://www.docker.com/products/docker-desktop))
- .NET 9.0 SDK installed
- Git

### 1. Clone and Setup

```bash
# Clone the repository
git clone <your-repo-url>
cd InsuranceClaimsAPI

# Create .env file from example
cp .env.example .env

# Edit .env file and update passwords
# IMPORTANT: Change the default passwords!
```

### 2. Start MySQL Database with Docker

```bash
# Start MySQL and phpMyAdmin
docker-compose up -d

# Check if containers are running
docker-compose ps

# View logs
docker-compose logs -f mysql
```

**Services Started:**
- MySQL Server: `localhost:3306`
- phpMyAdmin (Web Interface): `http://localhost:8080`

### 3. Update Application Settings

Update `appsettings.json` or `appsettings.Development.json` with your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=insurance_user;Pwd=user_password_change_me;Port=3306;CharSet=utf8mb4;"
  }
}
```

**Note:** Use the same password you set in the `.env` file.

### 4. Apply Database Migrations

```bash
# Navigate to the project directory
cd InsuranceClaimsAPI

# Create initial migration (if not exists)
dotnet ef migrations add InitialCreate

# Update database with migrations
dotnet ef database update

# OR simply run the application (auto-creates database)
dotnet run
```

### 5. Verify Database

**Option 1: Using phpMyAdmin**
- Open browser: `http://localhost:8080`
- Login with username: `insurance_user` and your password
- Check if `InsuranceClaimsDB` database exists with all tables

**Option 2: Using MySQL CLI**
```bash
# Access MySQL container
docker exec -it insurance-claims-mysql mysql -u insurance_user -p

# Enter your password, then run:
USE InsuranceClaimsDB;
SHOW TABLES;
```

---

## 🔧 Alternative Setup Methods

### Option 1: Local MySQL Installation

If you prefer not to use Docker:

1. Download MySQL from [mysql.com](https://www.mysql.com/)
2. Install MySQL Server 8.0+
3. Create database:
   ```sql
   CREATE DATABASE InsuranceClaimsDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER 'insurance_user'@'localhost' IDENTIFIED BY 'your_password';
   GRANT ALL PRIVILEGES ON InsuranceClaimsDB.* TO 'insurance_user'@'localhost';
   FLUSH PRIVILEGES;
   ```
4. Update connection string in `appsettings.json`
5. Run migrations

### Option 2: Cloud MySQL (Production)

For production deployment, consider:
- **AWS RDS for MySQL**
- **Azure Database for MySQL**
- **Google Cloud SQL for MySQL**
- **Oracle MySQL HeatWave**

Update your connection string accordingly for cloud deployment.

---

## 📊 Database Schema

The application uses Entity Framework Core with the following entities:

- **Users** - System users (Service Providers, Insurance Agents, Admins)
- **Claims** - Insurance claims
- **Quotes** - Service provider quotes for claims
- **Messages** - Communication between users
- **Notifications** - System notifications
- **ClaimDocuments** - Documents attached to claims
- **QuoteDocuments** - Documents attached to quotes
- **AuditLogs** - Audit trail for all actions

---

## 🛠️ Useful Commands

### Docker Commands

```bash
# Start database
docker-compose up -d

# Stop database
docker-compose down

# Stop and remove volumes (CAUTION: This deletes all data)
docker-compose down -v

# View logs
docker-compose logs -f mysql

# Restart database
docker-compose restart mysql

# Backup database
docker exec insurance-claims-mysql mysqldump -u root -proot_password_change_me InsuranceClaimsDB > backup.sql

# Restore database
docker exec -i insurance-claims-mysql mysql -u root -proot_password_change_me InsuranceClaimsDB < backup.sql
```

### Entity Framework Commands

```bash
# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Drop database
dotnet ef database drop
```

---

## 🔐 Security Best Practices

1. **Never commit `.env` file** - It's already in `.gitignore`
2. **Change default passwords** - Always use strong passwords
3. **Use different passwords for dev/prod** - Never reuse production passwords
4. **Rotate passwords regularly** - Especially in production
5. **Use SSL/TLS** - For production connections, enable SSL
6. **Limit database user privileges** - Only grant necessary permissions
7. **Keep JWT secret secure** - Store in environment variables or Azure Key Vault

---

## 🐛 Troubleshooting

### Connection Issues

**Error: "Unable to connect to MySQL server"**
```bash
# Check if MySQL is running
docker-compose ps

# Check MySQL logs
docker-compose logs mysql

# Test connection
docker exec -it insurance-claims-mysql mysql -u root -p
```

**Error: "Access denied for user"**
- Verify username and password in connection string
- Check `.env` file settings
- Ensure database user has proper privileges

**Error: "Can't connect to MySQL server on 'localhost' (10061)"**
- Ensure Docker is running
- Check if port 3306 is not being used by another service
- Try `docker-compose restart mysql`

### Migration Issues

**Error: "A connection was successfully established... but then an error occurred"**
- Check if database server is running
- Verify connection string
- Check firewall settings

**Error: "The database does not exist"**
- Run `docker-compose up -d` to create database
- Or manually create database using phpMyAdmin

---

## 👥 Team Collaboration

### For New Team Members

1. **Get the code:**
   ```bash
   git clone <repo-url>
   cd InsuranceClaimsAPI
   ```

2. **Setup environment:**
   ```bash
   cp .env.example .env
   # Ask team lead for development passwords
   ```

3. **Start database:**
   ```bash
   docker-compose up -d
   ```

4. **Run application:**
   ```bash
   cd InsuranceClaimsAPI
   dotnet restore
   dotnet run
   ```

### Shared Development Database

Option 1: **Local Docker** (Recommended)
- Each developer runs their own MySQL instance
- Easy to reset and test
- No conflicts between developers

Option 2: **Shared Dev Server**
- Host MySQL on a shared server
- Update connection string to point to server IP
- Coordinate schema changes

---

## 📚 Additional Resources

- [MySQL Official Documentation](https://dev.mysql.com/doc/)
- [MySQL Workbench](https://www.mysql.com/products/workbench/) - GUI for MySQL
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Docker Documentation](https://docs.docker.com/)
- [Pomelo EF Core Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)

---

## 📞 Support

If you encounter issues:
1. Check this documentation
2. Review error logs: `docker-compose logs mysql`
3. Check application logs: `logs/insurance-claims-*.txt`
4. Contact team lead or DevOps

---

**Happy Coding! 🚀**

