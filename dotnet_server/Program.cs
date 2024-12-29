using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Compression.Protobuf;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using Npgsql;
using database;
using repositories;
using services;

const string WEB_CLIENT_URL = "http://localhost:5173";
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

{
  var services = builder.Services;
  var env = builder.Environment;

  services.AddCors(options =>
{
  options.AddPolicy("AllowSpecificOrigin",
      policy =>
        policy.WithOrigins(WEB_CLIENT_URL)
                .AllowAnyMethod()
                .AllowAnyHeader()
        );
});

  builder.WebHost.ConfigureKestrel(options =>
  {
    options.Limits.MaxRequestBodySize = null;
  });

  services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

  var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
  var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
  var dbUser = Environment.GetEnvironmentVariable("DB_USER");
  var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
  var dbName = Environment.GetEnvironmentVariable("DB_NAME");

  var dbConnString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName}";

  services.Configure<DbSettings>(builder.Configuration.GetSection("DbSettings"));
  // services.AddSingleton<DbSettings>(new DbSettings
  // (
  //   !String.IsNullOrEmpty(dbHost) ? dbHost : "localhost",
  //   !String.IsNullOrEmpty(dbPort) ? dbPort : "5432",
  //   !String.IsNullOrEmpty(dbUser) ? dbUser : "postgres",
  //   !String.IsNullOrEmpty(dbPassword) ? dbPassword : "password",
  //   !String.IsNullOrEmpty(dbName) ? dbName : "postgres"
  // ));

  var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<util.JwtSettings>();
  services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
          .AddJwtBearer(options =>
          {
            options.TokenValidationParameters = new TokenValidationParameters
            {
              ValidateIssuerSigningKey = true,
              IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidIssuer = jwtSettings.Issuer,
              ValidAudience = jwtSettings.Audience
            };
          });

  services.AddSingleton<util.JwtSettings>(jwtSettings);

  services.AddSingleton<DataContext>();
  services.AddScoped<IUserRepository, UserRepository>();
  services.AddScoped<IUserService, UserService>();

  services.AddAuthorization(options =>
  {
    options.AddPolicy("PaidUser", policy =>
    {
      policy.RequireClaim("SubscriptionType", "Paid");
    });
  });

  services.AddSingleton<RabbitMQ.Client.ConnectionFactory>(new RabbitMQ.Client.ConnectionFactory
  {
    // TODO use env vars
    HostName = "localhost",
    Port = 5672
  });

  services.AddSingleton<RabbitmqRpc.RpcProducer>();

  services.AddSingleton<Handlers>();

}



var app = builder.Build();

{
  using var scope = app.Services.CreateScope();
  var context = scope.ServiceProvider.GetRequiredService<DataContext>();
  await context.Init();

  var connection = context.CreateConnection();
  await context.SeedSubscriptionPlansAsync(connection);
}

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowSpecificOrigin");


app.Use(async (context, next) =>
{
  if (context.Request.Headers.ContainsKey("Authorization"))
  {
    Console.WriteLine("request has auth token");
  }
  else
  {
    Console.WriteLine("request does not have auth token");
  }

  await next();
});

{
  var producer = app.Services.GetRequiredService<RabbitmqRpc.RpcProducer>();
  await producer.StartAsync();

}

app.MapPost("/users/signup", async ([FromBody] models.users.CreateRequest user, IUserService userService) =>
{
  // TODO error handling e.g. if user already exists
  await userService.Create(user);
  return Results.Ok();
});

app.MapPost("/users/signin", async (IUserService userService, [FromBody] models.users.CreateRequest user) =>
{
  // TODO should this should be done in a middleware ?
  if (user == null)
  {
    return Results.BadRequest("invalid request");
  }

  // TODO encapsulate this in a service, use it also in signup
  var foundUser = await userService.GetByEmail(user.Email);

  if (foundUser == null)
  {
    return Results.BadRequest("user not found");
  }

  var isPasswordValid = BCrypt.Net.BCrypt.Verify(user.Password, foundUser.PasswordHash);

  if (!isPasswordValid)
  {
    return Results.BadRequest("invalid password");
  }

  var token = await userService.GenerateToken(foundUser);

  return Results.Ok(new { token });
});

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0
// https://andrewlock.net/reading-json-and-binary-data-from-multipart-form-data-sections-in-aspnetcore/
// TODO bind to protobuf generated class instead ?
app.MapPost("/upload", async (HttpContext context, IFormFile videoFile,
    [FromForm] string outputFormat, [FromForm] string codec, Handlers handlers) =>
{
  var subscriptionPlan = context.User.FindFirst("SubscriptionPlan")?.Value;

  if (subscriptionPlan == "Paid")
  {
    Console.WriteLine("we got a paid plan user");
  }
  else
  {
    Console.WriteLine("we got a free plan user");
  }

  Console.WriteLine($"outputFormat: {outputFormat}");
  Console.WriteLine($"codec: {codec}");

  using (MemoryStream ms = new MemoryStream())
  {
    videoFile.CopyTo(ms);

    var parsedOutputFormat = Enum.Parse<Format>(outputFormat, true);
    Console.WriteLine($"parsedOutputFormat: {parsedOutputFormat}");

    var parsedCodec = Enum.Parse<Codec>(codec, true);
    Console.WriteLine($"parsedCodec: {parsedCodec}");

    var compressionTask = handlers.HandleCompressionRequest(new CompressionRequest
    {
      VideoBytes = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
      OutputFormat = parsedOutputFormat,
      Codec = parsedCodec
    });


    return await compressionTask;
  }
}).DisableAntiforgery();

app.Run();
