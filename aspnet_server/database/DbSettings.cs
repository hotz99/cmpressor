namespace database;

public class DbSettings
{
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;

    // ctor for DI
    public DbSettings() { }

    public DbSettings(string host, string port, string username, string password, string database)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        Database = database;
    }
}