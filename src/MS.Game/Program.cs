using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

// 验证 Gateway → Game 这条链路能通，具体战斗/房间逻辑之后再填
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "ms-game" }));

app.MapGet("/api/game/ping", () => Results.Ok(new PingResponse("pong", DateTime.UtcNow)));

// ==================== TODO：游戏逻辑 ====================
// - LiteNetLib UDP 监听：建议用 IHostedService 常驻，跟 Minimal API 的 Kestrel 分开监听端口
// - MemoryPack 序列化协议
// - 房间/Actor（不用 Akka.NET，参考文档里 AOT 兼容问题，考虑自研轻量 Actor 或纯状态机）
// - AOI（KDTree/QuadTree/Octree 三选一，看同屏人数量级）

app.Run();

record PingResponse(string Message, DateTime ServerTime);

[JsonSerializable(typeof(PingResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
