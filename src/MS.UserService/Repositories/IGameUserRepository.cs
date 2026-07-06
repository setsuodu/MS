using MS.UserService.Models;

namespace MS.UserService.Repositories;

public interface IGameUserRepository
{
    Task<GameUser?> GetByUidAsync(Guid uid);
    Task CreateAsync(GameUser user);
}
