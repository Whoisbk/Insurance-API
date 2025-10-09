# Insurance Claims API 🏥

A comprehensive RESTful API for managing insurance claims, built with ASP.NET Core 9.0 and MySQL.

## 🌟 Features

- **User Management**: Service Providers, Insurance Agents, and Admin roles
- **Claims Processing**: Create, track, and manage insurance claims
- **Quote System**: Service providers can submit quotes for claims
- **Real-time Communication**: SignalR hubs for messages and notifications
- **Document Management**: Upload and manage claim and quote documents
- **Audit Logging**: Complete audit trail for all actions
- **JWT Authentication**: Secure token-based authentication
- **Role-based Authorization**: Granular access control

## 🚀 Quick Start

New to the project? **Choose your preferred database setup:**

### Option 1: MySQL Workbench (Recommended for Teams) ⭐

Perfect if you already have MySQL installed or prefer a GUI.

1. **Install Prerequisites**

   - [MySQL Server & Workbench](https://dev.mysql.com/downloads/)
   - [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

2. **Setup Database**

   - Open MySQL Workbench
   - Run: `scripts/create-database-workbench.sql`
   - Update connection string in `appsettings.Development.json`

3. **Run Application**
   ```bash
   cd InsuranceClaimsAPI
   dotnet run
   ```

📖 **See [WORKBENCH_QUICKSTART.md](WORKBENCH_QUICKSTART.md) - 5 minute setup!**

### Option 2: Docker (Isolated Environment)

Perfect for containerized development.

1. **Install Prerequisites**

   - [Docker Desktop](https://www.docker.com/products/docker-desktop)
   - [.NET 9.0 SDK](https://dotnet.microsoft.com/download)

2. **Clone and Setup**

   ```bash
   git clone <your-repo-url>
   cd InsuranceClaimsAPI
   cp env.example .env
   # Edit .env and change passwords!
   ```

3. **Start Database**

   ```bash
   docker-compose up -d
   ```

4. **Run Application**
   ```bash
   cd InsuranceClaimsAPI
   dotnet run
   ```

📖 **See [QUICKSTART.md](QUICKSTART.md) for Docker setup**

## 📚 Documentation

### Database Setup (Choose One)

- **[WORKBENCH_QUICKSTART.md](WORKBENCH_QUICKSTART.md)** - MySQL Workbench setup (5 min) ⭐
- **[MYSQL_WORKBENCH_SETUP.md](MYSQL_WORKBENCH_SETUP.md)** - Complete Workbench guide
- **[QUICKSTART.md](QUICKSTART.md)** - Docker setup (5 min)
- **[DATABASE_SETUP.md](DATABASE_SETUP.md)** - Complete Docker guide

### Configuration

- **[env.example](env.example)** - Environment variables (for Docker)
- **[scripts/](scripts/)** - Database scripts and utilities

## 🏗️ Architecture

### Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Database**: MySQL 8.0 ([mysql.com](https://www.mysql.com/))
- **ORM**: Entity Framework Core with Pomelo MySQL Provider
- **Authentication**: JWT Bearer Tokens
- **Real-time**: SignalR
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

### Project Structure

```
InsuranceClaimsAPI/
├── Controllers/          # API endpoints
├── Services/            # Business logic
├── Repositories/        # Data access layer
├── Models/
│   ├── Domain/         # Database entities
│   └── DTOs/           # Data transfer objects
├── Data/               # DbContext and migrations
├── Hubs/               # SignalR hubs
├── Middleware/         # Custom middleware
├── Configuration/      # App configuration
└── Utils/              # Helper utilities
```

## 🗄️ Database

The API uses [MySQL](https://www.mysql.com/) as the database. We provide Docker setup for easy local development.

### Database Entities

- **Users** - System users (Service Providers, Agents, Admins)
- **Claims** - Insurance claims
- **Quotes** - Service provider quotes
- **Messages** - User communication
- **Notifications** - System notifications
- **Documents** - Claim and quote attachments
- **AuditLogs** - Audit trail

### Database Management

```bash
# Start MySQL
docker-compose up -d

# Access phpMyAdmin
open http://localhost:8080

# Backup database
./scripts/backup-database.sh

# Restore database
./scripts/restore-database.sh backups/your-backup.sql.gz

# Access MySQL CLI
docker exec -it insurance-claims-mysql mysql -u insurance_user -p
```

## 🔌 API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/refresh` - Refresh JWT token

### Claims

- `GET /api/claims` - Get all claims
- `GET /api/claims/{id}` - Get claim by ID
- `POST /api/claims` - Create new claim
- `PUT /api/claims/{id}` - Update claim
- `DELETE /api/claims/{id}` - Delete claim

### Quotes

- `GET /api/quotes/claim/{claimId}` - Get quotes for claim
- `POST /api/quotes` - Submit quote (Service Provider)
- `PUT /api/quotes/{id}/status` - Update quote status (Agent)

### Messages

- `GET /api/messages/claim/{claimId}` - Get claim messages
- `POST /api/messages` - Send message

### Notifications

- `GET /api/notifications` - Get user notifications
- `PUT /api/notifications/{id}/read` - Mark as read

### SignalR Hubs

- `/messageHub` - Real-time messaging
- `/notificationHub` - Real-time notifications

## 🔐 Security

### Authentication

The API uses JWT (JSON Web Tokens) for authentication:

- Access tokens expire in 60 minutes (configurable)
- Refresh tokens expire in 7 days (configurable)
- Tokens include user ID, email, and role claims

### Authorization Policies

- `RequireServiceProviderRole` - Service Provider only
- `RequireInsuranceAgentRole` - Insurance Agent only
- `RequireAdminRole` - Admin only

### Security Best Practices

✅ Passwords hashed with BCrypt  
✅ JWT tokens for stateless authentication  
✅ Role-based access control  
✅ SQL injection protection via EF Core  
✅ CORS configured for specific origins  
✅ HTTPS enforcement  
✅ Sensitive data logging disabled in production

## 🧪 Testing

```bash
# Run the application
dotnet run

# Test health endpoint
curl https://localhost:5001/health

# View Swagger documentation
open https://localhost:5001/swagger
```

## 📦 Deployment

### Docker Production Setup

```bash
# Use production compose file
docker-compose -f docker-compose.prod.yml up -d

# Or deploy to cloud
# - AWS RDS for MySQL
# - Azure Database for MySQL
# - Google Cloud SQL
```

### Environment Variables

Set these in your production environment:

- `MYSQL_ROOT_PASSWORD` - MySQL root password
- `MYSQL_USER` - Application database user
- `MYSQL_PASSWORD` - Application database password
- `JWT_SECRET` - JWT signing key (strong secret)
- Connection string in `appsettings.Production.json`

## 👥 Team Collaboration

### For New Team Members

1. Read [QUICKSTART.md](QUICKSTART.md)
2. Setup your local environment
3. Review [DATABASE_SETUP.md](DATABASE_SETUP.md)
4. Check the API documentation in Swagger
5. Review code conventions

### Development Workflow

```bash
# 1. Pull latest changes
git pull origin main

# 2. Start database
docker-compose up -d

# 3. Run migrations (if any)
cd InsuranceClaimsAPI
dotnet ef database update

# 4. Run application
dotnet run

# 5. Make changes and test

# 6. Create feature branch
git checkout -b feature/your-feature

# 7. Commit and push
git add .
git commit -m "Add your feature"
git push origin feature/your-feature
```

## 🛠️ Useful Commands

### Docker

```bash
docker-compose up -d              # Start services
docker-compose down               # Stop services
docker-compose logs -f mysql      # View MySQL logs
docker-compose ps                 # Check status
```

### Entity Framework

```bash
dotnet ef migrations add MigrationName        # Create migration
dotnet ef database update                     # Apply migrations
dotnet ef migrations list                     # List migrations
dotnet ef database drop                       # Drop database
```

### .NET

```bash
dotnet run                        # Run application
dotnet build                      # Build application
dotnet clean                      # Clean build artifacts
dotnet restore                    # Restore packages
```

## 📊 Monitoring

- **Health Checks**: `/health` endpoint
- **Logging**: Serilog logs to console and files (`logs/` directory)
- **Database Monitoring**: phpMyAdmin at http://localhost:8080

## 🐛 Troubleshooting

### Common Issues

**"Unable to connect to MySQL"**

```bash
# Check if MySQL is running
docker-compose ps

# Restart MySQL
docker-compose restart mysql

# Check logs
docker-compose logs mysql
```

**"Port 3306 already in use"**

```bash
# Check what's using the port
lsof -i :3306

# Stop other MySQL services or change port in docker-compose.yml
```

**"Migration failed"**

```bash
# Drop and recreate database
dotnet ef database drop
dotnet ef database update
```

## 📞 Support

For issues and questions:

1. Check [DATABASE_SETUP.md](DATABASE_SETUP.md) troubleshooting section
2. Review application logs in `logs/` directory
3. Check Docker logs: `docker-compose logs`
4. Contact team lead or DevOps

## 📄 License

[Your License Here]

## 👨‍💻 Contributors

[Your Team Members]

---

**Built with ❤️ using [MySQL](https://www.mysql.com/) and ASP.NET Core**
# Insurance-API
