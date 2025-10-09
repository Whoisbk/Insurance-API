# 🚀 Quick Start Guide - Insurance Claims API

Get up and running in 5 minutes!

## Prerequisites
- Docker Desktop installed and running
- .NET 9.0 SDK installed
- Git

## Setup Steps

### 1️⃣ Get the Environment File
```bash
# Copy the environment template
cp env.example .env

# Edit .env and change the passwords
# Use a text editor or:
nano .env
```

⚠️ **IMPORTANT:** Change all passwords in `.env` file before starting!

### 2️⃣ Start MySQL Database
```bash
# Start MySQL and phpMyAdmin
docker-compose up -d

# Verify containers are running
docker-compose ps
```

You should see:
- ✅ `insurance-claims-mysql` - Running on port 3306
- ✅ `insurance-claims-phpmyadmin` - Running on port 8080

### 3️⃣ Update Connection String

Edit `InsuranceClaimsAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=insurance_user;Pwd=YOUR_PASSWORD_FROM_ENV;Port=3306;CharSet=utf8mb4;"
  }
}
```

Replace `YOUR_PASSWORD_FROM_ENV` with the password you set in `.env` file.

### 4️⃣ Run the Application
```bash
cd InsuranceClaimsAPI
dotnet restore
dotnet run
```

The application will:
- ✅ Create the database automatically
- ✅ Apply all migrations
- ✅ Seed initial data
- ✅ Start the API server

### 5️⃣ Verify Everything Works

**Access phpMyAdmin:**
- Open: http://localhost:8080
- Username: `insurance_user`
- Password: (from your .env file)
- Check if tables are created

**Test the API:**
- Open: https://localhost:5001/health (or check the port from console)
- You should see a healthy status

## 🎉 You're Done!

The database is now running and ready for development.

## Useful URLs
- **API**: https://localhost:5001 (check console for actual port)
- **Swagger**: https://localhost:5001/swagger
- **phpMyAdmin**: http://localhost:8080
- **Health Check**: https://localhost:5001/health

## Common Commands

```bash
# Stop database
docker-compose down

# Start database
docker-compose up -d

# View logs
docker-compose logs -f

# Backup database
./scripts/backup-database.sh

# Access MySQL CLI
docker exec -it insurance-claims-mysql mysql -u insurance_user -p
```

## Need Help?
See [DATABASE_SETUP.md](DATABASE_SETUP.md) for detailed documentation.

---

**Happy Coding! 🚀**

