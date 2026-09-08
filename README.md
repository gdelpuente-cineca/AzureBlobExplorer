# Azure Blob Storage Explorer with Azure AD B2C

A modern, web-based Azure Blob Storage Explorer with Azure AD B2C authentication supporting both tenant users and guest users with role-based access control.

## Features

✅ **Azure AD B2C Authentication** - Support for both tenant and guest users  
✅ **Guest Access** - Read-only file browsing and download  
✅ **Admin Panel** - Full CRUD operations for administrators  
✅ **Audit Logging** - Track all file downloads and user actions  
✅ **Role-Based Access Control** - Fine-grained permissions per container/folder  
✅ **Scalable** - Designed for 1000+ concurrent users  
✅ **Docker Ready** - Easy deployment with Docker Compose  

## Prerequisites

- **.NET 8 SDK** - Development and build
- **Docker & Docker Compose** - For containerized deployment
- **Azure Subscription** - With Azure Blob Storage and Azure AD B2C tenant
- **Azure AD B2C Application Registration** - For OAuth2/OIDC authentication

## Configuration

### 1. Azure AD B2C Setup

1. Create an Azure AD B2C tenant
2. Register a web application:
   - Name: `AzureBlobExplorer`
   - Redirect URI: `https://localhost:5001/signin-oidc` (dev) or `https://yourdomain.com/signin-oidc` (prod)
   - Enable ID token and Access token
3. Create a sign-up/sign-in user flow (`b2c_1_susi`)
4. Configure custom attributes or app roles as needed

### 2. Environment Configuration

Update `appsettings.json` with your B2C credentials:

```json
{
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com/",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id"
  },
  "AzureStorage": {
    "ConnectionString": "your-storage-account-connection-string"
  }
}
```

### 3. Azure RBAC Configuration

Assign Azure roles to users/groups in your Storage Account:
- **Storage Blob Data Reader** - For guest users (read-only access)
- **Storage Blob Data Contributor** - For admin users (full access)

## Running Locally

### Option A: Using .NET CLI

```bash
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

Access at: `https://localhost:5001`

### Option B: Using Docker Compose

```bash
# Build and run with Docker Compose
docker-compose up --build
```

Access at: `http://localhost:5000`

## Deployment

### Docker Build

```bash
docker build -t azure-blob-explorer:latest .

# Run with environment variables
docker run -d \
  -p 8080:80 \
  -e AzureAdB2C__ClientId=your-client-id \
  -e AzureAdB2C__ClientSecret=your-client-secret \
  -e AzureStorage__ConnectionString=your-storage-connection \
  azure-blob-explorer:latest
```

### Azure Container Instances

```bash
az container create \
  --resource-group myRG \
  --name azure-blob-explorer \
  --image azure-blob-explorer:latest \
  --environment-variables \
    AzureAdB2C__ClientId=your-client-id \
    AzureStorage__ConnectionString=your-storage-connection
```

## Architecture

```
┌─────────────────────────────────────────┐
│        Web Browser / Client             │
└────────────┬────────────────────────────┘
             │ HTTPS
┌────────────▼────────────────────────────┐
│    ASP.NET Core MVC Application         │
│  (Authentication, Authorization, UI)    │
└────────────┬────────────────────────────┘
             │
     ┌───────┴────────┬──────────┐
     │                │          │
┌────▼──────┐  ┌──────▼────┐  ┌─▼──────────┐
│Azure AD B2C│  │ Azure     │  │ SQLite/SQL │
│            │  │ Blob      │  │ Database   │
│(Auth)      │  │ Storage   │  │(Logs)      │
└────────────┘  └───────────┘  └────────────┘
```

## Usage

### For Guest Users

1. Click "Sign In" on the home page
2. Sign up with your email (or sign in if existing)
3. Browse assigned containers and folders
4. Download files with automatic audit logging
5. Logout when done

### For Administrators

1. Sign in with admin account
2. Access admin panel
3. Manage user permissions and access policies
4. View audit logs
5. Configure container access

## Database Schema

### Users
- `Id` - User's Azure AD B2C Object ID
- `DisplayName` - User's display name
- `Email` - User's email
- `CreatedAt` - Registration date
- `Role` - Admin or Guest

### AuditLogs
- `Id` - Log ID
- `UserId` - User who performed the action
- `Action` - Type of action (Download, Upload, Delete, etc.)
- `Container` - Target container
- `Blob` - Target blob/file
- `Timestamp` - When the action occurred
- `IpAddress` - User's IP address

### AccessPolicies
- `Id` - Policy ID
- `UserId` - User the policy applies to
- `ContainerName` - Target container
- `Path` - Folder path (optional)
- `Permission` - Read, Write, Delete
- `ExpiresAt` - Policy expiration date

## Security Considerations

✅ All authentication handled by Azure AD B2C  
✅ Access control enforced at API level  
✅ SAS tokens for time-limited blob access  
✅ HTTPS enforced in production  
✅ Audit logging for compliance  
✅ CORS properly configured  
✅ Secrets stored in environment variables  

## Troubleshooting

### "Redirect URI mismatch" error
- Ensure redirect URI in B2C app registration matches your deployment URL
- Check `appsettings.json` configuration

### "Access Denied" to blob storage
- Verify Azure RBAC role assignments
- Check Storage Account firewall rules
- Ensure connection string is correct

### Database migration fails
- Delete `app.db` and re-run migrations
- Check database connection string

## Contributing

Contributions are welcome! Please submit pull requests or open issues for bugs and feature requests.

## License

MIT License - See LICENSE file for details

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review Azure AD B2C documentation
3. Open an issue on GitHub
