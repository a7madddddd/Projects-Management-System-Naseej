using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Projects_Management_System_Naseej.Implementations;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using Microsoft.AspNetCore.Identity;
using Projects_Management_System_Naseej;
using Microsoft.Office.Interop.Excel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add repositories with Scoped lifetime
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IFileCategoryRepository, FileCategoryRepository>();
builder.Services.AddScoped<IFilePermissionRepository, FilePermissionRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IFileTypeHandler, PdfHandler>();
builder.Services.AddScoped<IFileTypeHandler, ExcelHandler>();

// Add HttpContextAccessor (this can remain Transient)
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Editor", policy => policy.RequireRole("Editor"));
    options.AddPolicy("Viewer", policy => policy.RequireRole("Viewer"));
    options.AddPolicy("Uploader", policy => policy.RequireRole("Uploader"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CustomAuthenticationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();