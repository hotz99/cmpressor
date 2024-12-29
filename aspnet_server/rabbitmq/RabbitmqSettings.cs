namespace rabbitmq;

public class RabbitmqSettings
{
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // ctor for DI
    public RabbitmqSettings() { }

    public RabbitmqSettings(string host, string port, string username, string password)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
    }
}