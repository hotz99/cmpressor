using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Compression.Protobuf;
using repositories;
using services;

const string WEB_CLIENT_URL = "http://localhost:5000";
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

{
  var services = builder.Services;
  var env = builder.Environment;

  services.AddCors(options =>
{
  options.AddPolicy("AllowAll",
        policy =>
        {
          policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
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

  services.Configure<database.DbSettings>(options =>
  {
    options.Host = dbHost ?? "localhost";
    options.Port = dbPort ?? "5432";
    options.Username = dbUser ?? "postgres";
    options.Password = dbPassword ?? "postgres";
    options.Database = dbName ?? "postgres";
  });

  services.AddSingleton<database.DapperContext>();
  // TODO why not singletons ?
  services.AddScoped<IUserRepository, UserRepository>();
  services.AddScoped<IUserService, UserService>();

  services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
  services.AddScoped<ISubscriptionService, SubscriptionService>();
  services.AddMemoryCache();

  var rabbitmqUser = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
  var rabbitmqPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
  var rabbitmqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
  var rabbitmqPort = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";

  var rabbitmqUri = $"amqp://{rabbitmqUser}:{rabbitmqPassword}@{rabbitmqHost}:{rabbitmqPort}";

  Console.WriteLine($"rabbitmqUri: {rabbitmqUri}");

  services.AddSingleton(new RabbitMQ.Client.ConnectionFactory
  {
    Uri = new Uri(rabbitmqUri)
  });

  services.Configure<rabbitmq.RabbitmqSettings>(options =>
  {
    options.Host = rabbitmqHost;
    options.Port = rabbitmqPort;
    options.Username = rabbitmqUser;
    options.Password = rabbitmqPassword;
  });

  services.AddSingleton<rabbitmq.RpcProducer>();

  var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<util.JwtSettings>();
  services.AddSingleton<util.JwtSettings>(jwtSettings);

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

  services.AddAuthorization(options =>
  {
    options.AddPolicy("RequireSubscriptionPlan", policy =>
    // all tokens will have this claim, but redundancy is fine
      policy.RequireClaim("SubscriptionPlan"));
  });

  services.AddSingleton<Handlers>();
}



var app = builder.Build();

{
  using var scope = app.Services.CreateScope();
  var context = scope.ServiceProvider.GetRequiredService<database.DapperContext>();
  await context.Init();

  var connection = context.CreateConnection();
  await context.SeedSubscriptionPlansAsync(connection);
}


app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
  System.Console.WriteLine("we are in development mode");
  app.UseDeveloperExceptionPage();
}

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
  var producer = app.Services.GetRequiredService<rabbitmq.RpcProducer>();
  await producer.StartAsync();

}

app.MapGet("/", () => "yes hello");

app.MapPost("/users/sign_up", async ([FromBody] models.users.CreateRequest model, IUserService userService) =>
{
  Console.WriteLine($"signup request: {model.Email} : {model.Password}");

  var result = await userService.Create(model);

  if (!result.IsSuccess)
  {
    Console.WriteLine($"failed to create user: {result.ErrorMessage}");
    return Results.BadRequest(new { Message = result.ErrorMessage });
  }

  return Results.Ok(new { Token = result.Value });
});

app.MapPost("/users/sign_in", async (IUserService userService, [FromBody] models.users.CreateRequest user) =>
{
  // TODO should this should be done in a middleware ?
  if (user == null)
  {
    return Results.BadRequest("invalid request");
  }

  Console.WriteLine($"signin request: {user.Email} : {user.Password}");

  // TODO encapsulate this in a service, use it also in signup
  var foundUser = await userService.GetByEmail(user.Email);

  // TODO `is null` vs `== null` ?
  if (foundUser is null)
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
app.MapPost("/upload", async (ISubscriptionService subscriptionService, HttpContext context, IFormFile videoFile,
    [FromForm] string outputFormat, [FromForm] string codec, Handlers handlers) =>
{
  var subscriptionPlan = context.User.FindFirst("SubscriptionPlan")?.Value;
  var result = subscriptionService.ValidateSubscriptionLimits(subscriptionPlan, (int)videoFile.Length, 10, 1);

  // TODO should this be done in a middleware ?
  if (!result.IsSuccess)
  {
    return Results.BadRequest(new { Message = result.ErrorMessage });
  }

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
