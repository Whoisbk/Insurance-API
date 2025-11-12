# Render Deployment Guide

This guide will help you deploy the Insurance Claims API to Render using Docker.

## Prerequisites

1. A Render account (sign up at https://render.com)
2. A MySQL database (you can use Render's MySQL service or an external provider)
3. Your Firebase credentials
4. Your Resend API key

## Deployment Steps

### 1. Push your code to GitHub/GitLab/Bitbucket

Make sure your code is in a Git repository that Render can access.

### 2. Create a Web Service on Render

1. Go to your Render dashboard
2. Click "New +" → "Web Service"
3. Connect your repository
4. Configure the service:
   - **Name**: `insurance-claims-api` (or your preferred name)
   - **Environment**: `Docker`
   - **Region**: Choose the closest region to your users
   - **Branch**: `main` (or your main branch)
   - **Dockerfile Path**: `Dockerfile` (should auto-detect)
   - **Docker Context**: `.` (root directory)

### 3. Set Environment Variables

In the Render dashboard, add these environment variables:

#### Database Connection
```
ConnectionStrings__DefaultConnection=Server=YOUR_DB_HOST;Database=InsuranceClaimsDB;Uid=YOUR_DB_USER;Pwd=YOUR_DB_PASSWORD;Port=3306;CharSet=utf8mb4;SslMode=Required;
```

#### JWT Settings
```
JwtSettings__Secret=YOUR_SECURE_JWT_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG
JwtSettings__Issuer=InsuranceClaimsAPI
JwtSettings__Audience=InsuranceClaimsClient
JwtSettings__TokenExpiryInMinutes=60
JwtSettings__RefreshTokenExpiryInDays=7
```

#### Firebase Configuration

**Important**: When setting the Firebase private key via environment variables, you need to preserve the newlines. Here are two options:

**Option 1: Using \n (recommended for Render)**
```
Firebase__ProjectId=your-firebase-project-id
Firebase__PrivateKey=-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDS16v7EnjhjJNz\n...your full private key...\n-----END PRIVATE KEY-----\n
Firebase__ClientEmail=firebase-adminsdk-xxxxx@your-project.iam.gserviceaccount.com
```

**Option 2: Using ConnectionStrings__DefaultConnection format (alternative)**
You can also use the double underscore format:
```
Firebase:ProjectId=your-firebase-project-id
Firebase:PrivateKey=-----BEGIN PRIVATE KEY-----\n...full key...\n-----END PRIVATE KEY-----\n
Firebase:ClientEmail=firebase-adminsdk-xxxxx@your-project.iam.gserviceaccount.com
```

**Note**: The application will automatically convert `\n` to actual newlines. Make sure your private key includes the `-----BEGIN PRIVATE KEY-----` and `-----END PRIVATE KEY-----` markers.

#### Email Configuration
```
Email__ResendApiKey=your-resend-api-key
Email__From=noreply@yourdomain.com
```

#### Application Settings
```
ASPNETCORE_ENVIRONMENT=Production
```

**Note**: Render automatically sets the `PORT` environment variable - your application will use it automatically.

### 4. Create a MySQL Database (if using Render's MySQL)

1. In Render dashboard, click "New +" → "PostgreSQL" (or use external MySQL)
2. Configure:
   - **Name**: `insurance-claims-db`
   - **Database**: `InsuranceClaimsDB`
   - **User**: Your username
   - **Password**: Generate a strong password
3. Copy the connection string and update the `ConnectionStrings__DefaultConnection` environment variable

### 5. Deploy

1. Click "Create Web Service"
2. Render will build and deploy your application
3. Wait for the build to complete (first build may take 5-10 minutes)
4. Once deployed, your API will be available at `https://your-service-name.onrender.com`

### 6. Verify Deployment

- Health check: `https://your-service-name.onrender.com/health`
- Swagger UI: `https://your-service-name.onrender.com/swagger` (if enabled in production)

## Local Testing with Docker

To test locally before deploying:

```bash
# Build and run with docker-compose
docker-compose up --build

# The API will be available at http://localhost:8080
```

## Troubleshooting

### Build Fails
- Check that all dependencies are in `InsuranceClaimsAPI.csproj`
- Verify Dockerfile syntax
- Check Render build logs for specific errors

### Application Won't Start / Status 139 Error
- **Firebase Configuration**: This is the most common cause. Ensure:
  - All three Firebase environment variables are set: `Firebase__ProjectId`, `Firebase__PrivateKey`, `Firebase__ClientEmail`
  - The private key includes `-----BEGIN PRIVATE KEY-----` and `-----END PRIVATE KEY-----` markers
  - Use `\n` for newlines in the private key (the app will convert them automatically)
  - Example: `Firebase__PrivateKey=-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSj...\n-----END PRIVATE KEY-----\n`
- Verify all environment variables are set correctly
- Check database connection string format
- Review application logs in Render dashboard for specific error messages
- The app will now continue even if Firebase fails to initialize (non-critical)

### Database Connection Issues
- Ensure MySQL is accessible from Render (check firewall rules)
- Verify SSL mode is set correctly (`SslMode=Required`)
- Check database credentials are correct

### Port Issues
- Render sets PORT automatically - don't override it
- The application automatically uses the PORT environment variable

## Environment Variables Reference

| Variable | Description | Required |
|----------|-------------|----------|
| `PORT` | Port number (set by Render automatically) | Yes |
| `ConnectionStrings__DefaultConnection` | MySQL connection string | Yes |
| `JwtSettings__Secret` | JWT secret key (min 32 chars) | Yes |
| `Firebase__ProjectId` | Firebase project ID | Yes |
| `Firebase__PrivateKey` | Firebase private key | Yes |
| `Firebase__ClientEmail` | Firebase client email | Yes |
| `Email__ResendApiKey` | Resend API key | Yes |
| `ASPNETCORE_ENVIRONMENT` | Environment (Production) | Yes |

## Notes

- Render provides HTTPS automatically - no additional configuration needed
- The application listens on `0.0.0.0` to accept connections from any interface
- Logs are written to `/app/logs` directory inside the container
- Database migrations run automatically on startup via `EnsureCreated()`

