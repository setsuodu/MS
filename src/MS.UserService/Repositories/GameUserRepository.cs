using Dapper;
using MS.UserService.Models;
using Npgsql;

namespace MS.UserService.Repositories;

public class GameUserRepository : IGameUserRepository
{
    private readonly NpgsqlDataSource _db;

    public GameUserRepository(NpgsqlDataSource db) => _db = db;

    public async Task<GameUser?> GetByUidAsync(Guid uid)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QueryFirstOrDefaultAsync<GameUser>(
            "SELECT * FROM game_users WHERE uid = @Uid", new { Uid = uid });
    }

    public async Task CreateAsync(GameUser user)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            "INSERT INTO game_users (uid, nickname, level, created_at) VALUES (@Uid, @Nickname, @Level, @CreatedAt)",
            user);
    }
}
