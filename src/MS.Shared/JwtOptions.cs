namespace MS.Shared;

// 这套配置必须和 MP.UserService 的 appsettings.json 里 Jwt 节点完全一致，
// 否则签名校验通不过。MP 是签发方，MS 这边只做校验，不签发。
//
// 关键点：Issuer / Audience / SecretKey 三者都要跟 MP 对齐，
// 联调时如果 MS 这边一直 401，先检查这三个值有没有抄对。
public class JwtOptions
{
    public string Issuer { get; set; } = "mp-midplatform";
    public string Audience { get; set; } = "game-clients";
    public string SecretKey { get; set; } = string.Empty;
}
