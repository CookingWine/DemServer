using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Server.Data;
using Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- 数据库
// 连接串里的相对路径统一解析到项目根目录，避免从不同工作目录启动时生成多个 db 文件。
var rawConnectionString = builder.Configuration.GetConnectionString("Default")
                          ?? "Data Source=App_Data/server.db";

var sqliteBuilder = new SqliteConnectionStringBuilder(rawConnectionString);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource);
}

var sqliteDirectory = Path.GetDirectoryName(sqliteBuilder.DataSource);
if (!string.IsNullOrWhiteSpace(sqliteDirectory))
{
    Directory.CreateDirectory(sqliteDirectory);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteBuilder.ConnectionString));

// ---------------------------------------------------------------- Web 基础能力
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "本地后台服务 API",
        Version = "v1",
        Description = "供本地桌面客户端调用的后台接口。"
    });
});

// 桌面端如果是 Electron / WebView2 这类内嵌浏览器，会受同源策略限制，这里放开本地调用。
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// ---------------------------------------------------------------- 建库
// 首次启动自动建表；有迁移文件时走迁移，没有则用 EnsureCreated 兜底。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.GetMigrations().Any())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// ---------------------------------------------------------------- 管道
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "本地后台服务 API v1");
    });
}

app.MapControllers();

// 根路径给个可读的服务信息，方便浏览器直接打开确认服务活着。
app.MapGet("/", () => Results.Ok(new
{
    service = "本地后台服务",
    status = "running",
    docs = "/swagger",
    health = "/api/health",
    serverTime = DateTimeOffset.UtcNow
}));

app.Run();
