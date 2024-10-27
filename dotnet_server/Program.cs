using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<CompressionAMQP.RpcProducer>();

builder.Services.AddSingleton<Handlers>();

var app = builder.Build();

app.Use(async (context, next) =>
{
  var httpMaxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

  // TODO base limit on user role (free vs paid)
  if (httpMaxRequestBodySizeFeature is not null)
    // 100MB limit
    httpMaxRequestBodySizeFeature.MaxRequestBodySize = 100_000_000;

  await next(context);
});

app.MapPost("/upload", async (HttpRequest req, Handlers handlers) => await handlers.HandleCompressionRequest(req));

app.Run();
