using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Negotiations.Configuration;
using Negotiations.Data;
using Negotiations.Models;
using Negotiations.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        name: "database", 
        tags: new[] { "db", "sql", "postgresql" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), 
        tags: new[] { "service" });

// Configure JWT
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Add Authentication
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
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? "fallbackKeyForDevOnly"))
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
    options.AddPolicy("RequireSellerRole", policy => policy.RequireRole("seller"));
    options.AddPolicy("RequireAdminOrSellerRole", policy => policy.RequireRole("admin", "seller"));
});

// Register custom services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger services with JWT configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Negotiations API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
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
});

var app = builder.Build();

// Apply migrations at startup with retry mechanism
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var retryCount = 5;
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
            
            // Apply migrations instead of EnsureCreated
            // This will create the database if it doesn't exist and apply all migrations
            context.Database.Migrate();
            Console.WriteLine("Database migrations applied successfully.");
            
            // Initialize database with seed data
            await DbInitializer.Initialize(context);
            Console.WriteLine("Database initialized with seed data.");
            
            success = true;
        }
        catch (Exception ex)
        {
            if (i == retryCount - 1)
            {
                Console.WriteLine($"Failed to apply migrations after {retryCount} attempts: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            else
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}. Retrying in {retryDelay.TotalSeconds} seconds...");
                await Task.Delay(retryDelay);
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger and Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints
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

app.MapControllers();

app.Run();