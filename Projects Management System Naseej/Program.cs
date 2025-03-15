using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Projects_Management_System_Naseej.Implementations;
using Projects_Management_System_Naseej.Models;
using Projects_Management_System_Naseej.Repositories;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<MyDbContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.


builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<MyDbContex>() 
.AddDefaultTokenProviders();




/////////////
// Add repositories
builder.Services.AddTransient<IFileRepository, FileRepository>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IRoleRepository, RoleRepository>();
builder.Services.AddTransient<IFileCategoryRepository, FileCategoryRepository>();
builder.Services.AddTransient<IFilePermissionRepository, FilePermissionRepository>();
builder.Services.AddTransient<IAuditLogRepository, AuditLogRepository>();

// Add file type handlers
builder.Services.AddTransient<PdfHandler>();
builder.Services.AddTransient<ExcelHandler>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
