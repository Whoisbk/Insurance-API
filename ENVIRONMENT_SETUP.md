# 🌍 Environment Setup Guide: Development + Production

This guide explains how to configure the Insurance Claims API for both **local development** and **production deployment**.

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Local Development Setup](#local-development-setup)
3. [Production Setup](#production-setup)
4. [Configuration Files Explained](#configuration-files-explained)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

### Environment Strategy

| Environment        | Database Location                       | Purpose                        | Configuration File             |
| ------------------ | --------------------------------------- | ------------------------------ | ------------------------------ |
| 🧑‍💻 **Development** | Local MySQL (localhost:3306)            | Personal testing & development | `appsettings.Development.json` |
| 🌍 **Production**  | Remote MySQL (Railway/PlanetScale/etc.) | Live shared data               | `appsettings.Production.json`  |

✅ **Benefits of this approach:**

- Team members can work offline with their own local databases
- No interference between developers' test data
- Single production database for the live API
- Easy to reset/test locally without affecting production

---

## 🧑‍💻 Local Development Setup

### 1️⃣ Install MySQL Locally

Each team member needs their own local MySQL installation:

**Option A: MySQL Workbench (Recommended)**

- Download from: https://dev.mysql.com/downloads/workbench/
- Follow the installation wizard
- Set a root password during installation

**Option B: Homebrew (macOS)**

```bash
brew install mysql
brew services start mysql
mysql_secure_installation
```

**Option C: Docker**

```bash
docker run --name mysql-local -e MYSQL_ROOT_PASSWORD=your_password -p 3306:3306 -d mysql:8.0
```

### 2️⃣ Create the Database

**Using MySQL Workbench:**

1. Open MySQL Workbench
2. Connect to your local MySQL instance
3. Run the `scripts/init-db.sql` script
4. Or let the API create it automatically on first run

**Using Terminal:**

```bash
mysql -u root -p < scripts/init-db.sql
```

### 3️⃣ Configure Your Local Environment

**Option A: Using appsettings.Development.json (Current Method)**

Edit `InsuranceClaimsAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YOUR_LOCAL_PASSWORD;Port=3306;CharSet=utf8mb4;"
  }
}
```

> ⚠️ **Note:** `appsettings.Development.json` is tracked in Git. Don't commit your actual passwords!

**Option B: Using User Secrets (Recommended for Passwords)**

This keeps your passwords out of Git entirely:

```bash
cd InsuranceClaimsAPI

# Set your connection string securely
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YOUR_LOCAL_PASSWORD;Port=3306;CharSet=utf8mb4;"

# Set JWT secret
dotnet user-secrets set "JwtSettings:Secret" "YourDevelopmentJwtSecretKey_AtLeast32Characters"
```

### 4️⃣ Run the API in Development Mode

```bash
cd InsuranceClaimsAPI
dotnet run --environment "Development"
```

Or press `F5` in Visual Studio / Rider (it will use Development by default).

✅ **The API will:**

- Use your local MySQL database
- Automatically create tables if they don't exist
- Seed initial test data
- Enable detailed logging

---

## 🌍 Production Setup

### 1️⃣ Choose a MySQL Hosting Provider

Pick one of these free/affordable options:

#### 🟣 Railway (Recommended)

- Free tier with 500 hours/month
- Easy MySQL plugin
- Great for .NET deployments
- **Setup:** https://railway.app

#### 🟢 Clever Cloud

- Free MySQL tier (256MB)
- EU-based servers
- **Setup:** https://www.clever-cloud.com

#### 🟠 PlanetScale

- 5GB free storage
- Excellent performance
- Branch-based workflows
- **Setup:** https://planetscale.com

#### 🔵 Aiven.io

- Free trial available
- Multiple cloud providers
- **Setup:** https://aiven.io

### 2️⃣ Create Remote MySQL Database

After signing up with your chosen provider, you'll receive connection details like:

```
Host: myapp-production-db.railway.app
Port: 3306
Database: InsuranceClaimsDB
Username: mysql
Password: ********
```

### 3️⃣ Configure Production Settings

**Update `InsuranceClaimsAPI/appsettings.Production.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_PRODUCTION_HOST;Database=InsuranceClaimsDB;Uid=YOUR_PRODUCTION_USER;Pwd=YOUR_PRODUCTION_PASSWORD;Port=3306;CharSet=utf8mb4;SslMode=Required;"
  },
  "JwtSettings": {
    "Secret": "YOUR_PRODUCTION_JWT_SECRET_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG"
  }
}
```

> ⚠️ **IMPORTANT:** Never commit `appsettings.Production.json` with real credentials!

**Better Option: Use Environment Variables**

Copy `env.production.example` to `.env.production`:

```bash
cp env.production.example .env.production
```

Edit `.env.production` with your actual production values:

```env
MYSQL_PRODUCTION_HOST=myapp-db.railway.app
MYSQL_PRODUCTION_PORT=3306
MYSQL_PRODUCTION_DATABASE=InsuranceClaimsDB
MYSQL_PRODUCTION_USER=mysql
MYSQL_PRODUCTION_PASSWORD=your_secure_password
JWT_SECRET=your_very_long_jwt_secret_key
```

### 4️⃣ Deploy to Production

When deploying to hosting platforms (Railway, Azure, Render, etc.):

**Set Environment Variable:**

```
ASPNETCORE_ENVIRONMENT=Production
```

**Set Connection String (via environment variables or secrets):**

```
ConnectionStrings__DefaultConnection=Server=host;Database=db;Uid=user;Pwd=pass;Port=3306;SslMode=Required;
```

**Example for Railway:**

1. Go to your project settings
2. Navigate to "Variables" tab
3. Add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `[your connection string]`
   - `JwtSettings__Secret` = `[your JWT secret]`

**Example for Azure App Service:**

```bash
az webapp config appsettings set --name MyApp --resource-group MyGroup --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  ConnectionStrings__DefaultConnection="Server=host;Database=db..." \
  JwtSettings__Secret="your_secret"
```

### 5️⃣ Initialize Production Database

**Option A: Let the API create tables automatically**

- On first run, the API will call `context.Database.EnsureCreated()`
- Tables will be created automatically

**Option B: Run migration script manually**

```bash
mysql -h your-production-host.railway.app -u mysql -p < scripts/init-db.sql
```

---

## 📁 Configuration Files Explained

### `appsettings.json` (Base Settings)

- **Purpose:** Shared settings used in all environments
- **Contains:** Logging, JWT issuer/audience (non-sensitive)
- **Git:** ✅ Committed

### `appsettings.Development.json` (Development Override)

- **Purpose:** Local development settings
- **Contains:** Local MySQL connection, development JWT secret
- **Git:** ✅ Committed (but use placeholders for passwords)
- **Priority:** Overrides `appsettings.json` in Development

### `appsettings.Production.json` (Production Override)

- **Purpose:** Production settings
- **Contains:** Production MySQL connection, production JWT secret
- **Git:** ⚠️ Committed with placeholders only
- **Priority:** Overrides `appsettings.json` in Production

### `.env` / `.env.production` (Environment Variables)

- **Purpose:** Store actual passwords and secrets
- **Git:** ❌ Never committed (already in `.gitignore`)
- **Security:** ✅ Best practice for sensitive data

### User Secrets (Development Only)

- **Purpose:** Store local development secrets securely
- **Location:** Outside project directory
- **Git:** ❌ Never committed
- **Command:** `dotnet user-secrets set "Key" "Value"`

---

## 🔐 Best Practices

### Security Checklist

✅ **DO:**

- Use strong, unique passwords for production
- Generate long JWT secrets (minimum 32 characters)
- Use `SslMode=Required` for production databases
- Keep `.env.production` out of Git
- Use environment variables on deployment platforms
- Rotate secrets regularly

❌ **DON'T:**

- Commit real passwords to Git
- Use the same passwords for dev and production
- Share production credentials in team chat
- Use weak or short JWT secrets

### Password Management

**Generate Strong Secrets:**

```bash
# Generate 32-character random string (for JWT secret)
openssl rand -base64 32

# Generate 64-character random string
openssl rand -base64 64
```

### Team Collaboration

**For Development:**

- Each team member sets their own local MySQL password
- Use `dotnet user-secrets` to avoid password conflicts in Git
- Share database schema via `scripts/init-db.sql`

**For Production:**

- Only team leads have production credentials
- Use deployment platform's secret management
- Document who has access to production

---

## 🐛 Troubleshooting

### Issue: "Unable to connect to MySQL server"

**Check:**

1. MySQL service is running: `mysql.server status` (macOS) or `sudo service mysql status` (Linux)
2. Port 3306 is not blocked by firewall
3. Username/password are correct
4. Database exists: `SHOW DATABASES;`

### Issue: "JWT token validation failed"

**Check:**

1. JWT secret matches between token generation and validation
2. Secret is at least 16 characters (32+ recommended)
3. Issuer and Audience match configuration

### Issue: "Tables don't exist"

**Solution:**

```bash
# Delete and recreate database
cd InsuranceClaimsAPI
dotnet run --environment "Development"
```

The API will automatically create tables on startup.

### Issue: "Connection works locally but not in production"

**Check:**

1. `SslMode=Required` is set in production connection string
2. Production MySQL allows connections from your hosting IP
3. Firewall rules on hosting provider allow MySQL port
4. Environment variable `ASPNETCORE_ENVIRONMENT=Production` is set

---

## 🚀 Quick Reference

### Run Locally

```bash
cd InsuranceClaimsAPI
dotnet run --environment "Development"
```

### Check Current Environment

```bash
echo $ASPNETCORE_ENVIRONMENT  # macOS/Linux
echo %ASPNETCORE_ENVIRONMENT%  # Windows
```

### Set Environment Temporarily

```bash
# macOS/Linux
export ASPNETCORE_ENVIRONMENT=Production
dotnet run

# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```

### Test Production Configuration Locally

```bash
# Update appsettings.Production.json with test values
dotnet run --environment "Production"
```

---

## 📞 Support

**Questions?**

- Check the MySQL Workbench setup guide: `MYSQL_WORKBENCH_SETUP.md`
- Review connection string examples: `env.example` and `env.production.example`
- Ask in team Slack/Discord

**Need Help with Hosting?**

- Railway docs: https://docs.railway.app
- PlanetScale docs: https://planetscale.com/docs
- .NET deployment: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/

---

✅ **You're all set!** Your team can now develop locally while sharing a single production database.
