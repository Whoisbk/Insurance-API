# Firebase Setup Guide for Insurance Claims API

This guide will help you set up Firebase Authentication for the Insurance Claims API using the Firebase Admin SDK.

## Prerequisites

1. A Firebase project
2. Firebase Admin SDK service account key
3. .NET 9.0 SDK

## Step 1: Create a Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Create a project" or "Add project"
3. Enter a project name (e.g., "insurance-claims-api")
4. Enable/disable Google Analytics as needed
5. Click "Create project"

## Step 2: Enable Authentication

1. In your Firebase project, go to "Authentication" in the left sidebar
2. Click "Get started"
3. Go to the "Sign-in method" tab
4. Enable "Email/Password" authentication
5. Optionally enable other providers as needed

## Step 3: Generate Service Account Key

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your Firebase project
3. Go to "IAM & Admin" > "Service Accounts"
4. Click "Create Service Account"
5. Enter a name and description
6. Assign the "Firebase Admin SDK Administrator Service Agent" role
7. Click "Create and Continue"
8. Click "Done"
9. Find your service account in the list and click on it
10. Go to the "Keys" tab
11. Click "Add Key" > "Create new key"
12. Choose "JSON" format
13. Download the JSON file

## Step 4: Configure Your Application

### Development Environment

1. Open `appsettings.Development.json`
2. Replace the Firebase configuration with your actual values:

```json
{
  "Firebase": {
    "ProjectId": "your-actual-project-id",
    "PrivateKey": "-----BEGIN PRIVATE KEY-----\nYOUR_ACTUAL_PRIVATE_KEY_HERE\n-----END PRIVATE KEY-----\n",
    "ClientEmail": "firebase-adminsdk-xxxxx@your-project.iam.gserviceaccount.com"
  }
}
```

**Important Notes:**

- The `ProjectId` can be found in your Firebase project settings
- The `PrivateKey` should be the entire private key including the BEGIN/END markers
- The `ClientEmail` is the service account email from the JSON file
- Make sure to replace `\n` with actual newlines in the private key

### Production Environment

1. Open `appsettings.Production.json`
2. Update the Firebase configuration with your production values
3. Consider using environment variables or Azure Key Vault for sensitive data

## Step 5: Environment Variables (Recommended for Production)

Instead of storing sensitive data in appsettings files, you can use environment variables:

```bash
export Firebase__ProjectId="your-project-id"
export Firebase__PrivateKey="-----BEGIN PRIVATE KEY-----\nYOUR_PRIVATE_KEY\n-----END PRIVATE KEY-----\n"
export Firebase__ClientEmail="firebase-adminsdk-xxxxx@your-project.iam.gserviceaccount.com"
```

## Step 6: Test the Setup

1. Run your application:

   ```bash
   dotnet run
   ```

2. Check the logs to ensure Firebase is initialized successfully:

   ```
   [Information] Firebase Admin SDK initialized successfully for project: your-project-id
   ```

3. Test creating a user via the API:
   ```bash
   POST /api/AdminUser/insurers
   POST /api/AdminUser/providers
   ```

## API Endpoints

The following endpoints are available for managing users with Firebase:

### Create Insurer

```
POST /api/AdminUser/insurers
Authorization: Bearer <admin-jwt-token>
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "password": "securePassword123",
  "companyName": "ABC Insurance",
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "city": "New York",
  "postalCode": "10001",
  "country": "USA",
  "contactPerson": "Jane Smith"
}
```

### Create Provider

```
POST /api/AdminUser/providers
Authorization: Bearer <admin-jwt-token>
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane.smith@example.com",
  "password": "securePassword123",
  "companyName": "XYZ Healthcare",
  "phoneNumber": "+1234567890",
  "address": "456 Oak Ave",
  "city": "Los Angeles",
  "postalCode": "90210",
  "country": "USA",
  "contactPerson": "Bob Johnson"
}
```

### Other Endpoints

- `PUT /api/AdminUser/insurers/{id}` - Update insurer
- `PUT /api/AdminUser/providers/{id}` - Update provider
- `DELETE /api/AdminUser/insurers/{id}` - Delete insurer
- `DELETE /api/AdminUser/providers/{id}` - Delete provider
- `GET /api/AdminUser/insurers` - Get all insurers
- `GET /api/AdminUser/providers` - Get all providers

## Security Considerations

1. **Never commit service account keys to version control**
2. **Use environment variables or secure key management in production**
3. **Rotate service account keys regularly**
4. **Limit service account permissions to minimum required**
5. **Use HTTPS in production**
6. **Implement proper logging and monitoring**

## Troubleshooting

### Common Issues

1. **Firebase initialization fails**

   - Check that your service account has the correct permissions
   - Verify the project ID matches your Firebase project
   - Ensure the private key is properly formatted

2. **Authentication errors**

   - Verify that email/password authentication is enabled in Firebase
   - Check that the service account email is correct
   - Ensure the private key is not corrupted

3. **Permission denied errors**
   - Make sure the service account has Firebase Admin SDK permissions
   - Check that the project ID is correct

### Logs

Check the application logs for detailed error messages:

- Development: Console output and `logs/` directory
- Production: Configure appropriate log providers

## Additional Resources

- [Firebase Admin SDK Documentation](https://firebase.google.com/docs/admin/setup)
- [Firebase Authentication Documentation](https://firebase.google.com/docs/auth)
- [Google Cloud Service Accounts](https://cloud.google.com/iam/docs/service-accounts)


kill -9 $(lsof -ti:5020)