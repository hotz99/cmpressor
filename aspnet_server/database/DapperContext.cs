using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace database;

public class DapperContext
{
    private DbSettings _dbSettings;

    // `dbSettings` is resolved from the DI container at runtime
    public DapperContext(IOptions<DbSettings> dbSettings)
    {
        _dbSettings = dbSettings.Value;
    }

    public async Task SeedSubscriptionPlansAsync(IDbConnection connection)
    {
        var existingPlans = await connection.QueryAsync<int>("SELECT subscription_plan_id FROM subscription_plans");

        if (existingPlans.Any())
        {
            return;
        }

        var plans = new[]
                 {
                new { Name = "Free", MaxFileSizeMegabytes = 10, MaxConversionMins = 10, MaxConcurrentConversions = 5 },
                new { Name = "Paid", MaxFileSizeMegabytes = 100, MaxConversionMins = 200, MaxConcurrentConversions = 15 }
            };

        foreach (var plan in plans)
        {
            await connection.ExecuteAsync("""
                    INSERT INTO subscription_plans 
                    (name, max_file_size_megabytes, max_conversion_mins, max_concurrent_conversions)
                    VALUES (@Name, @MaxFileSizeMegabytes, @MaxConversionMins, @MaxConcurrentConversions)
                """, plan);
        }
    }

    public IDbConnection CreateConnection()
    {
        var connectionString = $"Host={_dbSettings.Host}; Port={_dbSettings.Port}; Database={_dbSettings.Database}; Username={_dbSettings.Username}; Password={_dbSettings.Password};";
        return new NpgsqlConnection(connectionString);
    }

    public async Task Init()
    {
        Console.WriteLine($"Host: {_dbSettings.Host}");
        Console.WriteLine($"Port: {_dbSettings.Port}");
        Console.WriteLine($"Database: {_dbSettings.Database}");
        Console.WriteLine($"Username: {_dbSettings.Username}");
        Console.WriteLine($"Password: {_dbSettings.Password}");

        await _initDatabase();
        await _initTables();
    }

    private async Task _initDatabase()
    {
        var connection = this.CreateConnection();
        var sqlDbCount = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{_dbSettings.Database}';";
        var dbCount = await connection.ExecuteScalarAsync<int>(sqlDbCount);
        if (dbCount == 0)
        {
            var sql = $"CREATE DATABASE \"{_dbSettings.Database}\"";
            await connection.ExecuteAsync(sql);
        }
    }

    private async Task _initTables()
    {
        // create tables if they don't exist
        using var connection = CreateConnection();
        await _initUsers();

        async Task _initUsers()
        {
            var sql = """
                CREATE TABLE IF NOT EXISTS Users (
                    Id SERIAL PRIMARY KEY,
                    Title VARCHAR,
                    FirstName VARCHAR,
                    LastName VARCHAR,
                    Email VARCHAR,
                    Role INTEGER,
                    PasswordHash VARCHAR
                );
            """;
            await connection.ExecuteAsync(sql);
        }
    }
}