using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Npgsql;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using RabbitMQ.Client;
//
using API.Services;
using Repositories.Implementations;
using Repositories.Interfaces;

// Find where the web application is being configured
var builder = WebApplication.CreateBuilder(args);

// Add this line to change the port
builder.WebHost.UseUrls("http://localhost:5267"); // Change to a different port

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddControllers();
// Replace the existing CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder
            .WithOrigins("http://localhost:5089")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddHostedService<NotificationBackgroundService>();

// Repositories
builder.Services.AddScoped<IUserInterface, UserRepository>();
builder.Services.AddScoped<ITaskInterface, TaskRepository>();
builder.Services.AddScoped<IChatInterface, ChatRepository>();
builder.Services.AddScoped<INotificationInterface, NotificationRepository>();
// Register Database Connection
builder.Services.AddSingleton<NpgsqlConnection>((serviceProvider) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("pgconn");
    return new NpgsqlConnection(connectionString);
});

// Redis Connection
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new ArgumentNullException("Redis is not defined");
    return ConnectionMultiplexer.Connect(redisConnectionString);
});

// Register RedisService
builder.Services.AddSingleton<RedisService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]
            ?? throw new ArgumentNullException("Jwt:Key is empty")))
    };
});

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("token", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Name = HeaderNames.Authorization
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "token"
                },
            },
            Array.Empty<string>()
        }
    });
});

//Elastic Search Services
builder.Services.AddSingleton<ElasticsearchService>();
builder.Services.AddSingleton(provider =>
{
    var configuration = builder.Configuration;

    // Ensure required values are not null
    string elasticUri = configuration["Elasticsearch:Uri"]
        ?? throw new InvalidOperationException("Elasticsearch:Uri is missing from configuration.");
    string taskIndex = configuration["Elasticsearch:TaskIndex"]
        ?? throw new InvalidOperationException("Elasticsearch:TaskIndex is missing from configuration.");
    string username = configuration["Elasticsearch:Username"]
        ?? throw new InvalidOperationException("Elasticsearch:Username is missing from configuration.");
    string password = configuration["Elasticsearch:Password"]
        ?? throw new InvalidOperationException("Elasticsearch:Password is missing from configuration.");

    var settings = new ElasticsearchClientSettings(new Uri(elasticUri))
        .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
        .DefaultIndex(taskIndex)
        .Authentication(new BasicAuthentication(username, password))
        .DisableDirectStreaming();

    return new ElasticsearchClient(settings);
});


// Configure SignalR with options
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Helpful for debugging
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// RabbitMQ connection with retry logic
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var factory = new ConnectionFactory()
    {
        HostName = "localhost",
        UserName = "guest",
        Password = "guest",
        DispatchConsumersAsync = true
    };

    int retryCount = 3;
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            using var connection = factory.CreateConnection();
            Console.WriteLine("[RabbitMQ] Connection successful.");
            return factory;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ] Connection attempt {i + 1} failed: {ex.Message}");
            Thread.Sleep(2000); // Wait 2 seconds before retrying
        }
    }
    throw new Exception("[RabbitMQ] Failed to establish connection after retries.");
});

// RabbitMQ service
builder.Services.AddSingleton<RabbitMqService>();
// background service for listening to messages
builder.Services.AddHostedService<RabbitMqListener>();

// SignalR Hub as a scoped service
builder.Services.AddTransient<ChatHub>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Keep only this mapping with the detailed configuration
app.MapHub<ChatHub>("/chatHub", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    options.ApplicationMaxBufferSize = 102400; // 100 KB
    options.TransportMaxBufferSize = 102400; // 100 KB
});

// Indexing for Elasticsearch
app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskInterface>();
    var esService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();

    try
    {
        await esService.CreateIndexAsync();
        var esServiceRepo = await taskRepository.GetAll();

        if (esServiceRepo.Any())
        {
            foreach (var task in esServiceRepo)
            {
                await esService.IndexTaskAsync(task);
            }
            Console.WriteLine($"✅ {esServiceRepo.Count} Tasks indexed successfully in Elasticsearch.");
        }
        else
        {
            Console.WriteLine("⚠️ No tasks found in PostgreSQL.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error indexing tasks: {ex.Message}");
    }
});

app.MapHub<NotificationHub>("/notificationHub");

app.Run();