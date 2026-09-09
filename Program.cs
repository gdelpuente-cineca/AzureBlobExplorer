using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using AzureBlobExplorer.Data;
using AzureBlobExplorer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var configuration = builder.Configuration;
var enableAuditLogging = configuration.GetValue<bool>("Features:EnableAuditLogging");
var enableAccessPolicies = configuration.GetValue<bool>("Features:EnableAccessPolicies");

// Add services to the container

// Authentication with Azure AD B2C
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAdB2C"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Authorization policy
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("extension_role", "Admin"));
    
    options.AddPolicy("CanDownload", policy =>
        policy.RequireAuthenticatedUser());
});

// Add MVC with authorization filter
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();

// Add Entity Framework only if audit/access policies are enabled
if (enableAuditLogging || enableAccessPolicies)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=app.db";

    if (builder.Configuration.GetValue<string>("Database:Provider") == "SqlServer")
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
    else
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
    }

    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IAccessPolicyService, AccessPolicyService>();
}
else
{
    // Add null implementations when features are disabled
    builder.Services.AddScoped<IAuditService, NoOpAuditService>();
    builder.Services.AddScoped<IAccessPolicyService, NoOpAccessPolicyService>();
}

// Add Services (always available)
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();

// Add Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowB2CCallback", builder =>
        builder.WithOrigins("http://localhost:5000", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add HttpContextAccessor for audit service
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Apply database migrations only if enabled
if (enableAuditLogging || enableAccessPolicies)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
