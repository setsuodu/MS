using System.Text;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MS.Shared;
using MS.UserService.Data;
using MS.UserService.Endpoints;
using MS.UserService.Models;
using MS.UserService.Repositories;
using MS.UserService.Services;
using Npgsql;

[assembly: DapperAot]

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 只用于建表，不用于运行时查询
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
           .UseSnakeCaseNamingConvention());

// Postgres 连接（运行时走这个，Dapper.AOT用）
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Postgres connection string not found");
    return new NpgsqlDataSourceBuilder(connStr).Build();
});

builder.Services.AddScoped<IGameUserRepository, GameUserRepository>();
builder.Services.AddScoped<GameUserService>();

// ==================== JWT 校验（跟 MP.UserService 保持完全一致） ====================
// UserService 独立校验一遍 token，不完全信任 Gateway 已经验过——
// 微服务之间零信任，Gateway 挂了/被绕过时这层还在。
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// v0.0.1 极简阶段：直接按当前模型建表，没有迁移历史文件。
// 等表结构稳定、需要多环境演进时，再切回 dotnet ef migrations（跟 MP 一样的做法）。
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ms-userservice" }));

app.MapGameUserEndpoints();

app.Run();

[JsonSerializable(typeof(GameUser))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
