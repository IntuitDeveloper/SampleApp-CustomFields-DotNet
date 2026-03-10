using QuickBooks_CustomFields_API.Models;
using QuickBooks_CustomFields_API.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "QuickBooks Custom Fields API",
        Version = "v1",
        Description = "API for managing QuickBooks Custom Fields using GraphQL"
    });

    // Add Bearer token authentication
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure QuickBooks settings
builder.Services.Configure<QuickBooksConfig>(
    builder.Configuration.GetSection("QuickBooks"));

// Register services
builder.Services.AddScoped<ITokenManagerService, TokenManagerService>();
builder.Services.AddScoped<ICustomFieldService, CustomFieldService>();

// Add HTTP client
builder.Services.AddHttpClient();

// Add session support for OAuth state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add data protection for session cookies
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

// Enable static files for the UI
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable session middleware
app.UseSession();

app.UseAuthorization();

// Enable Swagger in all environments for API testing
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickBooks Custom Fields API v1");
    options.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
});

app.MapControllers();

app.Run();
