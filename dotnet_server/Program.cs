using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
  options.Limits.MaxRequestBodySize = null;
});

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");

var dbConnString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName}";

builder.Services.AddDbContext<Database.AppDbContext>(options =>
    options.UseNpgsql(dbConnString));

builder.Services.AddSingleton<RabbitMQ.Client.ConnectionFactory>(new RabbitMQ.Client.ConnectionFactory
{
  // TODO use env vars
  HostName = "localhost",
  Port = 5672
});

builder.Services.AddSingleton<RabbitmqRpc.RpcProducer>();

builder.Services.AddSingleton<Handlers>();

var app = builder.Build();

app.MapPost("/upload", async (IFormFile file, Handlers handlers) => await handlers.HandleCompressionRequest(file)).DisableAntiforgery();

app.Run();
