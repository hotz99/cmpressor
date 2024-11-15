using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Compression.Protobuf;

const string WEB_CLIENT_URL = "http://localhost:5173";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowSpecificOrigin",
      policy =>
      {
        policy.WithOrigins(WEB_CLIENT_URL)
                .AllowAnyMethod()
                .AllowAnyHeader();
      });
});

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

app.UseCors("AllowSpecificOrigin");

/*app.MapPost("/upload", async (IFormFile file, Handlers handlers) => await handlers.HandleCompressionRequest(file)).DisableAntiforgery();*/

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0
// https://andrewlock.net/reading-json-and-binary-data-from-multipart-form-data-sections-in-aspnetcore/
// TODO bind to protobuf generated class instead ?
app.MapPost("/upload", async (IFormFile videoFile,
    [FromForm] string outputFormat, [FromForm] string codec, Handlers handlers) =>
{
  Console.WriteLine($"outputFormat: {outputFormat}");
  Console.WriteLine($"codec: {codec}");

  using (MemoryStream ms = new MemoryStream())
  {
    videoFile.CopyTo(ms);

    var compressionTask = handlers.HandleCompressionRequest(new CompressionRequest
    {
      VideoBytes = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
      OutputFormat = Enum.Parse<Format>(outputFormat, true),
      Codec = Enum.Parse<Codec>(codec, true)
    });

    return await compressionTask;
  }
}).DisableAntiforgery();

app.Run();
