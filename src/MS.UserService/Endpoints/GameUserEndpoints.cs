using System.Security.Claims;
using MS.UserService.Services;

namespace MS.UserService.Endpoints;

public static class GameUserEndpoints
{
    public static void MapGameUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("GameUsers");

        // 拿着 MP 签发的 JWT 来访问：第一次访问自动建档，后面都是查询。
        // 这一个接口就是 v0.0.1 "跑通流程" 的验收点。
        group.MapGet("/me", async (ClaimsPrincipal claims, GameUserService svc) =>
        {
            var idStr = claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idStr is null || !Guid.TryParse(idStr, out var uid))
                return Results.Unauthorized();

            var user = await svc.GetOrCreateAsync(uid);
            return Results.Ok(user);
        })
        .RequireAuthorization()
        .WithName("GameUserMe");
    }
}
