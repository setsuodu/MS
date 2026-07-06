namespace MS.UserService.Models;

// 注意：这里的 Id 不是本服务生成的，是 MP 那边 User.Id（JWT 的 NameIdentifier claim）。
// MS 从不给玩家分配新的身份 ID，只用 MP 发的这个 uid 做外键，
// 这样以后同一个 IP 下想加别的 MS（比如另一个游戏服）也能共用同一套账号体系。
public class GameUser
{
    public Guid Uid { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
