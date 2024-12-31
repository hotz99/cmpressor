using Dapper;
using entities;
using database;

public interface ISubscriptionPlanRepository
{
    Task<IEnumerable<SubscriptionPlan>> GetAll();
    Task<SubscriptionPlan> GetById(int id);
    Task<SubscriptionPlan> GetByName(string name);
    Task Create(SubscriptionPlan subscriptionPlan);
    Task Update(SubscriptionPlan subscriptionPlan);
    Task Delete(int id);
}

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private DapperContext _context;

    public SubscriptionPlanRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task Create(SubscriptionPlan subscriptionPlan)
    {
        using var connection = _context.CreateConnection();

        var sql = @"
            INSERT INTO subscription_plans (name, max_file_size_megabytes, max_conversion_mins, max_concurrent_conversions)
            VALUES (@Name, @MaxFileSizeMegabytes, @MaxConversionMins, @MaxConcurrentConversions)
            RETURNING subscription_plan_id
        ";

        var parameters = new
        {
            subscriptionPlan.Name,
            subscriptionPlan.MaxFileSizeMegabytes,
            subscriptionPlan.MaxConversionMins,
            subscriptionPlan.MaxConcurrentConversions
        };

        var id = await connection.ExecuteScalarAsync<int>(sql, parameters);
        subscriptionPlan = subscriptionPlan with { SubscriptionPlanId = id };
    }

    public async Task<IEnumerable<SubscriptionPlan>> GetAll()
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT * FROM subscription_plans
        ";
        return await connection.QueryAsync<SubscriptionPlan>(sql);
    }

    public async Task<SubscriptionPlan> GetById(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT * FROM subscription_plans WHERE subscription_plan_id = @Id
        ";
        return await connection.QuerySingleOrDefaultAsync<SubscriptionPlan>(sql, new { Id = id });
    }

    public async Task<SubscriptionPlan> GetByName(string name)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            SELECT * FROM subscription_plans WHERE name = @Name
        ";
        return await connection.QuerySingleOrDefaultAsync<SubscriptionPlan>(sql, new { Name = name });
    }

    public async Task Update(SubscriptionPlan subscriptionPlan)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            UPDATE subscription_plans
            SET name = @Name, max_file_size_megabytes = @MaxFileSizeMegabytes, 
                max_conversion_mins = @MaxConversionMins, max_concurrent_conversions = @MaxConcurrentConversions
            WHERE subscription_plan_id = @SubscriptionPlanId
        ";
        await connection.ExecuteAsync(sql, subscriptionPlan);
    }

    public async Task Delete(int id)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            DELETE FROM subscription_plans WHERE subscription_plan_id = @Id
        ";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}