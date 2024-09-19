using TwitchClipPlayer.Config;
using TwitchClipPlayer.Services;


var builder = WebApplication.CreateBuilder(args);

// Load configuration from the JSON file
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("config/twitchconfig.json", optional: false, reloadOnChange: true)
    .Build();

// Bind the configuration to the TwitchConfig class
var twitchConfig = new TwitchConfig();
configuration.Bind(twitchConfig);

// Register TwitchConfig with the DI container
builder.Services.AddSingleton(twitchConfig);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(b =>
    {
        b.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register the Twitch service
builder.Services.AddHttpClient();
//builder.Services.AddScoped<ITwitchService, TwitchService>();
builder.Services.AddSingleton<ITwitchService, TwitchService>();

// Add controllers
builder.Services.AddControllers();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();