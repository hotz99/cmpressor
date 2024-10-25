using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var host = Environment.GetEnvironmentVariable("DB_HOST");
var port = Environment.GetEnvironmentVariable("DB_PORT");
var user = Environment.GetEnvironmentVariable("DB_USER");
var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
var database = Environment.GetEnvironmentVariable("DB_NAME");

var connectionString = $"Host={host};Port={port};Username={user};Password={password};Database={database}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapPost("/upload", async (HttpRequest request) =>
{
  if (!request.HasFormContentType)
  {
    return Results.BadRequest("request must be multipart/form-data.");
  }

  var form = await request.ReadFormAsync();
  var file = form.Files.GetFile("file");

  if (file == null || file.Length == 0)
  {
    return Results.BadRequest("no file attached");
  }

  var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

  if (!Directory.Exists(uploadPath))
  {
    Directory.CreateDirectory(uploadPath);
  }

  var filePath = Path.Combine(uploadPath, file.FileName);
  using (var stream = new FileStream(filePath, FileMode.Create))
  {
    await file.CopyToAsync(stream);
  }

  return Results.Ok();
});


app.Run();
