using Crypto_Hockey.Components;
using Crypto_Hockey.Data;
using Crypto_Hockey.Models;
using Crypto_Hockey.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure blockchain settings
builder.Services.Configure<BlockchainConfig>(
    builder.Configuration.GetSection("BlockchainConfig"));

// Add database context
var sqlServerConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteFallbackPath = GetSqliteFallbackPath(builder.Environment.ContentRootPath);
var useSqlServer = ShouldUseSqlServer(sqlServerConnectionString);

builder.Services.AddDbContext<GameDbContext>(options =>
{
    if (useSqlServer)
    {
        options.UseSqlServer(sqlServerConnectionString);
        return;
    }

    options.UseSqlite($"Data Source={sqliteFallbackPath}");
});

// Register services
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameEngine, GameEngine>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IOnlineCompetitionService, OnlineCompetitionService>();

var app = builder.Build();

if (useSqlServer)
{
    app.Logger.LogInformation("Using SQL Server for game persistence.");
}
else
{
    app.Logger.LogWarning(
        "Using SQLite fallback database at {DatabasePath} because a production-safe SQL Server connection string was not configured.",
        sqliteFallbackPath);
}

// Configure the HTTP request pipeline.
var trustProxyTerminatedTls = builder.Configuration.GetValue<bool>("TRUST_PROXY_HEADERS_FROM_RENDER");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!trustProxyTerminatedTls)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();

static bool ShouldUseSqlServer(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    var isLocalDbConnection = connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase);
    return !isLocalDbConnection || OperatingSystem.IsWindows();
}

static string GetSqliteFallbackPath(string contentRootPath)
{
    var appDataDirectory = Path.Combine(contentRootPath, "App_Data");
    Directory.CreateDirectory(appDataDirectory);
    return Path.Combine(appDataDirectory, "crypto-hockey.db");
}
