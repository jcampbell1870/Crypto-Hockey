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
    if (useSqlServer)
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await EnsureSqliteFallbackSchemaAsync(dbContext, sqliteFallbackPath, app.Logger);
    }
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

static async Task EnsureSqliteFallbackSchemaAsync(
    GameDbContext dbContext,
    string sqliteFallbackPath,
    ILogger logger)
{
    if (!File.Exists(sqliteFallbackPath))
    {
        await dbContext.Database.EnsureCreatedAsync();
        return;
    }

    logger.LogInformation("Updating SQLite fallback schema at {DatabasePath}.", sqliteFallbackPath);

    await dbContext.Database.OpenConnectionAsync();
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PlayerProfiles" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PlayerProfiles" PRIMARY KEY AUTOINCREMENT,
                "WalletAddress" TEXT NOT NULL,
                "TotalGames" INTEGER NOT NULL DEFAULT 0,
                "TotalWins" INTEGER NOT NULL DEFAULT 0,
                "TotalLosses" INTEGER NOT NULL DEFAULT 0,
                "TotalRewardsEarned" TEXT NOT NULL DEFAULT '0',
                "CreatedAt" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                "LastPlayedAt" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "GameSessions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_GameSessions" PRIMARY KEY AUTOINCREMENT,
                "PlayerAddress" TEXT NOT NULL,
                "PlayerScore" INTEGER NOT NULL DEFAULT 0,
                "OpponentScore" INTEGER NOT NULL DEFAULT 0,
                "DifficultyLevel" TEXT NOT NULL DEFAULT 'Medium',
                "PlayerWon" INTEGER NOT NULL DEFAULT 0,
                "StartedAt" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                "EndedAt" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000',
                "RewardAmount" TEXT NOT NULL DEFAULT '0',
                "TransactionHash" TEXT NULL,
                "RewardClaimed" INTEGER NOT NULL DEFAULT 0,
                "PlayerProfileId" INTEGER NULL
            );
            """);

        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "WalletAddress", "\"WalletAddress\" TEXT NOT NULL DEFAULT ''");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "TotalGames", "\"TotalGames\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "TotalWins", "\"TotalWins\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "TotalLosses", "\"TotalLosses\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "TotalRewardsEarned", "\"TotalRewardsEarned\" TEXT NOT NULL DEFAULT '0'");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "CreatedAt", "\"CreatedAt\" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'");
        await EnsureSqliteColumnExistsAsync(dbContext, "PlayerProfiles", "LastPlayedAt", "\"LastPlayedAt\" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'");

        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "PlayerAddress", "\"PlayerAddress\" TEXT NOT NULL DEFAULT ''");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "PlayerScore", "\"PlayerScore\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "OpponentScore", "\"OpponentScore\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "DifficultyLevel", "\"DifficultyLevel\" TEXT NOT NULL DEFAULT 'Medium'");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "PlayerWon", "\"PlayerWon\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "StartedAt", "\"StartedAt\" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "EndedAt", "\"EndedAt\" TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000'");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "RewardAmount", "\"RewardAmount\" TEXT NOT NULL DEFAULT '0'");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "TransactionHash", "\"TransactionHash\" TEXT NULL");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "RewardClaimed", "\"RewardClaimed\" INTEGER NOT NULL DEFAULT 0");
        await EnsureSqliteColumnExistsAsync(dbContext, "GameSessions", "PlayerProfileId", "\"PlayerProfileId\" INTEGER NULL");

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_PlayerProfiles_WalletAddress"
            ON "PlayerProfiles" ("WalletAddress");
            """);

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_GameSessions_PlayerAddress"
            ON "GameSessions" ("PlayerAddress");
            """);
    }
    finally
    {
        await dbContext.Database.CloseConnectionAsync();
    }
}

static async Task EnsureSqliteColumnExistsAsync(
    GameDbContext dbContext,
    string tableName,
    string columnName,
    string columnDefinition)
{
    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
    }

    await reader.DisposeAsync();
    await dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{tableName}\" ADD COLUMN {columnDefinition};");
}
