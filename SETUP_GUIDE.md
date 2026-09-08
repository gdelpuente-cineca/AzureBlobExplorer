# Azure Blob Explorer - Setup & Deployment Guide

## Quick Start

### Prerequisites
- .NET 8 SDK
- Azure Subscription
- Azure AD B2C Tenant
- Azure Storage Account

### Step 1: Clone Repository

```bash
git clone https://github.com/gdelpuente-cineca/AzureBlobExplorer.git
cd AzureBlobExplorer
```

### Step 2: Configure Azure AD B2C

1. Go to [Azure Portal](https://portal.azure.com)
2. Create/Select your Azure AD B2C tenant
3. Register a web application:
   - **Name**: `AzureBlobExplorer`
   - **Application type**: Web application/API
   - **Redirect URI**: `https://localhost:5001/signin-oidc` (development)
   - Enable: ID token, Access token

4. Create a sign-up/sign-in user flow:
   - **Name**: `b2c_1_susi`
   - **Identity providers**: Email signup
   - **User attributes to collect**: Email Address, Display Name

5. Get your credentials:
   - Application (client) ID
   - Client secret (create one)
   - Tenant ID
   - Tenant name (e.g., `yourtenant.onmicrosoft.com`)

### Step 3: Configure Azure Storage

1. Go to your Storage Account in Azure Portal
2. Get your **Connection String** from Access Keys
3. Create test containers for demo (e.g., `documents`, `reports`)
4. Assign RBAC roles:
   - **Admin users**: "Storage Blob Data Contributor" role
   - **Guest users**: "Storage Blob Data Reader" role

### Step 4: Update Configuration

Edit `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com/",
    "ClientId": "your-app-id",
    "ClientSecret": "your-client-secret",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "SignUpSignInPolicyId": "b2c_1_susi"
  },
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=...;EndpointSuffix=core.windows.net"
  }
}
```

### Step 5: Run Locally

```bash
# Restore dependencies
dotnet restore

# Apply migrations
dotnet ef database update

# Run application
dotnet run
```

Access at: `https://localhost:5001`

---

## Docker Deployment

### Build Docker Image

```bash
docker build -t azure-blob-explorer:latest .
```

### Run with Docker Compose

```bash
# Update docker-compose.yml with your credentials first
docker-compose up -d
```

### Run with Environment Variables

```bash
docker run -d \
  -p 8080:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e AzureAdB2C__Instance=https://yourtenant.b2clogin.com/ \
  -e AzureAdB2C__ClientId=your-app-id \
  -e AzureAdB2C__ClientSecret=your-client-secret \
  -e AzureAdB2C__Domain=yourtenant.onmicrosoft.com \
  -e AzureAdB2C__TenantId=your-tenant-id \
  -e AzureStorage__ConnectionString="your-connection-string" \
  -e ConnectionStrings__DefaultConnection=/app/data/app.db \
  -v blob-explorer-data:/app/data \
  azure-blob-explorer:latest
```

---

## Azure Container Instances Deployment

### Push to Azure Container Registry

```bash
# Login to ACR
az acr login --name myregistry

# Tag image
docker tag azure-blob-explorer:latest myregistry.azurecr.io/azure-blob-explorer:v1

# Push
docker push myregistry.azurecr.io/azure-blob-explorer:v1
```

### Deploy to ACI

```bash
az container create \
  --resource-group myResourceGroup \
  --name azure-blob-explorer \
  --image myregistry.azurecr.io/azure-blob-explorer:v1 \
  --registry-login-server myregistry.azurecr.io \
  --registry-username <username> \
  --registry-password <password> \
  --environment-variables \
    AzureAdB2C__Instance="https://yourtenant.b2clogin.com/" \
    AzureAdB2C__ClientId="your-app-id" \
    AzureAdB2C__ClientSecret="your-client-secret" \
    AzureAdB2C__Domain="yourtenant.onmicrosoft.com" \
    AzureAdB2C__TenantId="your-tenant-id" \
    AzureStorage__ConnectionString="your-connection-string" \
  --dns-name-label azure-blob-explorer \
  --ports 80
```

---

## Setting Up User Access Policies

### Via API (Recommended for admins)

```bash
# Create access policy for a guest user
curl -X POST https://localhost:5001/api/admin/access-policy \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-object-id",
    "containerName": "documents",
    "path": "reports/2025/",
    "permission": 0,
    "expiresAt": "2025-12-31T23:59:59Z"
  }'
```

### Via Azure Storage Explorer

1. Open Azure Storage Explorer
2. Navigate to your Storage Account
3. Right-click on container → "Manage Access Policies"
4. Assign roles to users/groups

### Via Azure Portal

1. Storage Account → "Access Control (IAM)"
2. Click "+ Add" → "Add role assignment"
3. Select role: "Storage Blob Data Reader" (guest) or "Storage Blob Data Contributor" (admin)
4. Assign to user or group

---

## Database Setup

### SQLite (Development)

Database file: `app.db` (auto-created)

```bash
dotnet ef database update
```

### SQL Server (Production)

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=AzureBlobExplorer;Persist Security Info=False;User ID=sqladmin@myserver;Password=YourPassword123!;Encrypt=True;Connection Timeout=30;"
  },
  "Database": {
    "Provider": "SqlServer"
  }
}
```

Then run:
```bash
dotnet ef database update
```

---

## Troubleshooting

### "Redirect URI mismatch" Error
- Ensure B2C app registration includes your deployment URL
- Update `appsettings.json` with correct callback path

### "Access Denied" on Blob Operations
- Check Azure RBAC role assignments
- Verify user has "Storage Blob Data Reader" or higher role
- Check access policy in database

### Database Migration Fails
```bash
# Reset SQLite database
rm app.db
dotnet ef database update
```

### CORS Issues
- Add your frontend domain to `appsettings.json` CORS policy
- Ensure redirect URIs are registered in B2C

---

## Performance Optimization for 1000+ Users

### Database Optimization

1. Use SQL Server instead of SQLite
2. Create indexes on frequently queried columns:
   ```sql
   CREATE INDEX IX_AccessPolicies_UserId ON AccessPolicies(UserId);
   CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
   ```

### Caching

Add Redis caching for container lists:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

### Scaling

- **Horizontal scaling**: Deploy multiple app instances behind load balancer
- **Async operations**: Use async/await for I/O operations (already implemented)
- **Connection pooling**: Azure Blob SDK handles connection pooling

---

## Security Best Practices

✅ Store secrets in Azure Key Vault (not appsettings.json)  
✅ Use managed identities for Azure services  
✅ Enable HTTPS only in production  
✅ Set CORS to specific origins  
✅ Implement rate limiting  
✅ Enable audit logging  
✅ Regularly rotate credentials  
✅ Use SAS tokens for time-limited access  

---

## Monitoring

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry(builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
```

### View Logs

```bash
# Local logs
cat ~/Library/Logs/aspnetcore/AzureBlobExplorer/*.log

# Docker logs
docker logs azure-blob-explorer-container

# Audit logs via API
curl https://localhost:5001/api/admin/audit-logs
```

---

## Support

For issues or questions:
1. Check logs for error details
2. Review [Azure AD B2C documentation](https://learn.microsoft.com/azure/active-directory-b2c/)
3. Check [Azure Storage documentation](https://learn.microsoft.com/azure/storage/)
4. Open an issue on [GitHub](https://github.com/gdelpuente-cineca/AzureBlobExplorer/issues)
