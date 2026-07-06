using MS.UserService.Models;
using MS.UserService.Repositories;

namespace MS.UserService.Services;

// 只做一件事：拿着 MP 给的 uid，查游戏内档案，没有就建一个默认档案。
// 这是验证"MP 登录 → MS 落地"这条链路是否打通的核心逻辑。
public class GameUserService(IGameUserRepository repo)
{
    public async Task<GameUser> GetOrCreateAsync(Guid uid)
    {
        var user = await repo.GetByUidAsync(uid);
        if (user is not null) return user;

        user = new GameUser { Uid = uid, Nickname = $"Player_{uid.ToString()[..6]}" };
        await repo.CreateAsync(user);
        return user;
    }
}
