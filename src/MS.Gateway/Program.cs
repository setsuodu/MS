using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MS.Shared;

var builder = WebApplication.CreateSlimBuilder(args);

// ==================== JWT 校验（跟 MP.UserService 用同一套 Issuer/Audience/SecretKey） ====================
// Gateway 不签发 token，只负责校验客户端带来的、由 MP 签发的 token。
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

// ==================== 下游服务地址（容器网络内用服务名） ====================
builder.Services.AddHttpClient("userservice", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]!);
});
builder.Services.AddHttpClient("game", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:Game"]!);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ms-gateway" }));

// ==================== 反向代理 ====================
// v0.0.1 先用最朴素的手写转发，不引入 YARP，减少反射/AOT 兼容面。
// 路由收敛：客户端只认 Gateway 一个地址，具体后面挂了几个服务对它透明。
app.Map("/api/users/{**path}", (HttpContext ctx, IHttpClientFactory factory) =>
        ProxyAsync(ctx, factory, "userservice"))
   .RequireAuthorization();

app.Map("/api/game/{**path}", (HttpContext ctx, IHttpClientFactory factory) =>
        ProxyAsync(ctx, factory, "game"))
   .RequireAuthorization();

app.Run();

static async Task ProxyAsync(HttpContext ctx, IHttpClientFactory factory, string clientName)
{
    var client = factory.CreateClient(clientName);

    var path = ctx.Request.Path.Value ?? string.Empty;
    var target = path + ctx.Request.QueryString;

    var forwardRequest = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), target);

    // 转发 Authorization 头，下游服务(UserService)也会自己独立校验一次 JWT
    if (ctx.Request.Headers.TryGetValue("Authorization", out var auth))
        forwardRequest.Headers.TryAddWithoutValidation("Authorization", auth.ToArray());

    if (HttpMethods.IsPost(ctx.Request.Method) || HttpMethods.IsPut(ctx.Request.Method) || HttpMethods.IsPatch(ctx.Request.Method))
    {
        ctx.Request.EnableBuffering();
        forwardRequest.Content = new StreamContent(ctx.Request.Body);
        if (ctx.Request.ContentType is not null)
            forwardRequest.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
    }

    using var response = await client.SendAsync(forwardRequest, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);

    ctx.Response.StatusCode = (int)response.StatusCode;
    if (response.Content.Headers.ContentType is not null)
        ctx.Response.ContentType = response.Content.Headers.ContentType.ToString();

    await response.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
}
