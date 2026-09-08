# Architecture Overview

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Web Browser / Client                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTPS
┌──────────────────────────▼──────────────────────────────────┐
│           ASP.NET Core 8 MVC Application                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Middleware Layer                                      │ │
│  │  - Authentication (MSAL.NET + B2C)                    │ │
│  │  - Authorization (Role-based)                         │ │
│  │  - Error Handling                                     │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  Controllers                                           │ │
│  │  - HomeController (UI navigation)                     │ │
│  │  - AuthController (Login/Logout)                      │ │
│  │  - BlobController (API for blob operations)           │ │
│  │  - AdminController (Management & audit logs)          │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────┬─────────────────────────────────┐ │
│  │ Services             │ Services                        │ │
│  ├──────────────────────┼─────────────────────────────────┤ │
│  │ IAzureBlobService    │ - List containers/blobs        │ │
│  │ IAuditService        │ - Download/Upload/Delete       │ │
│  │ IAccessPolicyService │ - Generate SAS URIs            │ │
│  └──────────────────────┴─────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │ Data Access Layer                                       │ │
│  │ - Entity Framework Core                                │ │
│  │ - ApplicationDbContext                                 │ │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────┬────────────────┘
                           │                  │
          ┌────────────────▼──┐   ┌──────────▼──────────┐
          │ Azure Blob        │   │ SQLite/SQL Server   │
          │ Storage           │   │ Database            │
          │                   │   │                     │
          │ - Containers      │   │ - Users             │
          │ - Blobs           │   │ - AuditLogs         │
          │ - Access Control  │   │ - AccessPolicies    │
          └───────────────────┘   └─────────────────────┘
                  ▲
                  │
          ┌───────┴─────────┐
          │ Azure AD B2C    │
          │ - Authentication│
          │ - User Identity │
          │ - Token Validation
          └─────────────────┘
```

## Data Flow

### Guest User Download Flow

```
1. User navigates to app
   └─> Redirected to B2C login
   └─> User signs in with B2C account
   
2. App receives ID token from B2C
   └─> Token validated
   └─> User object created/updated in database
   └─> User claims extracted
   
3. User sees dashboard with containers
   └─> API checks AccessPolicies table
   └─> Only accessible containers displayed
   
4. User navigates to folder/file
   └─> BlobController verifies access
   └─> Audit log entry created
   
5. User clicks Download
   └─> API checks BlobPermission.Read
   └─> File stream returned
   └─> Audit log: Download recorded
   └─> User's browser downloads file
```

### Admin User Full Access Flow

```
1. Admin user logs in
   └─> User.Role = "Admin" (from B2C claims)
   
2. Admin can perform any action
   └─> [AdminOnly] authorization policy enforced
   └─> Access policy checks bypassed for admins
   └─> Full CRUD operations allowed
   
3. Admin accesses admin panel
   └─> View audit logs
   └─> Manage user policies
   └─> Create/Edit/Delete access policies
```

## Key Components

### 1. Authentication (MSAL.NET + Azure AD B2C)
- Handles OAuth2/OpenID Connect flow
- Manages tokens and token refresh
- Extracts claims from B2C user profiles
- Supports both tenant and guest users

### 2. Authorization (Role-Based)
- Guest users: Read-only access (via AccessPolicies)
- Admin users: Full access (via [AdminOnly] policy)
- Claims-based validation for user roles

### 3. Blob Operations Service
- Abstracts Azure SDK complexity
- Validates user access before operations
- Generates SAS tokens for time-limited access
- Handles error cases gracefully

### 4. Access Policy Service
- Manages user-container-permission mappings
- Supports path-level restrictions
- Handles policy expiration
- Caches accessible containers per user

### 5. Audit Service
- Logs all actions (download, upload, delete, browse)
- Records user, timestamp, IP address
- Tracks success/failure
- Enables compliance and security auditing

### 6. Database Schema

#### Users Table
```sql
CREATE TABLE Users (
    Id TEXT PRIMARY KEY,              -- Azure AD B2C Object ID
    DisplayName TEXT NOT NULL,         -- User's display name
    Email TEXT NOT NULL UNIQUE,        -- User's email
    Role INTEGER DEFAULT 0,            -- 0=Guest, 1=Admin
    CreatedAt DATETIME DEFAULT NOW(),  -- Registration date
    LastLogin DATETIME,                -- Last login timestamp
    IsActive BOOLEAN DEFAULT 1         -- Account status
);
```

#### AuditLogs Table
```sql
CREATE TABLE AuditLogs (
    Id INTEGER PRIMARY KEY,
    UserId TEXT NOT NULL,              -- User who performed action
    Action TEXT NOT NULL,              -- Download, Upload, Delete, etc
    ContainerName TEXT,                -- Target container
    BlobName TEXT,                     -- Target blob
    FileSizeBytes BIGINT,              -- File size in bytes
    Timestamp DATETIME DEFAULT NOW(),  -- When action occurred
    IpAddress TEXT,                    -- User's IP address
    IsSuccessful BOOLEAN DEFAULT 1,    -- Success flag
    ErrorMessage TEXT,                 -- Error details if failed
    INDEX IX_UserId (UserId),
    INDEX IX_Timestamp (Timestamp DESC),
    INDEX IX_UserId_Timestamp (UserId, Timestamp DESC)
);
```

#### AccessPolicies Table
```sql
CREATE TABLE AccessPolicies (
    Id INTEGER PRIMARY KEY,
    UserId TEXT NOT NULL,              -- User being granted access
    ContainerName TEXT NOT NULL,       -- Target container
    Path TEXT,                         -- Optional: restrict to folder
    Permission INTEGER DEFAULT 0,      -- 0=Read, 1=Write, 2=Delete, 3=All
    CreatedAt DATETIME NOT NULL,       -- Policy creation date
    ExpiresAt DATETIME,                -- Policy expiration (optional)
    IsActive BOOLEAN DEFAULT 1,        -- Active flag
    INDEX IX_UserId_Container (UserId, ContainerName)
);
```

## Security Measures

### Authentication
- Azure AD B2C handles all identity management
- Token validation on every request
- Automatic token refresh
- Support for MFA via B2C policies

### Authorization
- Role-based access control (Admin/Guest)
- Policy-based access to containers
- Path-level restrictions within containers
- Time-limited policy expiration

### Data Protection
- HTTPS enforced in production
- SAS tokens for direct blob access (time-limited)
- No secrets in application code
- Environment variables for sensitive config

### Audit & Compliance
- All actions logged with timestamp and user
- IP address tracking
- Success/failure recording
- Long-term audit trail retention

## Scalability Considerations

### Database
- SQLite for development (< 100 users)
- SQL Server for production (> 100 users)
- Indexes on frequently queried columns
- Partitioning audit logs by date

### Application
- Stateless design (no session affinity needed)
- Async/await throughout
- Connection pooling to Azure Storage
- Support for multiple instances behind load balancer

### Caching
- User access policies cached per session
- Container lists cached (short TTL)
- Token cache via MSAL

### Azure Services
- Azure Storage handles blob scalability
- Azure AD B2C handles auth at scale
- Application Insights for monitoring

## Deployment Strategies

### Local Development
- .NET CLI or Visual Studio
- SQLite database
- Local B2C redirect URI

### Docker
- Containerized app
- Optional SQL Server container
- Environment-variable configuration
- Easy to push to cloud registries

### Azure Container Instances
- Managed container service
- Auto-scaling support
- Integration with other Azure services
- Pay-per-second pricing

### Azure App Service
- Managed platform
- Built-in SSL/HTTPS
- Application Insights integration
- Slot-based deployments

## Performance Metrics

### Typical Response Times
- List containers: < 500ms
- List blobs (100 items): < 800ms
- Download file (10MB): < 2s + network speed
- Audit log insertion: < 50ms

### Scalability Targets
- Concurrent users: 1000+
- API requests/second: 100+
- Audit log retention: 1 year
- Database size (1M audit entries): ~500MB

## Monitoring & Logging

### Application Logging
- Structured logging via Serilog
- Levels: Information, Warning, Error
- Destination: Console, File, Application Insights

### Audit Logging
- Separate audit trail in database
- Queryable via admin API
- Compliance-ready format

### Performance Monitoring
- Application Insights integration
- Request/response time tracking
- Exception tracking
- Custom events for business metrics
