using Microsoft.EntityFrameworkCore;
using Compression.Protobuf;

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

/*app.MapPost("/upload", async (IFormFile file, Handlers handlers) => await handlers.HandleCompressionRequest(file)).DisableAntiforgery();*/

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0
// TODO bind to protobuf generated class instead ?
app.MapPost("/upload", async (HttpRequest request, Handlers handlers) =>
{
  var tasks = new List<Task<CompressionResponse>>();

  // Start a compression task for each file
  foreach (var file in request.Form.Files)
  {
    if (file.Length == 0)
    {
      Console.WriteLine("no file was uploaded");
      continue;
    }

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var videoBytes = ms.ToArray();

    var fileIndex = file.Name.Replace("file", "");
    var format = request.Form[$"format{fileIndex}"];
    var codec = request.Form[$"codec{fileIndex}"];

    // Create a compression task for each file and add it to the list
    var compressionTask = handlers.HandleCompressionRequest(new CompressionRequest
    {
      VideoBytes = Google.Protobuf.ByteString.CopyFrom(videoBytes),
      Format = Enum.Parse<Format>(format, true),
      Codec = Enum.Parse<Codec>(codec, true)
    });

    tasks.Add(compressionTask);
  }

  // Process tasks as they complete
  while (tasks.Count > 0)
  {
    // Wait for any task to complete
    var completedTask = await Task.WhenAny(tasks);
    tasks.Remove(completedTask);

    // Retrieve and respond with the result
    var result = await completedTask;
    await response.WriteAsync($"File processed with result: {result}\n");
    await response.Body.FlushAsync(); // Immediately send each result
  }
}).DisableAntiforgery();

app.Run();
