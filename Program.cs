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
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddHttpClient<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IGameEngine, GameEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
