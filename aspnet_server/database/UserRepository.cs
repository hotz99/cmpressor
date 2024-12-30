
using Dapper;
using entities;
using database;

namespace repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAll();
    Task<User> GetById(int id);
    Task<User> GetByEmail(string email);
    Task Create(User user);
    Task Update(User user);
    Task Delete(int id);
}

public class UserRepository : IUserRepository
{
    private DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task Create(User user)
    {
        using var connection = _context.CreateConnection();

        var defaultPlan = await connection.QuerySingleAsync<SubscriptionPlan>(
            "SELECT * FROM subscription_plans WHERE name = 'Free'");

        var sql = """
        INSERT INTO users (email, password_hash, subscription_plan_id, 
                           remaining_conversion_mins, current_concurrent_conversions, reset_date)
        VALUES (@Email, @PasswordHash, @SubscriptionPlanId, 
                @RemainingConversionMins, @CurrentConcurrentConversions, @ResetDate)
    """;

        var now = DateTime.UtcNow;
        var monthlyResetDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);

        var parameters = new
        {
            user.Email,
            user.PasswordHash,
            defaultPlan.SubscriptionPlanId,
            RemainingConversionMins = defaultPlan.MaxConversionMins,
            CurrentConcurrentConversions = 0,
            ResetDate = monthlyResetDate,
            CreatedAt = DateTime.UtcNow
        };

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<IEnumerable<User>> GetAll()
    {
        using var connection = _context.CreateConnection();
        var sql = """
            SELECT * FROM users
        """;
        return await connection.QueryAsync<User>(sql);
    }

    public async Task<User> GetById(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = """
            SELECT * FROM users 
            WHERE user_id = @id
        """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { id });
    }

    public async Task<User> GetByEmail(string email)
    {
        using var connection = _context.CreateConnection();
        var sql = """
            SELECT * FROM users
            WHERE email = @email
        """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task Update(User user)
    {
        using var connection = _context.CreateConnection();
        var sql = """
            UPDATE users 
            SET email = @Email, 
                password_hash = @PasswordHash
            WHERE user_id = @Id
        """;
        await connection.ExecuteAsync(sql, user);
    }

    public async Task Delete(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = """
            DELETE FROM users 
            WHERE user_id = @id
        """;
        await connection.ExecuteAsync(sql, new { id });
    }
}