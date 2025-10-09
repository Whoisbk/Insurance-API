# ⚡ Quick Start Guide - Insurance Claims API

Fast setup guide for team members. For detailed instructions, see `ENVIRONMENT_SETUP.md`.

---

## 🚀 For New Developers (First Time Setup)

### 1️⃣ Install Prerequisites

✅ **.NET 9 SDK** - https://dotnet.microsoft.com/download
✅ **MySQL 8.0+** - https://dev.mysql.com/downloads/mysql/
✅ **MySQL Workbench** (optional) - https://dev.mysql.com/downloads/workbench/

### 2️⃣ Clone the Repository

```bash
git clone <repository-url>
cd InsuranceClaimsAPI
```

### 3️⃣ Set Up Local MySQL

**Install MySQL** (if not already installed):

- macOS: `brew install mysql && brew services start mysql`
- Windows: Download installer from MySQL website
- Linux: `sudo apt install mysql-server`

**Create the database:**

```bash
mysql -u root -p < scripts/init-db.sql
```

Or let the API create it automatically on first run.

### 4️⃣ Configure Your Local Settings

**Option A: Update appsettings.Development.json**

Edit `InsuranceClaimsAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YOUR_MYSQL_PASSWORD;Port=3306;CharSet=utf8mb4;"
  }
}
```

**Option B: Use User Secrets (Recommended)**

```bash
cd InsuranceClaimsAPI
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=InsuranceClaimsDB;Uid=root;Pwd=YOUR_PASSWORD;Port=3306;CharSet=utf8mb4;"
dotnet user-secrets set "JwtSettings:Secret" "YourDevSecretKeyAtLeast32CharactersLong"
```

### 5️⃣ Run the API

```bash
cd InsuranceClaimsAPI
dotnet restore
dotnet run
```

✅ API will be available at: `https://localhost:5001`

### 6️⃣ Test the API

Open browser and visit:

- Swagger UI: `https://localhost:5001/swagger`
- Health check: `https://localhost:5001/health`

---

## 🌍 For Production Deployment

### 1️⃣ Choose MySQL Hosting

Pick one (see `MYSQL_HOSTING_PROVIDERS.md` for detailed setup):

- 🟣 **Railway** - Easiest, great for .NET
- 🟠 **PlanetScale** - 5GB free forever
- 🟢 **Clever Cloud** - European, GDPR-compliant

### 2️⃣ Update Production Config

Edit `InsuranceClaimsAPI/appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_HOST;Database=InsuranceClaimsDB;Uid=USER;Pwd=PASSWORD;Port=3306;SslMode=Required;"
  },
  "JwtSettings": {
    "Secret": "STRONG_PRODUCTION_SECRET_AT_LEAST_32_CHARS"
  }
}
```

### 3️⃣ Set Environment Variables

On your hosting platform (Railway, Azure, etc.):

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=host;Database=db;Uid=user;Pwd=pass;Port=3306;SslMode=Required;
JwtSettings__Secret=your_production_secret
```

### 4️⃣ Deploy

```bash
dotnet publish -c Release
# Upload to your hosting platform
```

---

## 📁 Project Structure

```
InsuranceClaimsAPI/
├── Configuration/          # App settings & mappings
├── Controllers/            # API endpoints
├── Data/                   # Database context & seeding
├── Hubs/                   # SignalR real-time hubs
├── Models/                 # Domain models & DTOs
├── Repositories/           # Data access layer
├── Services/               # Business logic
├── appsettings.json        # Base settings (shared)
├── appsettings.Development.json   # Local dev settings
└── appsettings.Production.json    # Production settings
```

---

## 🔑 Environment Files Reference

| File                           | Purpose      | Git Status   | Notes               |
| ------------------------------ | ------------ | ------------ | ------------------- |
| `appsettings.json`             | Base config  | ✅ Committed | No secrets          |
| `appsettings.Development.json` | Local dev    | ✅ Committed | Use placeholders    |
| `appsettings.Production.json`  | Production   | ⚠️ Committed | Use placeholders    |
| `.env`                         | Dev secrets  | ❌ Ignored   | Not committed       |
| `.env.production`              | Prod secrets | ❌ Ignored   | Not committed       |
| User Secrets                   | Dev only     | ❌ Ignored   | Stored outside repo |

---

## 🧪 Common Commands

### Development

```bash
# Run in development mode
dotnet run --environment Development

# Watch for changes (hot reload)
dotnet watch run

# Run tests
dotnet test

# Create database migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

### Production

```bash
# Build for production
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish

# Run in production mode (locally for testing)
dotnet run --environment Production
```

### Database

```bash
# Connect to local MySQL
mysql -u root -p InsuranceClaimsDB

# Run initialization script
mysql -u root -p < scripts/init-db.sql

# Backup database
mysqldump -u root -p InsuranceClaimsDB > backup.sql

# Restore database
mysql -u root -p InsuranceClaimsDB < backup.sql
```

### User Secrets

```bash
# Initialize user secrets
dotnet user-secrets init

# Set a secret
dotnet user-secrets set "Key:SubKey" "Value"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Key:SubKey"

# Clear all secrets
dotnet user-secrets clear
```

---

## 🔧 Troubleshooting

### "Unable to connect to MySQL"

- Check MySQL is running: `mysql.server status`
- Verify username/password in config
- Ensure port 3306 is available

### "Database does not exist"

- Run `mysql -u root -p < scripts/init-db.sql`
- Or let the API create it: `dotnet run`

### "JWT token validation failed"

- Ensure JWT secret is at least 32 characters
- Check secret matches in config

### "Swagger not showing"

- Only available in Development mode
- Set: `ASPNETCORE_ENVIRONMENT=Development`

---

## 📚 Documentation

- **Full Setup Guide:** `ENVIRONMENT_SETUP.md`
- **MySQL Hosting:** `MYSQL_HOSTING_PROVIDERS.md`
- **MySQL Workbench:** `MYSQL_WORKBENCH_SETUP.md`
- **API Documentation:** Visit `/swagger` when running in Development

---

## 🆘 Getting Help

**Issues with:**

- 🔧 Local setup → Check `ENVIRONMENT_SETUP.md`
- 🗄️ MySQL Workbench → Check `MYSQL_WORKBENCH_SETUP.md`
- 🌍 Production hosting → Check `MYSQL_HOSTING_PROVIDERS.md`
- 🐛 Bugs → Create an issue in the repo

**Still stuck?** Ask in team chat or contact project lead.

---

## ✅ Checklist for New Developers

- [ ] Install .NET 9 SDK
- [ ] Install MySQL 8.0+
- [ ] Clone repository
- [ ] Create local database
- [ ] Update `appsettings.Development.json` or use user secrets
- [ ] Run `dotnet restore`
- [ ] Run `dotnet run`
- [ ] Visit `https://localhost:5001/swagger`
- [ ] Test API endpoints

---

## 🎯 Next Steps After Setup

1. **Read the API docs** at `/swagger`
2. **Test authentication** - Register a user, login, get token
3. **Explore endpoints** - Claims, Quotes, Messages, Notifications
4. **Try SignalR hubs** - Real-time messaging
5. **Review the code** - Understand the architecture

---

**Happy coding! 🚀**
