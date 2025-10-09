# 🌐 MySQL Hosting Providers - Quick Setup Guide

This guide helps you choose and set up a **free/affordable remote MySQL database** for production deployment.

---

## 🏆 Recommended Providers Comparison

| Provider            | Free Tier    | Storage | Limitations         | Best For         | Setup Difficulty |
| ------------------- | ------------ | ------- | ------------------- | ---------------- | ---------------- |
| 🟣 **Railway**      | 500 hrs/mo   | 1GB     | $5 credit/month     | .NET apps        | ⭐ Easy          |
| 🟠 **PlanetScale**  | Forever free | 5GB     | 1 billion reads/mo  | High traffic     | ⭐⭐ Medium      |
| 🟢 **Clever Cloud** | Forever free | 256MB   | Limited connections | Small projects   | ⭐ Easy          |
| 🔵 **Aiven.io**     | 30-day trial | 1GB     | Trial only          | Testing          | ⭐⭐ Medium      |
| ⚫ **AlwaysData**   | Forever free | 100MB   | Ads on free tier    | European hosting | ⭐ Easy          |

---

## 🟣 Railway (Recommended)

**Why Railway?**

- ✅ Super easy .NET deployment
- ✅ MySQL as a plugin (one-click setup)
- ✅ Free $5 credit/month (enough for small projects)
- ✅ Great for group projects
- ✅ Can deploy your C# API on the same platform

### Setup Steps

1. **Sign up:** https://railway.app (use GitHub for easy login)

2. **Create a new project:**

   - Click "New Project"
   - Choose "Provision MySQL"
   - MySQL database will be created instantly

3. **Get connection details:**

   - Click on your MySQL service
   - Go to "Variables" tab
   - You'll see:
     ```
     MYSQL_HOST=containers-us-west-xxx.railway.app
     MYSQL_PORT=6xxx
     MYSQL_DATABASE=railway
     MYSQL_USER=root
     MYSQL_PASSWORD=xxxxxx
     ```

4. **Create your database:**

   ```bash
   # Connect via terminal
   mysql -h containers-us-west-xxx.railway.app -P 6xxx -u root -p

   # Create database
   CREATE DATABASE InsuranceClaimsDB;
   USE InsuranceClaimsDB;

   # Run your init script
   source /path/to/scripts/init-db.sql
   ```

5. **Update `appsettings.Production.json`:**

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=containers-us-west-xxx.railway.app;Database=InsuranceClaimsDB;Uid=root;Pwd=your_password;Port=6xxx;CharSet=utf8mb4;SslMode=Required;"
     }
   }
   ```

6. **Deploy your .NET API to Railway (Optional):**

   ```bash
   # Install Railway CLI
   npm i -g @railway/cli

   # Login
   railway login

   # Link to project
   railway link

   # Deploy
   railway up
   ```

**Cost Estimate:** ~$3-4/month for small projects (within free tier)

---

## 🟠 PlanetScale (Best for Production)

**Why PlanetScale?**

- ✅ 5GB storage free forever
- ✅ 1 billion row reads/month
- ✅ Built-in backups
- ✅ Horizontal scaling
- ⚠️ Uses Vitess (some differences from standard MySQL)

### Setup Steps

1. **Sign up:** https://planetscale.com (use GitHub)

2. **Create a database:**

   - Click "Create database"
   - Name: `insurance-claims-db`
   - Region: Choose closest to your users
   - Click "Create database"

3. **Get connection string:**

   - Click "Connect"
   - Choose "General"
   - Copy the connection details:
     ```
     Host: aws.connect.psdb.cloud
     Username: xxxxxx
     Password: pscale_pw_xxxxxx
     ```

4. **Update `appsettings.Production.json`:**

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=aws.connect.psdb.cloud;Database=insurance-claims-db;Uid=your_username;Pwd=pscale_pw_xxxxx;SslMode=Required;"
     }
   }
   ```

5. **Special note for PlanetScale:**
   - PlanetScale uses branches (like Git)
   - Main branch is production
   - Create a dev branch for testing schema changes
   - Merge branches to promote changes

**Cost Estimate:** Free forever for hobby projects

---

## 🟢 Clever Cloud (European Option)

**Why Clever Cloud?**

- ✅ Forever free tier (256MB)
- ✅ European data centers
- ✅ GDPR compliant
- ⚠️ Limited to 10 concurrent connections

### Setup Steps

1. **Sign up:** https://www.clever-cloud.com

2. **Create an add-on:**

   - Click "Create" → "An add-on"
   - Choose "MySQL"
   - Select "DEV" plan (free)
   - Choose region (Paris or other)

3. **Get connection details:**

   - Go to your add-on dashboard
   - Click "Connection URI"
   - Example:
     ```
     mysql://user:pass@mysql-host.services.clever-cloud.com:3306/dbname
     ```

4. **Parse the connection string:**

   ```
   Server=mysql-host.services.clever-cloud.com
   Database=dbname
   Uid=user
   Pwd=pass
   Port=3306
   ```

5. **Update `appsettings.Production.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=mysql-host.services.clever-cloud.com;Database=dbname;Uid=user;Pwd=pass;Port=3306;CharSet=utf8mb4;SslMode=Preferred;"
     }
   }
   ```

**Cost Estimate:** Free (up to 256MB)

---

## 🔵 Aiven.io (Trial Option)

**Why Aiven?**

- ✅ 30-day free trial ($300 credit)
- ✅ Multiple cloud providers (AWS, GCP, Azure)
- ✅ High performance
- ⚠️ Requires payment method after trial

### Setup Steps

1. **Sign up:** https://aiven.io (30-day trial)

2. **Create MySQL service:**

   - Click "Create service"
   - Choose "MySQL"
   - Select cloud provider & region
   - Choose smallest plan (for trial)

3. **Get connection details:**

   - Go to service "Overview"
   - Find "Connection information"
   - Download CA certificate (for SSL)

4. **Update `appsettings.Production.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=mysql-xxx.aivencloud.com;Database=defaultdb;Uid=avnadmin;Pwd=your_password;Port=12345;CharSet=utf8mb4;SslMode=Required;"
     }
   }
   ```

**Cost Estimate:** ~$30/month after trial (cheapest plan)

---

## 🎯 Quick Decision Guide

### Choose **Railway** if:

- ✅ You want to deploy your C# API on the same platform
- ✅ You're new to cloud hosting
- ✅ You want a simple all-in-one solution

### Choose **PlanetScale** if:

- ✅ You expect high traffic
- ✅ You want free forever (5GB is enough)
- ✅ You understand Git-like workflow

### Choose **Clever Cloud** if:

- ✅ Your data must stay in Europe (GDPR)
- ✅ You have a very small database (<256MB)
- ✅ You want 100% free hosting

### Choose **Aiven.io** if:

- ✅ You need enterprise features
- ✅ You're willing to pay after trial
- ✅ You need multi-cloud deployment

---

## 🔒 Security Best Practices

### SSL/TLS Configuration

**Always use SSL in production:**

```
SslMode=Required;
```

**For Railway, PlanetScale, Aiven:**

- SSL is enforced by default
- Use `SslMode=Required`

**For Clever Cloud:**

- Use `SslMode=Preferred` (falls back if SSL unavailable)

### Firewall Rules

Most providers automatically secure your database:

- ✅ Only allow connections from your API server's IP
- ✅ Use strong passwords
- ✅ Rotate credentials regularly

### Connection Pooling

Add to your connection string for better performance:

```
Pooling=true;MinimumPoolSize=0;MaximumPoolSize=100;
```

Full example:

```
Server=host;Database=db;Uid=user;Pwd=pass;Port=3306;CharSet=utf8mb4;SslMode=Required;Pooling=true;MinimumPoolSize=0;MaximumPoolSize=100;
```

---

## 🧪 Testing Your Connection

### From Command Line (MySQL Client)

```bash
# Standard MySQL
mysql -h YOUR_HOST -P YOUR_PORT -u YOUR_USER -p --ssl-mode=REQUIRED

# If you get SSL errors
mysql -h YOUR_HOST -P YOUR_PORT -u YOUR_USER -p --ssl-mode=PREFERRED
```

### From Your C# API

Create a test endpoint in `AuthController.cs`:

```csharp
[HttpGet("db-test")]
[AllowAnonymous]
public async Task<IActionResult> TestDatabaseConnection()
{
    try
    {
        await _context.Database.CanConnectAsync();
        var userCount = await _context.Users.CountAsync();
        return Ok(new {
            status = "Connected",
            userCount,
            environment = _env.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new {
            status = "Failed",
            error = ex.Message
        });
    }
}
```

Test with:

```bash
curl https://your-api.com/api/auth/db-test
```

---

## 📊 Monitoring & Maintenance

### Check Database Size

```sql
SELECT
    table_schema AS 'Database',
    ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS 'Size (MB)'
FROM information_schema.tables
WHERE table_schema = 'InsuranceClaimsDB'
GROUP BY table_schema;
```

### Optimize Tables

```sql
OPTIMIZE TABLE Claims;
OPTIMIZE TABLE Users;
OPTIMIZE TABLE Quotes;
```

### Backup Database

```bash
# From Railway/PlanetScale/Clever Cloud
mysqldump -h YOUR_HOST -P YOUR_PORT -u YOUR_USER -p InsuranceClaimsDB > backup.sql

# Restore
mysql -h YOUR_HOST -P YOUR_PORT -u YOUR_USER -p InsuranceClaimsDB < backup.sql
```

---

## 🆘 Troubleshooting

### Issue: "SSL connection error"

**Solution:**

- Change `SslMode=Required` to `SslMode=Preferred`
- Download CA certificate from provider
- Or disable SSL for testing (NOT recommended for production)

### Issue: "Host 'xxx' is not allowed to connect"

**Solution:**

- Check firewall rules in your hosting provider
- Add your deployment server's IP to whitelist
- Some providers auto-allow, some require manual setup

### Issue: "Too many connections"

**Solution:**

- Reduce `MaximumPoolSize` in connection string
- Check for connection leaks in your code
- Upgrade to a higher tier

### Issue: "Database doesn't exist"

**Solution:**

```bash
# Connect and create database
mysql -h YOUR_HOST -P YOUR_PORT -u YOUR_USER -p
CREATE DATABASE InsuranceClaimsDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

---

## 🚀 Next Steps

After setting up your production database:

1. ✅ Test connection locally with production credentials
2. ✅ Run database initialization script
3. ✅ Set environment variables on your hosting platform
4. ✅ Deploy your C# API
5. ✅ Test all endpoints
6. ✅ Set up monitoring/alerts

---

## 📚 Additional Resources

- **Railway Docs:** https://docs.railway.app
- **PlanetScale Docs:** https://planetscale.com/docs
- **Clever Cloud Docs:** https://www.clever-cloud.com/doc/
- **.NET Deployment:** https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/
- **MySQL Connector/NET:** https://dev.mysql.com/doc/connector-net/en/

---

✅ **Ready to deploy!** Pick a provider and follow the setup steps above.

**Need help choosing?** For group projects with C# API, we recommend **Railway** for its simplicity and integrated deployment.
