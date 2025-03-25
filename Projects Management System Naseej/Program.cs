using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Projects_Management_System_Naseej.Implementations;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using Microsoft.AspNetCore.Identity;
using Projects_Management_System_Naseej;
using Microsoft.Office.Interop.Excel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Projects_Management_System_Naseej.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using Serilog;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using File = System.IO.File;



var builder = WebApplication.CreateBuilder(args);



// CORS Configuration (Allow All Origins)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
             builder => builder
                 .WithOrigins("http://127.0.0.1:5500", "https://localhost:44320")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials());
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});





builder.Services.AddLogging();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()         // Log to the console
    .WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)  // Log to a file
    .CreateLogger();

builder.Host.UseSerilog();  // Use Serilog for logging

builder.Services.AddRazorPages();


builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("YourConnectionString")));


var credentialsPath = builder.Configuration["GoogleServiceAccount:CredentialsPath"];
var applicationName = builder.Configuration["GoogleServiceAccount:ApplicationName"];
builder.Services.AddScoped<DriveService>(provider =>
{
    try
    {
        var credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "service-account.json");

        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException($"Google service account file not found: {credentialsPath}");
        }

        GoogleCredential credential;

        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(new[]
                {
                    DriveService.Scope.Drive,           // Full drive access
                    DriveService.Scope.DriveFile        // File-level access
                });
        }

        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Projects Management System Naseej"
        });
    }
    catch (Exception ex)
    {
        // Comprehensive error logging
        Console.WriteLine($"Drive Service Initialization Error: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        throw;
    }
});



// Add repositories with Scoped lifetime
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IFileCategoryRepository, FileCategoryRepository>();
builder.Services.AddScoped<IFilePermissionRepository, FilePermissionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IFileTypeHandler, PdfHandler>();
builder.Services.AddScoped<IFileTypeHandler, ExcelHandler>();
builder.Services.AddScoped<MicrosoftGraphService>();
builder.Services.AddScoped<GoogleDriveService>();


// Add HttpContextAccessor (this can remain Transient)
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"];
        options.ClientSecret = builder.Configuration["Google:ClientSecret"];

        // IMPORTANT: Explicitly set redirect URI
        options.CallbackPath = "/api/Account/login-callback";

        // Add these scopes
        options.Scope.Add("email");
        options.Scope.Add("profile");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // Log additional information for debugging

                var claims = context.Principal.Identities.First().Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            },
            OnRemoteFailure = context =>
            {
                // Log detailed error information
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };

        options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.SaveTokens = true;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = true;
            options.SuppressModelStateInvalidFilter = true; // Optional

        });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin", "SuperAdmin"));
    options.AddPolicy("Editor", policy => policy.RequireRole("Editor", "Admin", "SuperAdmin"));
    options.AddPolicy("Uploader", policy => policy.RequireRole("Uploader", "Admin", "SuperAdmin"));
    options.AddPolicy("Viewer", policy => policy.RequireRole("Viewer", "Admin", "SuperAdmin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//app.UseMiddleware<CustomAuthenticationMiddleware>();
app.UseCors("AllowLocalhost");
app.UseSession();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapControllers();

app.Run();