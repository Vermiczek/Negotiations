using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Negotiations.Configuration;
using Negotiations.Data;
using Negotiations.Models;
using Negotiations.Services;
using System.Text;
using HealthChecks.NpgSql;
using Npgsql;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Create connection string from environment variables if they exist
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Override with environment variables if they exist (for .env file support)
string dbHost = Environment.GetEnvironmentVariable("DB_HOST");
string dbName = Environment.GetEnvironmentVariable("DB_NAME");
string dbUser = Environment.GetEnvironmentVariable("DB_USER");
string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbName) && 
    !string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPassword))
{
    connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString, 
        name: "postgresql", 
        tags: new[] { "db", "sql", "postgresql" },
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded
    )
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), 
        tags: new[] { "service" });

// Configure JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Override JWT settings from environment variables if they exist
string jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings?.Secret ?? "fallbackKeyForDevOnly";
string jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSettings?.Issuer ?? "NegotiationsApi";
string jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSettings?.Audience ?? "NegotiationsClient";

// Register the JWT settings service with values that may be from .env
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret;
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    
    if (int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES"), out int expiryMinutes))
        options.ExpiryMinutes = expiryMinutes;
    else
        options.ExpiryMinutes = jwtSettings?.ExpiryMinutes ?? 120;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
    options.AddPolicy("RequireSellerRole", policy => policy.RequireRole("seller"));
    options.AddPolicy("RequireAdminOrSellerRole", policy => policy.RequireRole("admin", "seller"));
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "Negotiations API", 
        Version = "v1",
        Description = "API for price negotiations between customers and sellers",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@negotiations-example.com"
        }
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Apply migrations at startup with retry mechanism
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var retryCount = 10; 
    var retryDelay = TimeSpan.FromSeconds(10);
    var success = false;
    
    for (int i = 0; i < retryCount && !success; i++)
    {
        try
        {
            if (i > 0)
            {
                Console.WriteLine($"Retry attempt {i} of {retryCount - 1} for database migrations...");
            }
            
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            if (!await context.Database.CanConnectAsync())
            {
                Console.WriteLine("Cannot connect to the database. Waiting before retry...");
                await Task.Delay(retryDelay);
                continue;
            }
            
            try
            {
                var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM pg_tables WHERE schemaname = 'public'");
                Console.WriteLine($"Database tables check result: {tableExists}");
            }
            catch (Exception tableEx)
            {
                Console.WriteLine($"Table check failed (this may be normal): {tableEx.Message}");
            }
            
            try 
            {
                Console.WriteLine("Applying database migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("Database migrations applied successfully.");
                
                Console.WriteLine("Initializing seed data...");
                await DbInitializer.Initialize(context);
                Console.WriteLine("Database initialized with seed data.");
            }
            catch (Exception migrateEx)
            {
                Console.WriteLine($"Migration error: {migrateEx.Message}");
                Console.WriteLine(migrateEx.ToString());
                throw; 
            }
            
            success = true;
        }
        catch (Exception ex)
        {
            if (i == retryCount - 1)
            {
                Console.WriteLine($"Failed to apply migrations after {retryCount} attempts: {ex.Message}");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Continuing application startup despite migration failure.");
                success = true; 
            }
            else
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}. Retrying in {retryDelay.TotalSeconds} seconds...");
                await Task.Delay(retryDelay);
            }
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Healyh check
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Add a separate endpoint for database health checks
app.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("db"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                exception = e.Value.Exception?.Message,
                duration = e.Value.Duration.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();

app.Run();